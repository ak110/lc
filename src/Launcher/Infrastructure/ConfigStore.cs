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
    /// デフォルトのディレクトリパス＋拡張子を除いたベースファイル名を取得。
    /// </summary>
    public static string DefaultBaseName
    {
        get
        {
            string path = Path.ChangeExtension(
                Environment.ProcessPath, null)!;
#if DEBUG
            if (path.EndsWith(".vshost"))
            {
                path = path.Substring(0, path.Length - 7);
            }
#endif
            return path;
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
        using (var mutex = Lock(fileName))
        {
            string tmpFileName = fileName + ".tmp";
            using (FileStream stream = File.Create(tmpFileName))
            {
                XmlSerializer s = new XmlSerializer(GetType());
                s.Serialize(stream, this);
            }
            /*
			try {
				File.Replace(tmpFileName, fileName, null);
			} catch (IOException) {
				if (File.Exists(fileName)) {
					File.Copy(tmpFileName, fileName, true);
					File.Delete(tmpFileName);
				} else {
					File.Move(tmpFileName, fileName);
				}
			}
			/*/
            File.Copy(tmpFileName, fileName, true);
            File.Delete(tmpFileName);
            //*/
        }
    }

    /// <summary>
    /// オブジェクトを文字列化
    /// </summary>
    /// <returns>文字列化されたデータ</returns>
    public string SerializeToString()
    {
        using (var stream = new StringWriter())
        {
            XmlSerializer s = new XmlSerializer(GetType());
            s.Serialize(stream, this);
            return stream.GetStringBuilder().ToString();
        }
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
        using (var mutex = Lock(fileName))
        {
            using (FileStream stream = File.OpenRead(fileName))
            {
                XmlSerializer formatter = new XmlSerializer(typeof(T));
                return (T)formatter.Deserialize(stream)!;
            }
        }
    }

    /// <summary>
    /// 文字列からオブジェクトを復元
    /// </summary>
    /// <param name="data">文字列化されたデータ</param>
    /// <returns>復元されたデータ</returns>
    public static T DeserializeFromString<T>(string data)
    {
        using (var stream = new StringReader(data))
        {
            XmlSerializer formatter = new XmlSerializer(typeof(T));
            return (T)formatter.Deserialize(stream)!;
        }
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
                throw new TimeoutException($"設定ファイルのロック取得がタイムアウトしました: {fileName}");
            }
            return new MutexLock(mutex);
        }
    }

    /// <summary>
    /// Mutexの取得・解放を安全に行うラッパー。
    /// ReleaseMutex()を呼んでからClose()することで、
    /// 他プロセスでAbandonedMutexExceptionが発生するのを防ぐ。
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
            if (mutex != null)
            {
                mutex.ReleaseMutex();
                mutex.Close();
                mutex = null;
            }
        }
    }
}
