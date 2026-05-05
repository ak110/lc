using System.Diagnostics;
using System.Xml.Serialization;

namespace Launcher.Infrastructure;

/// <summary>
/// シリアライズ可能なクラスの基底クラス。
/// </summary>
[Serializable]
public class ConfigStore
{
    static readonly object lockObject = new();

    /// <summary>
    /// 既定のディレクトリパス＋拡張子を除いたベースファイル名を取得する。
    /// </summary>
    public static string DefaultBaseName
    {
        get
        {
            return Path.ChangeExtension(Environment.ProcessPath, null)!;
        }
    }

    /// <summary>
    /// オブジェクトを保存
    /// </summary>
    /// <param name="ext">拡張子。１文字目は . にしておく。</param>
    protected void Serialize(string ext)
    {
        Serialize(ext, DefaultBaseName);
    }

    /// <summary>
    /// オブジェクトを保存
    /// </summary>
    /// <param name="baseName">ディレクトリパス＋拡張子を除いたベースファイル名</param>
    /// <param name="ext">拡張子。１文字目は . にしておく。</param>
    protected void Serialize(string ext, string baseName)
    {
        Debug.Assert(ext[0] == '.');
        string fileName = baseName + ext;
        SerializeToFile(fileName);
    }

    /// <summary>
    /// オブジェクトを保存
    /// </summary>
    /// <param name="fileName">保存するファイル名</param>
    public void SerializeToFile(string fileName)
    {
        using var mutex = Lock(fileName);
        string tmpFileName = fileName + ".tmp";
        using (FileStream stream = File.Create(tmpFileName))
        {
            XmlSerializer s = new XmlSerializer(GetType());
            s.Serialize(stream, this);
        }
        // 同一ボリューム上のMoveは原子的なリネーム(MoveFileEx)になるため、
        // 書き込み途中のクラッシュでファイルが破損するリスクを回避できる。
        // 外部プロセス(アンチウイルス等)による一時的なファイルロックに備えてリトライする。
        MoveFileWithRetry(tmpFileName, fileName);
    }

    /// <summary>
    /// リトライ付きファイル移動。最終失敗時はtmpファイルを削除してから例外を伝播する。
    /// </summary>
    static void MoveFileWithRetry(string source, string dest)
    {
        const int maxRetries = 2;
        const int retryDelayMs = 50;
        for (int i = 0; ; i++)
        {
            try
            {
                File.Move(source, dest, true);
                return;
            }
            catch (Exception ex) when (i < maxRetries && (ex is IOException || ex is UnauthorizedAccessException))
            {
                Thread.Sleep(retryDelayMs);
            }
            catch
            {
                // 最終失敗時はtmpファイルを残さない
                IoFailureHandler.IgnoreIoErrors(() => File.Delete(source));
                throw;
            }
        }
    }

    /// <summary>
    /// オブジェクトをXML文字列にシリアライズする。
    /// </summary>
    /// <returns>シリアライズされたXML文字列</returns>
    public string SerializeToString()
    {
        using var stream = new StringWriter();
        XmlSerializer s = new XmlSerializer(GetType());
        s.Serialize(stream, this);
        return stream.GetStringBuilder().ToString();
    }

    /// <summary>
    /// オブジェクトを復元
    /// </summary>
    /// <param name="ext">拡張子。１文字目は . にしておく。</param>
    /// <returns>復元されたデータ</returns>
    protected static T Deserialize<T>(string ext)
    {
        return Deserialize<T>(ext, DefaultBaseName);
    }

    /// <summary>
    /// オブジェクトを復元
    /// </summary>
    /// <param name="baseName">ディレクトリパス＋拡張子を除いたベースファイル名</param>
    /// <param name="ext">拡張子。１文字目は . にしておく。</param>
    /// <returns>復元されたデータ</returns>
    protected static T Deserialize<T>(string ext, string baseName)
    {
        Debug.Assert(ext[0] == '.');
        string fileName = baseName + ext;
        return DeserializeFromFile<T>(fileName);
    }

    /// <summary>
    /// ファイルからオブジェクトを復元
    /// </summary>
    public static T DeserializeFromFile<T>(string fileName)
    {
        using var mutex = Lock(fileName);
        using FileStream stream = File.OpenRead(fileName);
        XmlSerializer formatter = new XmlSerializer(typeof(T));
        return (T)formatter.Deserialize(stream)!;
    }

    /// <summary>
    /// XML文字列からオブジェクトをデシリアライズする。
    /// </summary>
    /// <param name="data">シリアライズされたXML文字列</param>
    /// <returns>復元されたオブジェクト</returns>
    public static T DeserializeFromString<T>(string data)
    {
        using var stream = new StringReader(data);
        XmlSerializer formatter = new XmlSerializer(typeof(T));
        return (T)formatter.Deserialize(stream)!;
    }

    static MutexLock Lock(string fileName)
    {
        lock (lockObject)
        {
            string mutexName = fileName.ToLower().Replace('\\', '/');
            var mutex = new Mutex(false, mutexName);
            if (!mutex.WaitOne(30000))
            {
                mutex.Close();
                throw new TimeoutException($"設定ファイルのロック取得がタイムアウトした: {fileName}");
            }
            return new MutexLock(mutex);
        }
    }

    /// <summary>
    /// Mutexの取得・解放を安全に行うラッパー。
    /// ReleaseMutex()の後にClose()することで他プロセスでのAbandonedMutexExceptionを防ぐ。
    /// </summary>
    private sealed class MutexLock : IDisposable
    {
        private Mutex? mutex;

        public MutexLock(Mutex mutex)
        {
            this.mutex = mutex;
        }

        public void Dispose()
        {
            if (mutex is not null)
            {
                mutex.ReleaseMutex();
                mutex.Close();
                mutex = null;
            }
        }
    }
}
