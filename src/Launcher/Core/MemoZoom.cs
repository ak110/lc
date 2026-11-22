namespace Launcher.Core;

/// <summary>
/// メモパッド本文の表示倍率の段階を計算する。
/// 刻みと範囲はWindows標準のメモ帳にそろえ、10%刻みで10%〜500%とする。
/// </summary>
public static class MemoZoom
{
    public const float Default = 1f;

    const int StepsPerUnit = 10;
    const int MinSteps = 1;
    const int MaxSteps = 50;

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
