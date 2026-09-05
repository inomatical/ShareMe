using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace EagleShare;

/// <summary>
/// The EagleShare main window. It is the single application surface: the local
/// receiver lives only while this window is open, and the inbox and preview are
/// presented here.
/// </summary>
public partial class MainWindow : Window
{
    private readonly ObservableCollection<InboxItem> _inboxItems = [];
    private readonly EagleShareReceiver _receiver;

    public MainWindow()
    {
        _receiver = new EagleShareReceiver();
        InitializeComponent();
        InboxList.ItemsSource = _inboxItems;
        _receiver.TextReceived += Receiver_TextReceived;
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

    private void Receiver_TextReceived(object? sender, InboxItem item)
    {
        _ = Dispatcher.InvokeAsync(() =>
        {
            _inboxItems.Add(item);
            InboxList.SelectedItem = item;
            StatusText.Text = "Text item received.";
        });
    }

    private void InboxList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var selectedItem = InboxList.SelectedItem as InboxItem;
        PreviewText.Text = selectedItem?.Content ?? string.Empty;
        CopyButton.IsEnabled = selectedItem is not null;
    }

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        if (InboxList.SelectedItem is not InboxItem selectedItem)
        {
            return;
        }

        Clipboard.SetText(selectedItem.Content);
        StatusText.Text = "Text copied to the clipboard.";
    }
}
