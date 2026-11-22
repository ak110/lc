using System.Xml.Serialization;

namespace Launcher.Infrastructure;

public class ConfigStore
{
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
                IoFailureHandler.IgnoreIoErrors(() => File.Delete(source));
                throw;
            }
        }
    }

    public static T DeserializeFromStream<T>(Stream stream)
    {
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
        }
        catch
        {
            mutex.Dispose();
            throw;
        }
        return new MutexLock(mutex);
    }

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
