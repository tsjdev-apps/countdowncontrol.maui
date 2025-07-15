namespace CountdownControlMaui.Controls;

/// <summary>
/// A custom ContentView that displays a circular animated countdown 
/// with a digital-style time display.
/// The component renders a radial progress ring with a centered time 
/// in MM:ss format, and includes optional blinking colon animation 
/// for a classic digital clock feel.
/// </summary>
public partial class CountdownControl : ContentView
{
    private TimeSpan _remainingTime;
    private DateTime _endTime;
    private CancellationTokenSource _cts = null!;
    private float _progress;
    private bool _isBlinking = false;
    private double _lastSize = 0;

    /// <summary>
    /// The total countdown duration. 
    /// Automatically clamped to a maximum of 60 minutes.
    /// </summary>
    public static readonly BindableProperty DurationProperty =
        BindableProperty.Create(
            nameof(Duration),
            typeof(TimeSpan),
            typeof(CountdownControl),
            TimeSpan.Zero,
            propertyChanged: OnDurationChanged);

    public TimeSpan Duration
    {
        get => (TimeSpan)GetValue(DurationProperty);
        set => SetValue(DurationProperty, value);
    }

    /// <summary>
    /// The color of the time text (MM:ss).
    /// </summary>
    public static readonly BindableProperty TextColorProperty =
        BindableProperty.Create(
            nameof(TextColor),
            typeof(Color),
            typeof(CountdownControl),
            Colors.Black);

    public Color TextColor
    {
        get => (Color)GetValue(TextColorProperty);
        set => SetValue(TextColorProperty, value);
    }

    /// <summary>
    /// The color of the active (remaining) portion of the ring.
    /// </summary>
    public static readonly BindableProperty ActiveColorProperty =
        BindableProperty.Create(
            nameof(ActiveColor),
            typeof(Color),
            typeof(CountdownControl),
            Colors.Red);

    public Color ActiveColor
    {
        get => (Color)GetValue(ActiveColorProperty);
        set => SetValue(ActiveColorProperty, value);
    }

    /// <summary>
    /// The color of the inactive (elapsed) portion of the ring.
    /// </summary>
    public static readonly BindableProperty InactiveColorProperty =
        BindableProperty.Create(
            nameof(InactiveColor),
            typeof(Color),
            typeof(CountdownControl),
            Colors.LightGray);

    public Color InactiveColor
    {
        get => (Color)GetValue(InactiveColorProperty);
        set => SetValue(InactiveColorProperty, value);
    }

    /// <summary>
    /// Event triggered when the countdown reaches zero.
    /// </summary>
    public event EventHandler CountdownCompleted = null!;

    /// <summary>
    /// Initializes the control and sets up 
    /// bindings and resizing behavior.
    /// </summary>
    public CountdownControl()
    {
        InitializeComponent();

        // Injects the custom drawable used to render the animated ring
        ProgressRing.Drawable
            = new CountdownDrawable(
                () => _progress,
                () => ActiveColor,
                () => InactiveColor);

        // Enable font resizing when the control size changes
        SizeChanged += OnSizeChanged;
    }

    /// <summary>
    /// Adjusts font size proportionally when the control size changes.
    /// Ensures that the MM:ss text remains visually balanced at any size.
    /// </summary>
    private void OnSizeChanged(object? sender, EventArgs e)
    {
        double controlSize = Math.Min(Width, Height);
        if (Math.Abs(controlSize - _lastSize) < 1)
        {
            return;
        }

        _lastSize = controlSize;

        // Use 20% of the smaller side as base font size
        double dynamicFontSize = controlSize * 0.2;

        MinutesLabel.FontSize = dynamicFontSize;
        ColonLabel.FontSize = dynamicFontSize;
        SecondsLabel.FontSize = dynamicFontSize;
    }

    /// <summary>
    /// Starts the countdown animation loop and colon blinking.
    /// </summary>
    public void Start()
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        _remainingTime = Duration;
        _endTime = DateTime.UtcNow + _remainingTime;

        StartColonBlinkAnimation();

        // Run the countdown loop at ~60 FPS using Task.Run
        Task.Run(async () =>
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                var now = DateTime.UtcNow;
                _remainingTime = _endTime - now;

                // Countdown complete
                if (_remainingTime <= TimeSpan.Zero)
                {
                    _progress = 0;

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        MinutesLabel.Text = "00";
                        SecondsLabel.Text = "00";
                        ProgressRing.Invalidate();
                        CountdownCompleted?.Invoke(this, EventArgs.Empty);
                        StopColonBlinkAnimation();
                    });
                    break;
                }

                // Update progress and UI
                var totalSeconds = Duration.TotalSeconds;
                var currentSeconds = _remainingTime.TotalSeconds;
                _progress = (float)(currentSeconds / totalSeconds);

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    MinutesLabel.Text = _remainingTime.ToString("mm");
                    SecondsLabel.Text = _remainingTime.ToString("ss");
                    ProgressRing.Invalidate();
                });

                // 60 FPS about 16 ms/frame
                await Task.Delay(16);
            }
        }, _cts.Token);
    }

    /// <summary>
    /// Stops the countdown (if running).
    /// </summary>
    public void Stop()
        => _cts?.Cancel();

    /// <summary>
    /// Triggered when the Duration property changes.
    /// Ensures it’s limited to 60 minutes, 
    /// updates labels and progress ring immediately.
    /// </summary>
    private static void OnDurationChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (CountdownControl)bindable;
        var newDuration = (TimeSpan)newValue;

        // Clamp to max 60 minutes
        if (newDuration > TimeSpan.FromMinutes(60))
        {
            newDuration = TimeSpan.FromMinutes(60);
        }

        control._remainingTime = newDuration;

        // Update UI immediately
        MainThread.BeginInvokeOnMainThread(() =>
        {
            control.MinutesLabel.Text = newDuration.ToString("mm");
            control.SecondsLabel.Text = newDuration.ToString("ss");
            control._progress = 1f;
            control.ProgressRing.Invalidate();
        });
    }

    /// <summary>
    /// Starts the colon blink animation (fade in/out loop).
    /// </summary>
    private void StartColonBlinkAnimation()
    {
        if (_isBlinking)
        {
            return;
        }

        _isBlinking = true;
        _ = BlinkLoopAsync();
    }

    /// <summary>
    /// Blink loop for the colon. 
    /// Fades opacity from 1 -> 0 and back every 500ms.
    /// </summary>
    private async Task BlinkLoopAsync()
    {
        while (_isBlinking)
        {
            await ColonLabel.FadeTo(0, 500, Easing.CubicInOut);
            await ColonLabel.FadeTo(1, 500, Easing.CubicInOut);
        }
    }

    /// <summary>
    /// Stops the colon blink animation and restores full opacity.
    /// </summary>
    private void StopColonBlinkAnimation()
    {
        _isBlinking = false;
        ColonLabel.Opacity = 1;
    }
}