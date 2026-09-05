using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace EagleShare;

/// <summary>
/// The EagleShare main window. It is the single application surface: the local
/// receiver lives only while this window is open, and the inbox and preview are
/// presented here.
/// </summary>
public partial class MainWindow : Window
{
    private readonly ObservableCollection<InboxItem> _inboxItems = [];
    private readonly EagleShareStorage _storage;
    private readonly EagleShareReceiver _receiver;

    public MainWindow()
    {
        _storage = EagleShareStorage.CreateDefault();
        _receiver = new EagleShareReceiver(_storage);
        InitializeComponent();
        InboxList.ItemsSource = _inboxItems;
        _receiver.ItemReceived += Receiver_ItemReceived;
        _receiver.ImageProgress += Receiver_ImageProgress;
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

    private void Receiver_ItemReceived(object? sender, InboxItem item)
    {
        _ = Dispatcher.InvokeAsync(() =>
        {
            _inboxItems.Add(item);
            InboxList.SelectedItem = item;
            IncomingProgress.Visibility = Visibility.Collapsed;
            StatusText.Text = $"{item.Kind} item received.";
        });
    }

    private void Receiver_ImageProgress(object? sender, IncomingTransferProgress progress)
    {
        _ = Dispatcher.InvokeAsync(() =>
        {
            IncomingProgress.Visibility = Visibility.Visible;
            IncomingProgress.Maximum = Math.Max(progress.TotalBytes, 1);
            IncomingProgress.Value = Math.Min(progress.BytesReceived, IncomingProgress.Maximum);
            StatusText.Text = $"Receiving image: {progress.BytesReceived} of {progress.TotalBytes} bytes.";
        });
    }

    private void InboxList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var selectedItem = InboxList.SelectedItem as InboxItem;
        var isImage = selectedItem?.Kind == "image";
        var isFile = selectedItem?.Kind == "file";
        var isBinaryItem = isImage || isFile;

        CopyButton.Visibility = isBinaryItem ? Visibility.Collapsed : Visibility.Visible;
        CopyButton.IsEnabled = selectedItem is not null && !isBinaryItem;
        SaveImageButton.Visibility = isImage ? Visibility.Visible : Visibility.Collapsed;
        SaveImageButton.IsEnabled = isImage;
        SaveFileButton.Visibility = isFile ? Visibility.Visible : Visibility.Collapsed;
        SaveFileButton.IsEnabled = isFile;
        PreviewText.Visibility = isBinaryItem ? Visibility.Collapsed : Visibility.Visible;
        ImagePreview.Visibility = isImage ? Visibility.Visible : Visibility.Collapsed;
        FilePreview.Visibility = isFile ? Visibility.Visible : Visibility.Collapsed;

        if (!isBinaryItem)
        {
            PreviewText.Text = selectedItem?.Content ?? string.Empty;
            ThumbnailImage.Source = null;
            FullPreviewImage.Source = null;
            return;
        }

        if (isFile)
        {
            ThumbnailImage.Source = null;
            FullPreviewImage.Source = null;
            FileNameText.Text = selectedItem!.OriginalFileName ?? selectedItem.Content;
            FileTypeText.Text = $"Type: {selectedItem.MimeType ?? "Unknown"}";
            FileSizeText.Text = $"Size: {FormatFileSize(selectedItem.Size)}";
            return;
        }

        try
        {
            var imageSource = LoadImage(selectedItem!);
            ThumbnailImage.Source = imageSource;
            FullPreviewImage.Source = imageSource;
        }
        catch (Exception)
        {
            ImagePreview.Visibility = Visibility.Collapsed;
            PreviewText.Visibility = Visibility.Visible;
            PreviewText.Text = "The received image preview is unavailable.";
            SaveImageButton.IsEnabled = false;
        }
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

    private void SaveImageButton_Click(object sender, RoutedEventArgs e)
    {
        if (InboxList.SelectedItem is not InboxItem selectedItem || selectedItem.Kind != "image")
        {
            return;
        }

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            FileName = selectedItem.OriginalFileName ?? selectedItem.Content,
            Filter = "Image files|*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.webp;*.tif;*.tiff|All files|*.*",
            OverwritePrompt = true,
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            _storage.CopyImageTo(selectedItem, dialog.FileName, overwrite: true);
            StatusText.Text = $"Image saved to {dialog.FileName}.";
        }
        catch (Exception exception)
        {
            StatusText.Text = $"Image could not be saved: {exception.Message}";
        }
    }

    private void SaveFileButton_Click(object sender, RoutedEventArgs e)
    {
        if (InboxList.SelectedItem is not InboxItem selectedItem || selectedItem.Kind != "file")
        {
            return;
        }

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            FileName = selectedItem.OriginalFileName ?? selectedItem.Content,
            Filter = "All files|*.*",
            OverwritePrompt = true,
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            _storage.CopyFileTo(selectedItem, dialog.FileName, overwrite: true);
            StatusText.Text = $"File saved to {dialog.FileName}.";
        }
        catch (Exception exception)
        {
            StatusText.Text = $"File could not be saved: {exception.Message}";
        }
    }

    private static string FormatFileSize(long? size) =>
        size is null ? "Unknown" : $"{size.Value:N0} bytes";

    private BitmapImage LoadImage(InboxItem item)
    {
        if (string.IsNullOrWhiteSpace(item.StoredFileName))
        {
            throw new InvalidOperationException("The image has no stored file name.");
        }

        var imagePath = System.IO.Path.Combine(_storage.InboxPath, item.StoredFileName);
        var image = new BitmapImage();
        image.BeginInit();
        image.UriSource = new System.Uri(imagePath, System.UriKind.Absolute);
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.EndInit();
        image.Freeze();
        return image;
    }
}
