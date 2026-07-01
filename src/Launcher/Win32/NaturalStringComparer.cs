using System.Runtime.InteropServices;

namespace Launcher.Win32;

/// <summary>
/// エクスプローラ互換の数値順比較 ("9" &lt; "10") を提供するIComparer&lt;string&gt;。
/// shlwapi.dllのStrCmpLogicalW()のラッパー。
/// </summary>
public sealed class NaturalStringComparer : IComparer<string>
{
    public static NaturalStringComparer Instance { get; } = new();

    public int Compare(string? x, string? y)
    {
        if (x is null && y is null) return 0;
        if (x is null) return -1;
        if (y is null) return 1;
        return StrCmpLogicalW(x, y);
    }

    [DllImport("shlwapi.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    static extern int StrCmpLogicalW(string psz1, string psz2);
}
