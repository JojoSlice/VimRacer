using System;
using System.IO;
using System.Text;

namespace VimRacer;

public enum MsgType : byte
{
    // Client → Server
    C_Hello        = 0x01,
    C_CreateLobby  = 0x02,
    C_ListLobbies  = 0x03,
    C_JoinLobby    = 0x04,
    C_LeaveLobby   = 0x05,
    C_ToggleReady  = 0x06,
    C_GameData     = 0x07,
    C_PlayerUpdate = 0x08,   // float x, float y
    C_Register     = 0x09,   // WriteStr(username), WriteStr(password)
    C_Login         = 0x0A,   // WriteStr(username), WriteStr(password)
    C_InviteFriend  = 0x0B,   // WriteStr(targetUsername)
    C_AcceptInvite  = 0x0C,   // Write(int lobbyId)
    C_DeclineInvite = 0x0D,   // Write(int lobbyId)

    // Server → Client
    S_LobbyList    = 0x10,
    S_LobbyJoined  = 0x11,
    S_LobbyUpdated = 0x12,
    S_LobbyLeft    = 0x13,
    S_GameStart    = 0x14,
    S_Error        = 0x1F,
    S_GameData     = 0x20,
    S_PlayerUpdate = 0x21,   // byte playerIndex, float x, float y
    S_LoginOk      = 0x22,   // Write(int userId), WriteStr(username)
    S_LoginFail    = 0x23,   // WriteStr(reason)
    S_LobbyInvite  = 0x24,   // WriteStr(fromUsername), Write(int lobbyId), WriteStr(lobbyName)
}

public static class Packet
{
    private static readonly Encoding Enc = Encoding.UTF8;

    public static void WriteStr(BinaryWriter w, string s)
    {
        byte[] b = Enc.GetBytes(s);
        if (b.Length > 255) throw new ArgumentException($"String too long ({b.Length} bytes, max 255)");
        w.Write((byte)b.Length);
        w.Write(b);
    }

    public static string ReadStr(BinaryReader r)
    {
        int len = r.ReadByte();
        return Enc.GetString(r.ReadBytes(len));
    }

    public static byte[] Build(MsgType type, Action<BinaryWriter> write)
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms, Enc, leaveOpen: true);
        bw.Write((byte)type);
        write(bw);
        bw.Flush();
        return ms.ToArray();
    }

    public static byte[] Simple(MsgType type) => [(byte)type];
}
