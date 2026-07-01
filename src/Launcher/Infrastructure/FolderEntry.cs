namespace Launcher.Infrastructure;

/// <summary>
/// フォルダ配下の1項目 (ファイルまたはサブフォルダ) を表す。
/// </summary>
/// <param name="FullPath">絶対パス</param>
/// <param name="DisplayName">表示名 (ファイル・フォルダ名)</param>
/// <param name="IsDirectory">フォルダの場合は true</param>
public sealed record FolderEntry(string FullPath, string DisplayName, bool IsDirectory);
