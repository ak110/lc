using System.Xml;

namespace Launcher.Infrastructure;

/// <summary>設定ファイルの解析・入出力で扱う例外の分類。</summary>
public static class ConfigFailure
{
    public static bool IsXml(Exception error) => error is InvalidOperationException or XmlException;

    public static bool IsRead(Exception error) => IsXml(error) || error is IOException
        or UnauthorizedAccessException or FormatException or IndexOutOfRangeException or KeyNotFoundException
        or TimeoutException or System.Security.SecurityException;

    public static bool IsSave(Exception error) => IsRead(error);
}

/// <summary>利用者へ通知する保存失敗の種類。</summary>
public enum ConfigSaveFailureKind
{
    Blocked,
    Write,
}

/// <summary>保存対象と、保存できなかった場合の案内。</summary>
public sealed record ConfigSaveFailure(string FileKind, ConfigSaveFailureKind Kind)
{
    public string Message => Kind == ConfigSaveFailureKind.Blocked
        ? $"設定ファイル({FileKind})を読み込めなかったため、編集内容を保存していません。\r\n"
            + "バックアップから復元するか、ファイルを直してから再起動してください。"
        : $"保存ファイル({FileKind})への書き込みに失敗し、編集内容を保存できませんでした。\r\n"
            + "らんちゃのフォルダーへ書き込める状態にしてから、もう一度保存する操作を行ってください。";
}
