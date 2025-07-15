namespace CountdownControlMaui;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
    }

    private void OnStartCountdownButtonClicked(object sender, EventArgs e)
    {
        MyCountdown.Start();
        MyCountdown2.Start();
        MyCountdown3.Start();
    }
}
