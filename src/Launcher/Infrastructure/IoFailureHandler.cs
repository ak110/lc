namespace Launcher.Infrastructure;

public static class IoFailureHandler
{
    public static void IgnoreIoErrors(Action action)
    {
        try
        {
            action();
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    public static T IgnoreIoErrors<T>(Func<T> action, T defaultValue)
    {
        try
        {
            return action();
        }
        catch (IOException)
        {
            return defaultValue;
        }
        catch (UnauthorizedAccessException)
        {
            return defaultValue;
        }
    }
}
