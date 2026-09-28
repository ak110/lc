using System.Text;
using System.Xml;

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
/// 1種類の保存データの読込・復元を担う。読込の状態・直前の正常な内容はファイルごとに
/// <see cref="ConfigFileState"/>へ記録し、保存時の判定 (<see cref="ConfigStore.SerializeToFile"/>) と共有する。
/// </summary>
public sealed class ConfigFile<T> where T : ConfigStore, new()
{
    readonly string ext;
    readonly Func<byte[], T?>? legacyParser;

    /// <param name="ext">拡張子。1文字目は <c>.</c></param>
    /// <param name="legacyParser">旧形式の読込。旧形式と判定できない内容では呼ばれない</param>
    public ConfigFile(string ext, Func<byte[], T?>? legacyParser = null)
    {
        this.ext = ext;
        this.legacyParser = legacyParser;
    }

    /// <summary>対象ファイルの完全パス</summary>
    public string FileName(string? baseName = null) => (baseName ?? ConfigStore.DefaultBaseName) + ext;

    /// <summary>
    /// 読み込む。成功したらその内容でバックアップを更新する。失敗したファイルへの保存は以後止まる。
    /// </summary>
    public ConfigLoadResult<T> Load(string? baseName = null)
    {
        string fileName = FileName(baseName);
        string backupName = ConfigFileState.BackupName(fileName);
        using var mutex = ConfigStore.Lock(fileName);

        if (!File.Exists(fileName))
        {
            if (File.Exists(backupName))
            {
                // 本体だけが消えた状態は初回と区別し、バックアップを上書きさせない
                ConfigFileState.MarkFailed(fileName, Parse);
                DiagnosticLog.Warn("Config.Load", $"本体が無くバックアップだけがある: {ConfigFileState.KindOf(fileName)}");
                return new(new T(), ConfigLoadStatus.Failed, fileName, true, new FileNotFoundException("本体が見つからない"));
            }
            ConfigFileState.MarkLoaded(fileName, null, Parse);
            return new(new T(), ConfigLoadStatus.NotFound, fileName, false, null);
        }

        byte[] bytes;
        T value;
        try
        {
            bytes = File.ReadAllBytes(fileName);
            value = Parse(bytes);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException
            or InvalidDataException or XmlException or FormatException or IndexOutOfRangeException or KeyNotFoundException)
        {
            ConfigFileState.MarkFailed(fileName, Parse);
            DiagnosticLog.Warn("Config.Load", $"読込失敗: {ConfigFileState.KindOf(fileName)} {ex.GetType().Name}");
            return new(new T(), ConfigLoadStatus.Failed, fileName, File.Exists(backupName), ex);
        }

        ConfigFileState.MarkLoaded(fileName, bytes, Parse);
        try
        {
            ConfigFileState.WriteAtomic(backupName, bytes);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // バックアップを作成できなくても読めたデータは使える。保存時に改めて作成を試みる
            DiagnosticLog.Warn("Config.Backup", $"バックアップ更新失敗: {ConfigFileState.KindOf(fileName)} {ex.GetType().Name}");
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
        ConfigFileState.WriteAtomic(fileName, bytes);
        ConfigFileState.MarkLoaded(fileName, bytes, Parse);
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
        catch (Exception ex) when (ex is InvalidOperationException or XmlException)
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
public static class ConfigFileState
{
    sealed class Entry
    {
        public bool Blocked;
        public byte[]? LastGood;
        public required Func<byte[], object> Validate;
    }

    static readonly object lockObject = new();
    static readonly Dictionary<string, Entry> entries = new(StringComparer.OrdinalIgnoreCase);

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

    internal static void MarkLoaded<T>(string fileName, byte[]? content, Func<byte[], T> validate)
        where T : notnull
    {
        lock (lockObject)
        {
            entries[fileName] = new Entry { Blocked = false, LastGood = content, Validate = b => validate(b) };
        }
    }

    internal static void MarkFailed<T>(string fileName, Func<byte[], T> validate)
        where T : notnull
    {
        lock (lockObject)
        {
            entries[fileName] = new Entry { Blocked = true, LastGood = null, Validate = b => validate(b) };
        }
    }

    /// <summary>そのファイルへの保存が止まっているか</summary>
    public static bool IsBlocked(string fileName)
    {
        lock (lockObject)
        {
            return entries.TryGetValue(fileName, out var entry) && entry.Blocked;
        }
    }

    /// <summary>
    /// 保存で本体を置換する前の処理。保存停止中なら例外を送出する。直前の本体が正常なら
    /// バックアップへ写し、外部で壊れていれば別名へ保全する。どちらかに失敗したら例外を送出し、本体を置換させない。
    /// <see cref="ConfigFile{T}"/>で読み込んでいないファイル (らんちゃ.datなど) は何もしない。
    /// </summary>
    internal static void BeforeReplace(string fileName)
    {
        Entry? entry;
        lock (lockObject)
        {
            entries.TryGetValue(fileName, out entry);
        }
        if (entry is null)
        {
            return;
        }
        if (entry.Blocked)
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
        if (entry.LastGood is { } lastGood && current.AsSpan().SequenceEqual(lastGood))
        {
            WriteAtomic(BackupName(fileName), current);
            return;
        }
        // 最後の正常な読込・保存から外部で変わった本体は、読めるときだけバックアップへ写す
        bool valid;
        try
        {
            entry.Validate(current);
            valid = true;
        }
        catch (Exception ex) when (ex is InvalidOperationException or XmlException or InvalidDataException
            or FormatException or IndexOutOfRangeException or KeyNotFoundException)
        {
            valid = false;
        }
        if (valid)
        {
            WriteAtomic(BackupName(fileName), current);
        }
        else
        {
            File.Copy(fileName, BrokenName(fileName));
            DiagnosticLog.Warn("Config.Save", $"外部で壊れた本体を別名で保全: {KindOf(fileName)}");
        }
    }

    internal static void AfterReplace(string fileName, byte[] content)
    {
        lock (lockObject)
        {
            if (entries.TryGetValue(fileName, out var entry))
            {
                entry.LastGood = content;
            }
        }
    }

    /// <summary>
    /// 一時ファイルへ書いてから置換する。
    /// </summary>
    internal static void WriteAtomic(string fileName, byte[] content)
    {
        string tmp = fileName + ".tmp";
        File.WriteAllBytes(tmp, content);
        try
        {
            File.Move(tmp, fileName, true);
        }
        catch
        {
            IoFailureHandler.IgnoreIoErrors(() => File.Delete(tmp));
            throw;
        }
    }
}
