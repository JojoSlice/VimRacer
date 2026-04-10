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

    // Server → Client
    S_LobbyList    = 0x10,
    S_LobbyJoined  = 0x11,
    S_LobbyUpdated = 0x12,
    S_LobbyLeft    = 0x13,
    S_GameStart    = 0x14,
    S_Error        = 0x1F,
    S_GameData     = 0x20,
}

public static class Packet
{
    private static readonly Encoding Enc = Encoding.UTF8;

    public static void WriteStr(BinaryWriter w, string s)
    {
        byte[] b = Enc.GetBytes(s);
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
