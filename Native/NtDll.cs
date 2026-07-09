using System.Runtime.InteropServices;

namespace qiyana.Native;

[StructLayout(LayoutKind.Sequential)]
internal struct UNICODE_STRING
{
    public ushort Length;
    public ushort MaximumLength;
    public nint Buffer;
}

internal static class NtDll
{
    [DllImport("ntdll.dll")]
    public static extern int NtQueryInformationProcess(
        nint ProcessHandle,
        int ProcessInformationClass,
        nint ProcessInformation,
        int ProcessInformationLength,
        out int ReturnLength);
}
