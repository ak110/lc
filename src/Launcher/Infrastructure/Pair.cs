namespace Launcher.Infrastructure;

/// <summary>
/// std::pair的な。
/// </summary>
/// <typeparam name="FirstType">型引数1</typeparam>
/// <typeparam name="SecondType">型引数2</typeparam>
public struct Pair<FirstType, SecondType>
{
    /// <summary>
    /// 1つ目。
    /// </summary>
    public FirstType First;
    /// <summary>
    /// 2つ目。
    /// </summary>
    public SecondType Second;

    /// <summary>
    /// コンストラクタ。
    /// </summary>
    /// <param name="f">1つ目。</param>
    /// <param name="s">2つ目。</param>
    public Pair(FirstType f, SecondType s)
    {
        First = f;
        Second = s;
    }
}
