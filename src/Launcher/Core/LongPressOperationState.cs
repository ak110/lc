namespace Launcher.Core;

public sealed class LongPressOperationState
{
    enum Phase { Idle, Pressing, SuppressClick }

    long currentOperationId;
    Phase phase;

    public long Begin()
    {
        phase = Phase.Pressing;
        return ++currentOperationId;
    }

    public void Cancel()
    {
        if (phase == Phase.Pressing) phase = Phase.Idle;
    }

    public bool TryFire(long operationId)
    {
        if (phase != Phase.Pressing || operationId != currentOperationId)
        {
            return false;
        }

        phase = Phase.SuppressClick;
        return true;
    }

    public bool ConsumeClickSuppression()
    {
        if (phase != Phase.SuppressClick) return false;
        phase = Phase.Idle;
        return true;
    }
}
