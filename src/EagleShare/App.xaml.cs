using System.Windows;

namespace EagleShare;

/// <summary>
/// EagleShare application entry point. The application is a foreground WPF app:
/// it has no system-tray presence and performs no work while the main window is
/// closed.
/// </summary>
public partial class App : Application
{
	protected override void OnStartup(StartupEventArgs e)
	{
		EagleShareStorage.CreateDefault().Initialize();

		base.OnStartup(e);
	}
}
