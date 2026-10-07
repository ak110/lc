using System.Text;

namespace Launcher.Infrastructure;

/// <summary>
/// 保存データの読込結果の状態。
/// </summary>
public enum ConfigLoadStatus
{
    /// <summary>読み込めた</summary>
    Loaded,

    /// <summary>本体もバックアップも無い初回。初期値で始めて保存してよい</summary>
    NotFound,

    /// <summary>読み込めなかった。そのファイルへの保存は停止する</summary>
    Failed,
}

/// <summary>
/// 保存データの読込結果。
/// </summary>
/// <param name="Value">読み込んだデータ。失敗時と初回は初期値</param>
/// <param name="Status">読込の状態</param>
/// <param name="FileName">対象ファイルの完全パス</param>
/// <param name="BackupAvailable">失敗時に復元できるバックアップがあるか</param>
/// <param name="Error">失敗の原因</param>
public sealed record ConfigLoadResult<T>(
    T Value, ConfigLoadStatus Status, string FileName, bool BackupAvailable, Exception? Error)
{
    /// <summary>ログと通知で使う拡張子部分 (例: <c>.cmd.cfg</c>)</summary>
    public string Kind => ConfigFileState.KindOf(FileName);

    /// <summary>読込には成功したが、その内容でバックアップを更新できなかった場合の原因</summary>
    public Exception? BackupError { get; init; }
}

/// <summary>
/// 読込に失敗したファイルへの保存を止めたことを表す例外。原本の上書きを防ぐ。
/// </summary>
public sealed class ConfigSaveBlockedException : IOException
{
    public ConfigSaveBlockedException()
    {
    }

    public ConfigSaveBlockedException(string message)
        : base(message)
    {
    }

    public ConfigSaveBlockedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// 1種類の保存データの読込・保存・復元と、失敗通知の重複抑止を担う。
/// </summary>
public sealed class ConfigFile<T> where T : ConfigStore, new()
{
    readonly string ext;
    readonly Func<byte[], T?>? legacyParser;
    readonly Dictionary<string, ConfigFileState> states = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>原本の保護とバックアップを行うか。ランタイムデータでは無効にする。</summary>
    public bool ProtectOriginal { get; }

    /// <param name="ext">拡張子。1文字目は <c>.</c></param>
    /// <param name="legacyParser">旧形式の読込。旧形式と判定できない内容では呼ばれない</param>
    public ConfigFile(string ext, Func<byte[], T?>? legacyParser = null, bool protectOriginal = true)
    {
        this.ext = ext;
        this.legacyParser = legacyParser;
        ProtectOriginal = protectOriginal;
    }

    /// <summary>対象ファイルの完全パス</summary>
    public string FileName(string? baseName = null) => (baseName ?? ConfigStore.DefaultBaseName) + ext;

    ConfigFileState State(string fileName)
    {
        lock (states)
        {
            if (!states.TryGetValue(fileName, out var state))
            {
                state = new ConfigFileState();
                states.Add(fileName, state);
            }
            return state;
        }
    }

    /// <summary>保存の成否を返す。同じファイルの同じ失敗は、保存が成功するまで1回だけ通知する。</summary>
    public bool Save(T value, Action<ConfigSaveFailure> notify, string? baseName = null)
    {
        string fileName = FileName(baseName);
        ConfigSaveFailure? failure = null;
        try
        {
            using var mutex = ConfigStore.Lock(fileName);
            var state = State(fileName);
            if (ProtectOriginal)
            {
                // 未読込の保存も既存原本の検査を通す。
                if (!state.LoadAttempted)
                {
                    Load(baseName);
                }
                state.BeforeReplace(fileName, bytes => Parse(bytes));
            }
            byte[] content = value.SerializeToBytes();
            ConfigStore.WriteAtomic(fileName, content);
            state.LastGood = content;
            lock (state.NotifiedFailures) state.NotifiedFailures.Clear();
            return true;
        }
        catch (Exception ex) when (ConfigFailure.IsSave(ex))
        {
            failure = RecordFailure(fileName, ex);
        }
        // 通知のモーダルループへ入る前にファイルの排他を解放する。
        if (failure is not null)
        {
            notify(failure);
        }
        return false;
    }

    /// <summary>別プロセスの更新を失わないよう、読込・変更・保存を同じ排他の下で行う。</summary>
    public bool ModifyAndSave(Action<T> modify, Action<ConfigSaveFailure> notify,
        Action<ConfigLoadResult<T>> loadFailed, string? baseName = null)
    {
        string fileName = FileName(baseName);
        ConfigLoadResult<T>? rejected = null;
        ConfigSaveFailure? failure = null;
        bool saved = false;
        try
        {
            using var mutex = ConfigStore.Lock(fileName);
            var result = Load(baseName);
            if (result.Status == ConfigLoadStatus.Failed)
            {
                rejected = result;
            }
            else
            {
                modify(result.Value);
                saved = Save(result.Value, value => failure = value, baseName);
            }
        }
        catch (Exception ex) when (ConfigFailure.IsSave(ex))
        {
            failure = RecordFailure(fileName, ex);
        }
        // モーダル通知はプロセス間排他を解放した後に行う。
        if (rejected is not null) loadFailed(rejected);
        if (failure is not null) notify(failure);
        return saved;
    }

    ConfigSaveFailure? RecordFailure(string fileName, Exception error)
    {
        var kind = error is ConfigSaveBlockedException ? ConfigSaveFailureKind.Blocked : ConfigSaveFailureKind.Write;
        DiagnosticLog.Warn("Config.Save", $"保存失敗: {ext} {kind} {error.GetType().Name}");
        var notified = State(fileName).NotifiedFailures;
        lock (notified)
        {
            return notified.Add(kind) ? new ConfigSaveFailure(ext, kind) : null;
        }
    }

    /// <summary>
    /// 読み込む。成功したらその内容でバックアップを更新する。失敗したファイルへの保存は以後止まる。
    /// </summary>
    public ConfigLoadResult<T> Load(string? baseName = null)
    {
        string fileName = FileName(baseName);
        try
        {
            return LoadCore(baseName);
        }
        catch (Exception ex) when (ConfigFailure.IsRead(ex))
        {
            State(fileName).MarkFailed();
            DiagnosticLog.Warn("Config.Load", $"読込失敗: {ext} {ex.GetType().Name}");
            return new(new T(), ConfigLoadStatus.Failed, fileName,
                ProtectOriginal && File.Exists(ConfigFileState.BackupName(fileName)), ex);
        }
    }

    ConfigLoadResult<T> LoadCore(string? baseName)
    {
        string fileName = FileName(baseName);
        string backupName = ConfigFileState.BackupName(fileName);
        using var mutex = ConfigStore.Lock(fileName);

        if (!File.Exists(fileName))
        {
            if (ProtectOriginal && File.Exists(backupName))
            {
                // 本体だけが消えた状態は初回と区別し、バックアップを上書きさせない
                State(fileName).MarkFailed();
                DiagnosticLog.Warn("Config.Load", $"本体が無くバックアップだけがある: {ConfigFileState.KindOf(fileName)}");
                return new(new T(), ConfigLoadStatus.Failed, fileName, true, new FileNotFoundException("本体が見つからない"));
            }
            State(fileName).MarkLoaded(null);
            return new(new T(), ConfigLoadStatus.NotFound, fileName, false, null);
        }

        byte[] bytes;
        T value;
        try
        {
            bytes = File.ReadAllBytes(fileName);
            value = Parse(bytes);
        }
        catch (Exception ex) when (ConfigFailure.IsRead(ex))
        {
            State(fileName).MarkFailed();
            DiagnosticLog.Warn("Config.Load", $"読込失敗: {ConfigFileState.KindOf(fileName)} {ex.GetType().Name}");
            return new(new T(), ConfigLoadStatus.Failed, fileName, ProtectOriginal && File.Exists(backupName), ex);
        }

        State(fileName).MarkLoaded(bytes);
        try
        {
            if (ProtectOriginal)
            {
                ConfigStore.WriteAtomic(backupName, bytes);
            }
        }
        catch (Exception ex) when (ConfigFailure.IsSave(ex))
        {
            // バックアップを作成できなくても読めたデータは使える。利用者へは結果の BackupError で通知させる
            DiagnosticLog.Warn("Config.Backup", $"バックアップ更新失敗: {ConfigFileState.KindOf(fileName)} {ex.GetType().Name}");
            return new(value, ConfigLoadStatus.Loaded, fileName, false, null) { BackupError = ex };
        }
        return new(value, ConfigLoadStatus.Loaded, fileName, false, null);
    }

    /// <summary>
    /// バックアップから復元する。読めない本体は <c>.broken-&lt;日時&gt;</c> へ保全してから置換し、保存を再開する。
    /// バックアップを読めない場合と保全できない場合は例外を送出し、本体を変えない。
    /// </summary>
    public T RestoreFromBackup(string? baseName = null)
    {
        string fileName = FileName(baseName);
        using var mutex = ConfigStore.Lock(fileName);
        byte[] bytes = File.ReadAllBytes(ConfigFileState.BackupName(fileName));
        T value = Parse(bytes);
        if (File.Exists(fileName))
        {
            File.Move(fileName, ConfigFileState.BrokenName(fileName));
        }
        ConfigStore.WriteAtomic(fileName, bytes);
        State(fileName).MarkLoaded(bytes);
        DiagnosticLog.Info("Config.Restore", $"バックアップから復元: {ConfigFileState.KindOf(fileName)}");
        return value;
    }

    T Parse(byte[] bytes)
    {
        try
        {
            using var stream = new MemoryStream(bytes);
            return ConfigStore.DeserializeFromStream<T>(stream);
        }
        catch (Exception ex) when (ConfigFailure.IsXml(ex))
        {
            if (legacyParser is null || !IsLegacyText(bytes))
            {
                throw;
            }
            return legacyParser(bytes) ?? throw new InvalidDataException("旧形式の設定を読めない", ex);
        }
    }

    /// <summary>
    /// 旧形式 (<c>キー = 値</c>の行) の内容か。XMLの開始で始まる内容 (破損したXML) は旧形式とみなさない。
    /// </summary>
    internal static bool IsLegacyText(byte[] bytes)
    {
        string text = Encoding.UTF8.GetString(bytes).TrimStart('﻿', ' ', '\t', '\r', '\n');
        if (text.Length == 0 || text[0] == '<')
        {
            return false;
        }
        return text.Split('\n').Any(line => line.Contains(" = ", StringComparison.Ordinal));
    }
}

/// <summary>
/// 保存データのファイルごとの読込状態。保存の可否と、保存前にバックアップへ写してよい内容かの判定に使う。
/// </summary>
public sealed class ConfigFileState
{
    internal bool LoadAttempted { get; private set; }
    bool blocked;
    internal byte[]? LastGood { get; set; }
    internal HashSet<ConfigSaveFailureKind> NotifiedFailures { get; } = [];

    /// <summary>バックアップのファイル名</summary>
    public static string BackupName(string fileName) => fileName + ".bak";

    /// <summary>読めない本体の保全先。同じ時刻の保全が重ならないようミリ秒まで付ける</summary>
    public static string BrokenName(string fileName) =>
        fileName + ".broken-" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff", System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>ログと通知で使う、ファイル名のうち最初の<c>.</c>以降 (例: <c>.cmd.cfg</c>)</summary>
    public static string KindOf(string fileName)
    {
        string name = Path.GetFileName(fileName);
        int dot = name.IndexOf('.', StringComparison.Ordinal);
        return dot < 0 ? name : name[dot..];
    }

    internal void MarkLoaded(byte[]? content)
    {
        LoadAttempted = true;
        blocked = false;
        LastGood = content;
    }

    internal void MarkFailed()
    {
        LoadAttempted = true;
        blocked = true;
        LastGood = null;
    }

    /// <summary>
    /// 保存で本体を置換する前の処理。保存停止中なら例外を送出する。直前の本体が正常なら
    /// バックアップへ写し、外部で壊れていれば別名へ保全する。どちらかに失敗したら例外を送出し、本体を置換させない。
    /// ランタイムデータの保存では呼ばない。
    /// </summary>
    internal void BeforeReplace(string fileName, Func<byte[], object> validate)
    {
        if (blocked)
        {
            throw new ConfigSaveBlockedException(
                $"設定ファイル({KindOf(fileName)})を読み込めなかったため、原本の上書きを防ぐために保存を停止しています。" +
                "バックアップから復元するか、ファイルを直してから再起動してください。");
        }
        if (!File.Exists(fileName))
        {
            return;
        }
        byte[] current = File.ReadAllBytes(fileName);
        if (LastGood is { } lastGood && current.AsSpan().SequenceEqual(lastGood))
        {
            ConfigStore.WriteAtomic(BackupName(fileName), current);
            return;
        }
        // 最後の正常な読込・保存から外部で変わった本体は、読めるときだけバックアップへ写す
        bool valid;
        try
        {
            validate(current);
            valid = true;
        }
        catch (Exception ex) when (ConfigFailure.IsRead(ex))
        {
            valid = false;
        }
        if (valid)
        {
            ConfigStore.WriteAtomic(BackupName(fileName), current);
        }
        else
        {
            File.Copy(fileName, BrokenName(fileName));
            DiagnosticLog.Warn("Config.Save", $"外部で壊れた本体を別名で保全: {KindOf(fileName)}");
        }
    }

}
