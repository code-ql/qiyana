using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using qiyana.Models;
using qiyana.Native;

namespace qiyana.Services;

public partial class LcuDiscoveryService : ILcuDiscoveryService
{
    private const uint ProcessQueryLimitedInformation = 0x1000;
    private const uint Th32CsSnapProcess = 0x00000002;
    private const int ProcessCommandLineInformation = 60;

    [GeneratedRegex("--app-port=(\\d+)")]
    private static partial Regex PortRegex();

    [GeneratedRegex("--remoting-auth-token=([\\w-]+)")]
    private static partial Regex TokenRegex();

    [GeneratedRegex("--region=([\\w-]+)")]
    private static partial Regex RegionRegex();

    [GeneratedRegex("--rso_platform_id=([\\w-]+)")]
    private static partial Regex RsoPlatformIdRegex();

    public LCUInfo? GetLcuInfo()
    {
        var pids = GetPidsByName("LeagueClientUx.exe");
        foreach (var pid in pids)
        {
            var cmd = GetProcessCommandLine(pid);
            if (cmd is null) continue;

            var port = PortRegex().Match(cmd);
            var token = TokenRegex().Match(cmd);
            if (port.Success && token.Success)
            {
                var region = RegionRegex().Match(cmd);
                var rso = RsoPlatformIdRegex().Match(cmd);
                return new LCUInfo
                {
                    Port = port.Groups[1].Value,
                    Token = token.Groups[1].Value,
                    Protocol = "https",
                    Region = region.Success ? region.Groups[1].Value : "",
                    RsoPlatformId = rso.Success ? rso.Groups[1].Value : ""
                };
            }
        }
        return null;
    }

    private static List<uint> GetPidsByName(string name)
    {
        var pids = new List<uint>();
        var snap = Kernel32.CreateToolhelp32Snapshot(Th32CsSnapProcess, 0);
        if (snap == (nint)(-1)) return pids;

        try
        {
            var pe = new PROCESSENTRY32W { dwSize = (uint)Marshal.SizeOf<PROCESSENTRY32W>() };
            if (Kernel32.Process32FirstW(snap, ref pe))
            {
                do
                {
                    if (pe.szExeFile.Equals(name, StringComparison.OrdinalIgnoreCase))
                        pids.Add(pe.th32ProcessID);
                } while (Kernel32.Process32NextW(snap, ref pe));
            }
        }
        finally
        {
            Kernel32.CloseHandle(snap);
        }

        return pids;
    }

    private static string? GetProcessCommandLine(uint pid)
    {
        var h = Kernel32.OpenProcess(ProcessQueryLimitedInformation, false, pid);
        if (h == nint.Zero) return null;

        try
        {
            var buf = Marshal.AllocHGlobal(4096);
            try
            {
                var status = NtDll.NtQueryInformationProcess(
                    h, ProcessCommandLineInformation, buf, 4096, out _);
                if (status != 0) return null;

                var us = Marshal.PtrToStructure<UNICODE_STRING>(buf);
                if (us.Buffer == nint.Zero || us.Length == 0) return null;

                return Marshal.PtrToStringUni(us.Buffer, us.Length / 2);
            }
            finally
            {
                Marshal.FreeHGlobal(buf);
            }
        }
        finally
        {
            Kernel32.CloseHandle(h);
        }
    }
}
