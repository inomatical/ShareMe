using System.Windows;

namespace EagleShare;

/// <summary>
/// The EagleShare main window. It is the single application surface: the local
/// receiver lives only while this window is open, and the inbox and preview are
/// presented here.
/// </summary>
public partial class MainWindow : Window
{
    private readonly EagleShareReceiver _receiver = new();

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        _receiver.StopAsync().GetAwaiter().GetResult();
        base.OnClosing(e);
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var lanAddress = EagleShareReceiver.GetLanAddress();
        AddressText.Text = $"Receiver: http://{lanAddress}:{_receiver.Port} (default port {EagleShareReceiver.DefaultPort})";

        try
        {
            await _receiver.StartAsync();
            StatusText.Text = "Receiver is listening. Windows may request private-network firewall permission.";
        }
        catch (Exception)
        {
            AddressText.Text = $"Receiver unavailable at http://{lanAddress}:{_receiver.Port}";
            StatusText.Text = "Receiver could not start. Check that the port is available and allow private-network access when prompted.";
        }
    }
}
