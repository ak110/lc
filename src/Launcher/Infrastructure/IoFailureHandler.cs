namespace Launcher.Infrastructure;

/// <summary>
/// ファイル操作で <see cref="IOException"/> と <see cref="UnauthorizedAccessException"/> の両方を
/// 無視する定型パターンを集約する。
/// </summary>
/// <remarks>
/// 一時ファイルの削除・属性補正・存在確認など、失敗してもアプリの動作継続が望ましい場面でのみ用いる。
/// catch の粒度（両例外型のみ捕捉）は呼び出し元の従来挙動と一致させており、それ以外の例外は呼び出し元へ伝播する。
/// </remarks>
internal static class IoFailureHandler
{
    /// <summary>
    /// <paramref name="action"/> を実行し、<see cref="IOException"/> または
    /// <see cref="UnauthorizedAccessException"/> が発生した場合は無視する。
    /// </summary>
    internal static void IgnoreIoErrors(Action action)
    {
        try
        {
            action();
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    /// <summary>
    /// <paramref name="action"/> を実行して結果を返す。<see cref="IOException"/> または
    /// <see cref="UnauthorizedAccessException"/> が発生した場合は <paramref name="defaultValue"/> を返す。
    /// </summary>
    internal static T IgnoreIoErrors<T>(Func<T> action, T defaultValue)
    {
        try
        {
            return action();
        }
        catch (IOException)
        {
            return defaultValue;
        }
        catch (UnauthorizedAccessException)
        {
            return defaultValue;
        }
    }
}
