using System.IO;
using System.Xml;
using Launcher.Infrastructure;

namespace Launcher.Core;

[System.Diagnostics.DebuggerDisplay("Count = {Count}")]
public sealed class CommandList : ConfigStore, ICloneable
{
    public List<Command> Commands { get; set; } = new List<Command>();

    /// <summary>
    /// 要素数
    /// </summary>
    public int Count
    {
        get { return Commands.Count; }
    }

    /// <summary>
    /// 複製の作成
    /// </summary>
    public CommandList Clone()
    {
        CommandList copy = (CommandList)MemberwiseClone();
        copy.Commands = Commands.ConvertAll(c => (Command)c.Clone());
        return copy;
    }

    #region ICloneable メンバ

    object ICloneable.Clone()
    {
        return Clone();
    }

    #endregion

    #region Serialize/Deserialize

    /// <summary>
    /// 書き込み
    /// </summary>
    public new void Serialize(string ext)
    {
        SerializeTo(ext, null);
    }

    /// <summary>
    /// 書き込み (保存先のベースファイル名を指定する)
    /// </summary>
    public void SerializeTo(string ext, string? baseName)
    {
        Commands.Sort();
        SerializeToFile(Store(ext).FileName(baseName));
    }

    /// <summary>
    /// 保存済みの一覧へコマンドを追加して保存する (「送る」からの登録)。
    /// 一覧を読み込めない場合は追加も保存もせず、その読込結果を返す。
    /// </summary>
    public static ConfigLoadResult<CommandList> AddAndSave(string ext, Command command, string? baseName = null)
    {
        var result = Load(ext, baseName);
        if (result.Status == ConfigLoadStatus.Failed)
        {
            return result;
        }
        result.Value.Add(command);
        result.Value.SerializeTo(ext, baseName);
        return result;
    }

    /// <summary>
    /// 読み込み
    /// </summary>
    public static ConfigLoadResult<CommandList> Load(string ext, string? baseName = null) => Store(ext).Load(baseName);

    /// <summary>
    /// 保存データの読込・復元。XMLとして読めない旧形式のコマンド一覧も読む
    /// </summary>
    public static ConfigFile<CommandList> Store(string ext) => new(ext, LoadLegacy);

    static CommandList LoadLegacy(byte[] content)
    {
        using var stream = new MemoryStream(content);
        return LoadFrom(new LegacyConfigReader(stream, false));
    }

    #endregion

    /// <summary>
    /// 後方互換性のための処理
    /// </summary>
    public static CommandList LoadFrom(LegacyConfigReader reader)
    {
        var list = new CommandList();
        int n;
        if (reader.ContainsKey("_") &&
            int.TryParse(reader.Indirect("_"), out n))
        {
            for (int i = 0; i < n; i++)
            {
                string key = i.ToString("d3");
                Command cmd = Command.LoadFrom(null,
                    reader.EscapedString(key));
                list.Commands.Add(cmd);
            }
        }
        else
        {
            foreach (string key in reader.Keys)
            {
                Command cmd = Command.LoadFrom(key,
                    reader.EscapedString(key));
                list.Commands.Add(cmd);
            }
        }
        list.Commands.Sort();
        return list;
    }

    /// <summary>
    /// 該当しそうなコマンドをリストアップして返す。
    /// </summary>
    public IEnumerable<Command> FindMatch(string input, Config config)
    {
        if (string.IsNullOrEmpty(input))
        {
            return [.. Commands];
        }
        return Commands
            .Select(x => new { Command = x, Score = x.GetMatchScore(input, config) })
            .Where(x => 0 < x.Score)
            .OrderByDescending(x => x.Score)
            .Select(x => x.Command);
    }

    /// <summary>
    /// 追加
    /// </summary>
    public void Add(Command command)
    {
        Commands.Add(command);
        Commands.Sort();
    }
}
