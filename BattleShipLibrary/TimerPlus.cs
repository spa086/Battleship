namespace BattleshipLibrary;

//todo possibility of infinite timers via configuration file (for manual testing)
public class TimerWithDueTime : IDisposable
{
    public TimerWithDueTime(Action callback, TimeSpan dueTime)
    {
        timer = new System.Threading.Timer(_ => { callback(); }, new object(), dueTime, Timeout.InfiniteTimeSpan);
        next = DateTime.Now.Add(dueTime);
    }

    public TimeSpan DueTime => next - DateTime.Now;

    public void Dispose()
    {
        timer.Dispose();
        GC.SuppressFinalize(this);
    }

    private readonly System.Threading.Timer timer;
    private readonly DateTime next;
}