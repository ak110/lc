using System.Text;

namespace Launcher.Infrastructure;

public enum ConfigLoadStatus
{
    Loaded,

    NotFound,

    Failed,
}

public sealed record ConfigLoadResult<T>(
    T Value, ConfigLoadStatus Status, string FileName, bool BackupAvailable, Exception? Error)
{
    public string Kind => ConfigFileState.KindOf(FileName);

    public Exception? BackupError { get; init; }
}

public sealed class ConfigSaveBlockedException : IOException
{
    public ConfigSaveBlockedException(string message)
        : base(message)
    {
    }
}

public sealed class ConfigFile<T> where T : ConfigStore, new()
{
    readonly string ext;
    readonly Func<byte[], T?>? legacyParser;
    readonly Dictionary<string, ConfigFileState> states = new(StringComparer.OrdinalIgnoreCase);

    public bool ProtectOriginal { get; }

    string ReadFailureNextAction => ProtectOriginal
        ? "保存は停止中。画面の読込失敗の案内に従い、バックアップから復元するかファイルを直してから再起動する"
        : "対応不要（初期値で処理を継続し、次の保存で再作成する）。繰り返す場合はらんちゃのフォルダーの読み書き権限を確認する";

    public ConfigFile(string ext, Func<byte[], T?>? legacyParser = null, bool protectOriginal = true)
    {
        this.ext = ext;
        this.legacyParser = legacyParser;
        ProtectOriginal = protectOriginal;
    }

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
                if (!state.LoadAttempted)
                {
                    Load(baseName);
                }
                state.BeforeReplace(fileName, ext, bytes => Parse(bytes));
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
        DiagnosticLog.Warn("Config.Save", $"保存失敗: {ext} {kind} {error.GetType().Name}", "画面の保存失敗の案内に従う（同じ種類の失敗は最初の1回だけ画面へ通知する）");
        var notified = State(fileName).NotifiedFailures;
        lock (notified)
        {
            return notified.Add(kind) ? new ConfigSaveFailure(ext, kind) : null;
        }
    }

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
            DiagnosticLog.Warn("Config.Load", $"読込失敗: {ext} {ex.GetType().Name}", ReadFailureNextAction);
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
                State(fileName).MarkFailed();
                DiagnosticLog.Warn("Config.Load", $"本体が無くバックアップだけがある: {ext}", "保存は停止中。画面の読込失敗の案内に従い、バックアップから復元する");
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
            DiagnosticLog.Warn("Config.Load", $"読込失敗: {ext} {ex.GetType().Name}", ReadFailureNextAction);
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
            DiagnosticLog.Warn("Config.Backup", $"バックアップ更新失敗: {ext} {ex.GetType().Name}",
                "読み込んだ内容で処理を継続した。画面の案内に従い、らんちゃのフォルダーとバックアップファイル（.bak）へ書き込めるか確認する");
            return new(value, ConfigLoadStatus.Loaded, fileName, false, null) { BackupError = ex };
        }
        return new(value, ConfigLoadStatus.Loaded, fileName, false, null);
    }

    public T RestoreFromBackup(string? baseName = null)
    {
        string fileName = FileName(baseName);
        using var mutex = ConfigStore.Lock(fileName);
        T value;
        try
        {
            byte[] bytes = File.ReadAllBytes(ConfigFileState.BackupName(fileName));
            value = Parse(bytes);
            if (File.Exists(fileName))
            {
                File.Move(fileName, ConfigFileState.BrokenName(fileName));
            }
            ConfigStore.WriteAtomic(fileName, bytes);
            State(fileName).MarkLoaded(bytes);
        }
        catch (Exception ex) when (ConfigFailure.IsRead(ex))
        {
            DiagnosticLog.Warn("Config.Restore", $"復元失敗: {ext} {ex.GetType().Name}",
                "保存の停止は続いている。画面の案内に従い、バックアップ（.bak）と本体の内容、フォルダーの読み書き権限を確認してから再起動する");
            throw;
        }
        DiagnosticLog.Info("Config.Restore", $"バックアップから復元: {ext}");
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

public sealed class ConfigFileState
{
    internal bool LoadAttempted { get; private set; }
    bool blocked;
    internal byte[]? LastGood { get; set; }
    internal HashSet<ConfigSaveFailureKind> NotifiedFailures { get; } = [];

    public static string BackupName(string fileName) => fileName + ".bak";

    public static string BrokenName(string fileName) =>
        fileName + ".broken-" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff", System.Globalization.CultureInfo.InvariantCulture);

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

    internal void BeforeReplace(string fileName, string ext, Func<byte[], object> validate)
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
            DiagnosticLog.Warn("Config.Save", $"外部で壊れた本体を別名で保全: {ext}",
                "対応不要（壊れた本体を同じフォルダーへ、ファイル名の末尾に.broken-と日時を付けて保全し、保存を継続した）。保全したファイルは内容を確認して不要なら削除してよい");
        }
    }

}
