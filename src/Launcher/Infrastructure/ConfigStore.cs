using System.Xml.Serialization;

namespace Launcher.Infrastructure;

/// <summary>
/// シリアライズ可能なクラスの基底クラス。
/// </summary>
public class ConfigStore
{
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

    internal byte[] SerializeToBytes()
    {
        using var buffer = new MemoryStream();
        new XmlSerializer(GetType()).Serialize(buffer, this);
        return buffer.ToArray();
    }

    // 本体、バックアップ、復元で同じ再試行と一時ファイルの後始末を使う。
    internal static void WriteAtomic(string fileName, byte[] content)
    {
        string temporary = fileName + ".tmp";
        try
        {
            File.WriteAllBytes(temporary, content);
            MoveFileWithRetry(temporary, fileName);
        }
        catch
        {
            IoFailureHandler.IgnoreIoErrors(() => File.Delete(temporary));
            throw;
        }
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
    /// ファイルからオブジェクトを復元
    /// </summary>
    public static T DeserializeFromFile<T>(string fileName)
    {
        using var mutex = Lock(fileName);
        using FileStream stream = File.OpenRead(fileName);
        return DeserializeFromStream<T>(stream);
    }

    /// <summary>
    /// ストリームからオブジェクトを復元
    /// </summary>
    public static T DeserializeFromStream<T>(Stream stream)
    {
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

    internal static IDisposable Lock(string fileName)
    {
        string mutexName = fileName.ToLowerInvariant().Replace('\\', '/');
        var mutex = new Mutex(false, mutexName);
        try
        {
            if (!mutex.WaitOne(30000))
            {
                throw new TimeoutException("設定ファイルのロック取得がタイムアウトした");
            }
        }
        catch (AbandonedMutexException)
        {
            // 前のプロセスが異常終了しても、このスレッドが取得した排他の下で原本を検査する。
        }
        catch
        {
            mutex.Dispose();
            throw;
        }
        return new MutexLock(mutex);
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
