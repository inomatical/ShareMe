using System.Windows;

namespace EagleShare;

/// <summary>
/// The EagleShare main window. It is the single application surface: the local
/// receiver lives only while this window is open, and the inbox and preview are
/// presented here.
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }
}
