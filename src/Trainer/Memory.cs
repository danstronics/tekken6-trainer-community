using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace CommunityTrainer;

internal interface IGuestMemory
{
    bool Read(uint address, int length, out byte[] bytes);
    bool Write(uint address, byte[] bytes);
}

internal static class Guest
{
    public static bool ReadNumber(this IGuestMemory memory, uint address, int width, out uint value)
    {
        value = 0;
        if (address == 0 || !memory.Read(address, width, out var bytes) || bytes.Length != width) return false;
        value = width switch
        {
            1 => bytes[0],
            2 => BinaryPrimitives.ReadUInt16BigEndian(bytes),
            4 => BinaryPrimitives.ReadUInt32BigEndian(bytes),
            _ => throw new ArgumentOutOfRangeException(nameof(width))
        };
        return true;
    }

    public static byte[] Encode(uint value, int width)
    {
        var bytes = new byte[width];
        switch (width)
        {
            case 1: bytes[0] = checked((byte)value); break;
            case 2: BinaryPrimitives.WriteUInt16BigEndian(bytes, checked((ushort)value)); break;
            case 4: BinaryPrimitives.WriteUInt32BigEndian(bytes, value); break;
            default: throw new ArgumentOutOfRangeException(nameof(width));
        }
        return bytes;
    }

    public static uint Resolve(this IGuestMemory memory, uint root, params uint[] offsets)
    {
        uint cursor = root;
        foreach (uint offset in offsets)
        {
            if (!memory.ReadNumber(cursor, 4, out uint pointer) || pointer == 0 || pointer >= 0x80000000) return 0;
            ulong next = (ulong)pointer + offset;
            if (next == 0 || next >= 0x80000000) return 0;
            cursor = (uint)next;
        }
        return cursor;
    }
}

internal sealed class ProcessMemory : IGuestMemory, IDisposable
{
    private readonly SafeProcessHandle handle;
    private readonly ulong guestBase;
    public int ProcessId { get; }

    public ProcessMemory(int processId, ulong guestBase)
    {
        if (guestBase > long.MaxValue - uint.MaxValue) throw new ArgumentOutOfRangeException(nameof(guestBase));
        this.guestBase = guestBase;
        ProcessId = processId;
        handle = OpenProcess(0x1000 | 0x10 | 0x20 | 0x08, false, processId);
        if (handle.IsInvalid)
        {
            handle.Dispose();
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        }
    }

    public bool IsAlive => !handle.IsClosed && GetExitCodeProcess(handle, out uint code) && code == 259;

    public bool Read(uint address, int length, out byte[] bytes)
    {
        if (length < 1 || length > 4096) throw new ArgumentOutOfRangeException(nameof(length));
        bytes = new byte[length];
        return address != 0 && IsAlive && ReadProcessMemory(handle, (nint)(guestBase + address), bytes,
            (nuint)length, out nuint count) && count == (nuint)length;
    }

    public bool Write(uint address, byte[] bytes) => address != 0 && bytes.Length > 0 && IsAlive &&
        WriteProcessMemory(handle, (nint)(guestBase + address), bytes, (nuint)bytes.Length, out nuint count) && count == (nuint)bytes.Length;

    public void Dispose() => handle.Dispose();

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern SafeProcessHandle OpenProcess(uint access, bool inherit, int pid);
    [DllImport("kernel32.dll")]
    private static extern bool GetExitCodeProcess(SafeProcessHandle process, out uint exitCode);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool ReadProcessMemory(SafeProcessHandle process, nint address, [Out] byte[] bytes, nuint length, out nuint count);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool WriteProcessMemory(SafeProcessHandle process, nint address, byte[] bytes, nuint length, out nuint count);
}
