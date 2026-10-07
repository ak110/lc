namespace Launcher.Infrastructure;

/// <summary>読込結果の案内。復元の確認だけが利用者の回答を必要とする。</summary>
public sealed record ConfigLoadNotice(string Message, bool ConfirmRestore = false);

/// <summary>読込結果に応じた案内と復元の選択を扱う。画面の表示は呼び出し元へ委ねる。</summary>
public static class ConfigLoadInteraction
{
    public static T Accept<T>(ConfigLoadResult<T> result, ConfigFile<T> store, T fallback,
        Func<ConfigLoadNotice, bool> show, string? baseName = null) where T : ConfigStore, new()
    {
        if (result.BackupError is not null)
        {
            show(new ConfigLoadNotice(
                $"設定ファイル({result.Kind})のバックアップ(.bak)を作成できませんでした。\r\n"
                + "読み込んだ内容は使えますが、バックアップを作成できるまで保存は失敗します。\r\n"
                + "らんちゃのフォルダーへ書き込める状態にしてください。"));
        }
        if (result.Status != ConfigLoadStatus.Failed)
        {
            return result.Value;
        }
        if (!store.ProtectOriginal)
        {
            show(new ConfigLoadNotice(
                $"実行状態のファイル({result.Kind})を読み込めませんでした。初期値で続行します。\r\n"
                + "次の保存で再作成します。繰り返す場合は、らんちゃのフォルダーの読み書き権限を確認してください。"));
            return fallback;
        }
        string message = $"設定ファイル({result.Kind})を読み込めませんでした。原本を保護するため保存を停止しています。";
        if (!result.BackupAvailable)
        {
            show(new ConfigLoadNotice(message + "\r\nファイルを直してから再起動してください。"));
            return fallback;
        }
        if (!show(new ConfigLoadNotice(message
            + "\r\n前回正常に読み込めたバックアップから復元しますか？\r\n"
            + "読み込めなかった本体は「.broken-日時」を付けた名前で残します。", ConfirmRestore: true)))
        {
            return fallback;
        }
        try
        {
            return store.RestoreFromBackup(baseName);
        }
        catch (Exception ex) when (ConfigFailure.IsRead(ex))
        {
            DiagnosticLog.Warn("Config.Restore", $"復元失敗: {result.Kind} {ex.GetType().Name}");
            show(new ConfigLoadNotice("復元できませんでした。保存の停止は続きます。\r\n"
                + "バックアップと本体の内容、フォルダーの読み書き権限を確認してから再起動してください。"));
            return fallback;
        }
    }
}
