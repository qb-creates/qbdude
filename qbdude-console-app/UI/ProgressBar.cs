using System.Text;

namespace qbdude.ui;

/// <summary>
/// Will create a progress bar UI element in the console. Nothing should be printed to the to the console
/// while the progress bar is active. Contains a static "Start" method that will generate a ProgressBar
/// object and return it. Progress bar should be wrapped in a using block or "Disposed" when it is no longer
/// needed.
/// </summary>
public sealed class ProgressBar : IDisposable
{
    // Delay in milliseconds before starting the progress bar.
    private const int START_TIMER_DELAY = 1000;

    // How often the timer text will be updated in milliseconds
    private const int TIMER_UPDATE_FREQUENCY = 10;

    // The ammount of spaces away from the progress bar percentage the timer text should be.
    private const int TIMER_TEXT_OFFSET = 4;

    // The ammount of spaces that make up the full progress bar.
    private const int PROGRESS_BAR_SIZE = 50;

    // The value each step in the progress bar represents. Since there are 50 spaces that make up the progress bare, each space must be 2 percent.
    private const int PROGRESS_BAR_STEP = 2;

    // Locker that will prevent two threads from updating the progress bar text at the same time.
    private readonly object _progressBarLocker = new object();

    private string _operationText = string.Empty;
    private StringBuilder _emptyProgressContainer = new StringBuilder();
    private StringBuilder _filledProgressContainer = new StringBuilder();
    private int _timeColumnPosition = 0;
    private double _elaspedTime = 0;
    private long _itemsCompleted = 0;
    private long _itemsToComplete = 0;
    private long _currentPercentage = 0;
    private Timer? _progressTimer;
    private bool _isActive = false;
    private bool _disposed = false;

    public long CurrentPercentage => _currentPercentage;

    /// <summary>
    /// Initializes  a new instance of a ProgressBar class and displays it in the console.
    /// The timer will be started and progress can be updated by calling ProgressBar.Update. 
    /// </summary>
    /// <param name="operationText">The operation that is being performed.</param>
    /// <param name="itemsToComplete">The total number of items that need to be completed.</param>
    public ProgressBar(string operationText, long itemsToComplete)
    {
        _operationText = operationText;
        _itemsToComplete = itemsToComplete;

        for (int i = 0; i < PROGRESS_BAR_SIZE; i++)
        {
            _emptyProgressContainer.Append (" ");
        }
    }

    /// <summary>
    /// Releases all resources used by the current instance of ProgressBar.
    /// </summary>
    public void Dispose()
    {
        _disposed = true;
        _isActive = false;
        _progressTimer?.Dispose();

        Console.Write("\r\n\r\n");
    }

    /// <summary>
    /// Will start the progress bar. Once started, the progress will need to be updated
    /// using the Update() method. Progress bar will run until it has been disposed of.
    /// Progress bar will not go above 100 percent.
    /// </summary>
    /// <returns></returns>
    public async Task Start()
    {
        if (_isActive || _disposed)
            return;

        Console.Write($"{_operationText} | ");
        Console.Write($"{_emptyProgressContainer}", backgroundColor: ConsoleColor.Gray);
        Console.Write($" | 0%");
        _timeColumnPosition = Console.CursorColumnPosition + TIMER_TEXT_OFFSET;

        await Task.Delay(START_TIMER_DELAY);
        _progressTimer = new Timer(ProgressBarTimer, null, 0, TIMER_UPDATE_FREQUENCY);
        _isActive = true;
    }

    /// <summary>
    /// Updates the progress bar with the items that were completed. The value passed in will be added to the
    /// total number of items completed.
    /// </summary>
    /// <param name="items">The number of items that were completed since the last Update call.</param>
    public void Update(long items)
    {
        if (!_isActive || _disposed)
            return;

        _itemsCompleted += items;

        long percentage = (long)(100 * _itemsCompleted / _itemsToComplete);

        if (percentage % PROGRESS_BAR_STEP != 0 || percentage == _currentPercentage || percentage > 100)
            return;

        for (long i = _currentPercentage; i < percentage; i += PROGRESS_BAR_STEP)
        {
            _emptyProgressContainer = _emptyProgressContainer.Remove(0, 1);
            _filledProgressContainer = _filledProgressContainer.Insert(0, " ");
        }

        lock (_progressBarLocker)
        {
            Console.CursorColumnPosition = 0;
            Console.Write($@"{_operationText} | ");
            Console.Write($@"{_filledProgressContainer}", backgroundColor: ConsoleColor.Green);
            Console.Write($@"{_emptyProgressContainer}", backgroundColor: ConsoleColor.Gray);
            Console.Write($@" | {percentage}%");
        }

        _currentPercentage = percentage;
    }

    private void ProgressBarTimer(object? state)
    {
        lock (_progressBarLocker)
        {
            if (!_disposed)
            {
                Console.CursorColumnPosition = _timeColumnPosition;
                Console.Write($"{_elaspedTime:N2}s");
            }
        }

        _elaspedTime += (double)TIMER_UPDATE_FREQUENCY / 1000;
    }
}