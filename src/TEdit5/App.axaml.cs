using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Linq;
using TEdit5.Editor;
using TEdit5.Services;
using TEdit5.ViewModels;
using TEdit5.Views;
using TEdit.Editor;

namespace TEdit5;

public partial class App : Application
{
    public IServiceProvider Services { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        var services = new ServiceCollection();

        // register view models
        services.AddSingleton<MainWindowViewModel>();
        services.AddTransient<FileManagerViewModel>();

        // register editing tools
        services.AddSingleton<ToolSelectionViewModel>();
        services.AddSingleton<TilePicker>();
        services.AddSingleton<IDocumentService, DocumentService>();

        services.AddSingleton<IMouseTool, ArrowTool>();
        //services.AddSingleton<IMouseTool, BrushTool>();
        services.AddSingleton<IMouseTool, PencilTool>();
        services.AddSingleton<IMouseTool, SelectTool>();

        // register services
        services.AddTransient<IDialogService, DialogService>();

        //services.AddSingleton<IMyInterface, MyImplementation>()
        var serviceProvider = services.BuildServiceProvider();
        this.Resources[typeof(IServiceProvider)] = serviceProvider;
        Services = serviceProvider;
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = (IServiceProvider)this.Resources[typeof(IServiceProvider)];
        var vm = services.GetRequiredService<MainWindowViewModel>();
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = new MainWindow { DataContext = vm };
            desktop.MainWindow = mainWindow;

            // Handle file open via OS file association (double-click in Finder/Explorer).
            // On macOS this fires through IActivatableLifetime.Activated with FileActivatedEventArgs.
            // On Windows/Linux the path arrives in desktop.Args instead.
            if (this.TryGetFeature<IActivatableLifetime>() is { } activatable)
            {
                activatable.Activated += async (_, e) =>
                {
                    if (e is FileActivatedEventArgs fileArgs)
                        foreach (var f in fileArgs.Files)
                            await mainWindow.LoadWorldFromPath(f.Path.LocalPath);
                };
            }

            // Windows / Linux: file path passed as command-line argument
            var initialFile = desktop.Args?
                .FirstOrDefault(a => a.EndsWith(".wld", StringComparison.OrdinalIgnoreCase) && File.Exists(a));
            if (initialFile != null)
                mainWindow.Opened += async (_, _) => await mainWindow.LoadWorldFromPath(initialFile);

        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainWindow
            {
                DataContext = vm
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
