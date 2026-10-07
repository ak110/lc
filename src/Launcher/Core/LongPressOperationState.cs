namespace Launcher.Core;

/// <summary>
/// 長押し操作の世代と、対応するクリックの抑止状態を管理する。
/// </summary>
public sealed class LongPressOperationState
{
    long currentOperationId;
    bool suppressNextClick;

    /// <summary>
    /// 新しい長押し操作を開始し、その操作を識別する値を返す。
    /// </summary>
    public long Begin()
    {
        suppressNextClick = false;
        return ++currentOperationId;
    }

    /// <summary>
    /// 現在の長押し操作を無効化する。
    /// 既に発火した操作のクリック抑止状態は維持する。
    /// </summary>
    public void Cancel()
    {
        currentOperationId++;
    }

    /// <summary>
    /// 指定した操作が現在の操作なら、長押し発火として記録する。
    /// </summary>
    public bool TryFire(long operationId)
    {
        if (operationId != currentOperationId)
        {
            return false;
        }

        suppressNextClick = true;
        return true;
    }

    /// <summary>
    /// 発火済みの長押しに対応するクリックを1回だけ抑止するかを返す。
    /// </summary>
    public bool ConsumeClickSuppression()
    {
        bool result = suppressNextClick;
        suppressNextClick = false;
        return result;
    }
}
