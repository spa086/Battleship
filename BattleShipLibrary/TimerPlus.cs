namespace BattleshipLibrary;

//todo possibility of infinite timers via configuration file.
public class TimerPlus : IDisposable
{
    public TimerPlus(TimerCallback callback, object state, TimeSpan dueTime, TimeSpan period)
    {
        timer = new System.Threading.Timer(Callback, state, dueTime, period);
        realCallback = callback;
        this.period = period;
        next = DateTime.Now.Add(dueTime);
    }

    public TimeSpan DueTime => next - DateTime.Now;

    public void Dispose()
    {
        timer.Dispose();
        GC.SuppressFinalize(this);
    }

    private void Callback(object? state)
    {
        next = DateTime.Now.Add(period);
        realCallback(state);
    }

    private readonly TimerCallback realCallback;
    private readonly System.Threading.Timer timer;
    private readonly TimeSpan period;
    private DateTime next;
}