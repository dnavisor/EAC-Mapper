using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

public class Program
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Data
    {
        public ulong FromPid;
        public ulong ToPid;
        public IntPtr FromAddress;
        public IntPtr ToAddress;
        public IntPtr Size;

        public IntPtr PsLookupProcessByProcessId;
        public IntPtr MmCopyVirtualMemory;
        public IntPtr ObfDereferenceObject;
    }

    [DllImport("ntdll.dll")]
    public static extern int PsLookupProcessByProcessId(ulong pid, out IntPtr processHandle);

    [DllImport("ntdll.dll")]
    public static extern int MmCopyVirtualMemory(IntPtr fromProcess, IntPtr fromAddress, IntPtr toProcess, IntPtr toAddress, IntPtr size, byte flags, out IntPtr bytesCopied);

    [DllImport("ntdll.dll")]
    public static extern void ObfDereferenceObject(IntPtr objectHandle);

    [DllImport("user32.dll")]
    public static extern IntPtr LoadLibraryA(string dllName);

    [DllImport("kernel32.dll")]
    public static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

    public static int Mapped(Data data)
    {
        if (PsLookupProcessByProcessId(data.FromPid, out IntPtr fromProcess) != 0 || PsLookupProcessByProcessId(data.ToPid, out IntPtr toProcess) != 0)
            return 0xC000000D; // STATUS_INVALID_PARAMETER

        if (MmCopyVirtualMemory(fromProcess, data.FromAddress, toProcess, data.ToAddress, data.Size, 1, out IntPtr sizeCopied) != 0)
            return 0xC000000D; // STATUS_INVALID_PARAMETER

        ObfDereferenceObject(fromProcess);
        ObfDereferenceObject(toProcess);

        return 0; // STATUS_SUCCESS
    }

    public static void Main()
    {
        //Test vars
        int testVariableOne = 0xDEAD;
        int testVariableTwo = 0xBEEF;

        IntPtr user32 = LoadLibraryA("user32.dll");
        IntPtr win32u = LoadLibraryA("win32u.dll");

        //Grab func addr for ntmapvrp
        IntPtr function = GetProcAddress(win32u, "NtMapVisualRelativePoints");

        Data data = new Data
        {
            FromPid = (ulong)Process.GetCurrentProcess().Id,
            ToPid = (ulong)Process.GetCurrentProcess().Id,
            FromAddress = Marshal.UnsafeAddrOfPinnedArrayElement(new[] { testVariableOne }, 0),
            ToAddress = Marshal.UnsafeAddrOfPinnedArrayElement(new[] { testVariableTwo }, 0),
            Size = (IntPtr)Marshal.SizeOf<int>()
        };

        Console.WriteLine("[+] before: {0:X}", testVariableTwo);
        int result = Marshal.GetDelegateForFunctionPointer< Func<Data, int> >(function)(data);
        Console.WriteLine("[+] result: {0}", result);
        Console.WriteLine("[+] after: {0:X}", testVariableTwo);

        Console.ReadLine();
    }
}