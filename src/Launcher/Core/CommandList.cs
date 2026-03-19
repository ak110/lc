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
        Commands.Sort();
        base.Serialize(ext);
    }

    /// <summary>
    /// 読み込み
    /// </summary>
    public static CommandList Deserialize(string ext)
    {
        try
        {
            return Deserialize<CommandList>(ext);
        }
        catch (Exception ex) when (ex is InvalidOperationException or XmlException or IOException)
        {
            // XMLデシリアライズ失敗時はレガシー形式での読み込みを試みる
            string name = DefaultBaseName + ext;
            if (File.Exists(name))
            {
                LegacyConfigReader reader = new LegacyConfigReader(name);
                try
                {
                    return CommandList.LoadFrom(reader);
                }
                catch (Exception ex2) when (ex2 is InvalidOperationException or IOException or FormatException)
                {
                }
            }
            return new CommandList();
        }
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
