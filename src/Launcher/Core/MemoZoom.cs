namespace Launcher.Core;

/// <summary>
/// メモパッド本文の表示倍率の段階を計算する。
/// 刻みと範囲はWindows標準のメモ帳にそろえ、10%刻みで10%〜500%とする。
/// </summary>
public static class MemoZoom
{
    /// <summary>既定の倍率 (100%)</summary>
    public const float Default = 1f;

    // 倍率を10%単位の段数で扱う。10%が1段、500%が50段に当たる。
    const int StepsPerUnit = 10;
    const int MinSteps = 1;
    const int MaxSteps = 50;

    /// <summary>
    /// 現在の倍率から、拡大なら次に大きい10%単位の倍率、縮小なら次に小さい10%単位の倍率を返す。
    /// Ctrl+ホイールで10%単位から外れた倍率も、操作の方向にある最も近い10%単位へ進める。
    /// 範囲の端で同じ方向へ操作した場合はnullを返し、倍率を変えない。
    /// Ctrl+ホイールで範囲外に達した倍率は、範囲へ近づく方向の操作だけを受け付ける。
    /// </summary>
    public static float? Step(float current, bool zoomIn)
    {
        // RichEditから読んだ倍率は1.1が10.9999のように段数の直前へずれるため、許容幅を持たせて段数へ換算する。
        const double Tolerance = 0.01;
        double steps = current * StepsPerUnit;
        int next = zoomIn
            ? (int)Math.Floor(steps + Tolerance) + 1
            : (int)Math.Ceiling(steps - Tolerance) - 1;
        next = Math.Clamp(next, MinSteps, MaxSteps);
        if (zoomIn ? next <= steps + Tolerance : next >= steps - Tolerance) return null;
        return next / (float)StepsPerUnit;
    }
}
