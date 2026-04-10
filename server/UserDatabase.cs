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

    // Stub used by Feature 4 — returns empty set until friends table is added.
    public HashSet<int> GetFriendIds(int userId) => new();

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
