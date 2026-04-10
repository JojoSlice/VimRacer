using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;

namespace VimRacerServer;

internal sealed class UserDatabase
{
    // NOTE: Passwords are stored as unsalted SHA-256 hex hashes.
    // Sufficient for a game server; upgrade to BCrypt for higher-stakes auth.
    private readonly SqliteConnection _conn;

    public UserDatabase(string path)
    {
        _conn = new SqliteConnection($"Data Source={path}");
        _conn.Open();
        EnsureSchema();
    }

    private void EnsureSchema()
    {
        Exec(@"
            CREATE TABLE IF NOT EXISTS users (
                id            INTEGER PRIMARY KEY AUTOINCREMENT,
                username      TEXT    NOT NULL UNIQUE COLLATE NOCASE,
                password_hash TEXT    NOT NULL,
                created_at    INTEGER NOT NULL
            );
            CREATE TABLE IF NOT EXISTS friendships (
                id            INTEGER PRIMARY KEY AUTOINCREMENT,
                requester_id  INTEGER NOT NULL REFERENCES users(id),
                addressee_id  INTEGER NOT NULL REFERENCES users(id),
                status        TEXT    NOT NULL DEFAULT 'pending',
                created_at    INTEGER NOT NULL,
                UNIQUE(requester_id, addressee_id)
            );
        ");
    }

    // ── Public API ───────────────────────────────────────────────────────────

    public bool TryRegister(string username, string password,
                            out int userId, out string error)
    {
        userId = 0;
        if (!IsValidUsername(username)) { error = "Username must be 1–20 alphanumeric chars or underscores."; return false; }
        if (password.Length == 0)      { error = "Password cannot be empty."; return false; }

        string hash = Hash(password);
        long   now  = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        using var cmd = _conn.CreateCommand();
        cmd.CommandText = "INSERT INTO users (username, password_hash, created_at) VALUES (@u, @h, @t)";
        cmd.Parameters.AddWithValue("@u", username.ToLowerInvariant());
        cmd.Parameters.AddWithValue("@h", hash);
        cmd.Parameters.AddWithValue("@t", now);

        try
        {
            cmd.ExecuteNonQuery();
            userId = (int)GetLastInsertId();
            error  = "";
            return true;
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19) // SQLITE_CONSTRAINT
        {
            error = "Username already taken.";
            return false;
        }
    }

    public bool TryLogin(string username, string password,
                         out int userId, out string displayName)
    {
        userId      = 0;
        displayName = "";

        using var cmd = _conn.CreateCommand();
        cmd.CommandText = "SELECT id, username FROM users WHERE username = @u AND password_hash = @h";
        cmd.Parameters.AddWithValue("@u", username);
        cmd.Parameters.AddWithValue("@h", Hash(password));

        using var r = cmd.ExecuteReader();
        if (!r.Read()) return false;

        userId      = r.GetInt32(0);
        displayName = r.GetString(1);
        return true;
    }

    public int? GetUserIdByUsername(string username)
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = "SELECT id FROM users WHERE username = @u";
        cmd.Parameters.AddWithValue("@u", username);
        var result = cmd.ExecuteScalar();
        return result is long id ? (int)id : null;
    }

    // ── Friends ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Sends a friend request from requester to addressee.
    /// If a reverse request already exists, auto-accepts (mutual add = accept).
    /// </summary>
    public (bool Ok, string? Error) SendFriendRequest(int requesterId, int addresseeId)
    {
        if (requesterId == addresseeId)
            return (false, "Cannot add yourself.");

        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // Check for reverse pending request → auto-accept both sides
        using (var cmd = _conn.CreateCommand())
        {
            cmd.CommandText = "SELECT id FROM friendships WHERE requester_id=@a AND addressee_id=@r AND status='pending'";
            cmd.Parameters.AddWithValue("@a", addresseeId);
            cmd.Parameters.AddWithValue("@r", requesterId);
            if (cmd.ExecuteScalar() is not null)
            {
                // Accept the existing reverse request and insert an accepted row for this direction
                using var upd = _conn.CreateCommand();
                upd.CommandText = "UPDATE friendships SET status='accepted' WHERE requester_id=@a AND addressee_id=@r";
                upd.Parameters.AddWithValue("@a", addresseeId);
                upd.Parameters.AddWithValue("@r", requesterId);
                upd.ExecuteNonQuery();

                TryInsertFriendship(requesterId, addresseeId, "accepted", now);
                return (true, null);
            }
        }

        try
        {
            TryInsertFriendship(requesterId, addresseeId, "pending", now);
            return (true, null);
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
        {
            return (false, "Already friends or request pending.");
        }
    }

    public bool RemoveFriendship(int userId, int otherId)
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = @"DELETE FROM friendships
            WHERE (requester_id=@u AND addressee_id=@o)
               OR (requester_id=@o AND addressee_id=@u)";
        cmd.Parameters.AddWithValue("@u", userId);
        cmd.Parameters.AddWithValue("@o", otherId);
        return cmd.ExecuteNonQuery() > 0;
    }

    /// <summary>
    /// Returns accepted friends and incoming pending requests for <paramref name="userId"/>.
    /// IsPending=true means the other user sent a request that hasn't been accepted yet.
    /// </summary>
    public List<(int Id, string Username, bool IsPending)> GetFriends(int userId)
    {
        var result = new List<(int, string, bool)>();
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = @"
            SELECT u.id, u.username, 0 as pending FROM friendships f
            JOIN users u ON u.id = CASE WHEN f.requester_id=@uid THEN f.addressee_id ELSE f.requester_id END
            WHERE (f.requester_id=@uid OR f.addressee_id=@uid) AND f.status='accepted'
            UNION
            SELECT u.id, u.username, 1 as pending FROM friendships f
            JOIN users u ON u.id = f.requester_id
            WHERE f.addressee_id=@uid AND f.status='pending'";
        cmd.Parameters.AddWithValue("@uid", userId);
        using var r = cmd.ExecuteReader();
        while (r.Read())
            result.Add((r.GetInt32(0), r.GetString(1), r.GetInt32(2) == 1));
        return result;
    }

    /// <summary>Returns IDs of accepted friends — used for online/offline push notifications.</summary>
    public HashSet<int> GetFriendIds(int userId)
    {
        var ids = new HashSet<int>();
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = @"
            SELECT CASE WHEN requester_id=@uid THEN addressee_id ELSE requester_id END
            FROM friendships
            WHERE (requester_id=@uid OR addressee_id=@uid) AND status='accepted'";
        cmd.Parameters.AddWithValue("@uid", userId);
        using var r = cmd.ExecuteReader();
        while (r.Read()) ids.Add(r.GetInt32(0));
        return ids;
    }

    private void TryInsertFriendship(int requesterId, int addresseeId, string status, long now)
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = "INSERT INTO friendships (requester_id, addressee_id, status, created_at) VALUES (@r,@a,@s,@t)";
        cmd.Parameters.AddWithValue("@r", requesterId);
        cmd.Parameters.AddWithValue("@a", addresseeId);
        cmd.Parameters.AddWithValue("@s", status);
        cmd.Parameters.AddWithValue("@t", now);
        cmd.ExecuteNonQuery();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static bool IsValidUsername(string u)
    {
        if (u.Length is < 1 or > 20) return false;
        foreach (char c in u)
            if (!char.IsAsciiLetterOrDigit(c) && c != '_') return false;
        return true;
    }

    private static string Hash(string password)
    {
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexStringLower(bytes);
    }

    private long GetLastInsertId()
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = "SELECT last_insert_rowid()";
        return (long)cmd.ExecuteScalar()!;
    }

    private void Exec(string sql)
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }
}
