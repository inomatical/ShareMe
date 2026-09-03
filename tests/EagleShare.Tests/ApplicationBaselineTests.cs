using System.Reflection;
using Xunit;

namespace EagleShare.Tests;

/// <summary>
/// Baseline tests that establish the automated-test boundary for the
/// EagleShare desktop application. They verify that the application assembly
/// is present, loadable, and exposes the expected application surface.
/// </summary>
public class ApplicationBaselineTests
{
    [Fact]
    public void ApplicationAssembly_IsLoadable()
    {
        var assembly = typeof(App).Assembly;

        Assert.NotNull(assembly);
        Assert.Equal("EagleShare", assembly.GetName().Name);
    }

    [Fact]
    public void ApplicationEntryPoint_IsDefined()
    {
        var assembly = typeof(App).Assembly;

        var entryPoint = assembly.EntryPoint;

        Assert.NotNull(entryPoint);
        Assert.Equal(MethodAttributes.Static, entryPoint.Attributes & MethodAttributes.Static);
    }

    [Fact]
    public void App_DerivesFromWpfApplication()
    {
        var appType = typeof(App);

        Assert.True(
            typeof(System.Windows.Application).IsAssignableFrom(appType),
            "EagleShare.App must derive from System.Windows.Application.");
    }

    [Fact]
    public void MainWindow_IsAWindow()
    {
        var windowType = typeof(MainWindow);

        Assert.True(
            typeof(System.Windows.Window).IsAssignableFrom(windowType),
            "EagleShare.MainWindow must derive from System.Windows.Window.");
    }

    [Fact]
    public void MainWindow_HasParameterlessConstructor()
    {
        var windowType = typeof(MainWindow);

        var constructor = windowType.GetConstructor(Type.EmptyTypes);

        Assert.NotNull(constructor);
        Assert.True(constructor.IsPublic);
    }
}
