namespace InternetBankingApp.Controls;

public partial class LoadingDots : ContentView
{
    private CancellationTokenSource? _cts;

    public LoadingDots()
    {
        InitializeComponent();
    }

    public void Start()
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        _ = AnimateLoopAsync(_cts.Token);
    }

    public void Stop()
    {
        _cts?.Cancel();
        _cts = null;
        Dot1.TranslationY = 0;
        Dot2.TranslationY = 0;
        Dot3.TranslationY = 0;
    }

    private async Task AnimateLoopAsync(CancellationToken token)
    {
        var dots = new[] { Dot1, Dot2, Dot3 };

        while (!token.IsCancellationRequested)
        {
            foreach (var dot in dots)
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }

                await dot.TranslateToAsync(0, -8, 150, Easing.CubicOut);
                await dot.TranslateToAsync(0, 0, 150, Easing.CubicIn);
            }
        }
    }
}
