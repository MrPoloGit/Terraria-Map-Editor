using Avalonia;
using Avalonia.Controls;
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

    /// <summary>
    /// Set by Program.cs before Avalonia starts when a .TEditSch path is given via CLI.
    /// The App will open SchematicViewerWindow as the main window in that case.
    /// </summary>
    public static string? PendingSchematicPath { get; set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        var services = new ServiceCollection();

        // view models
        services.AddSingleton<MainWindowViewModel>();
        services.AddTransient<FileManagerViewModel>();
        services.AddSingleton<ClipboardViewModel>();

        // editing tools
        services.AddSingleton<ToolSelectionViewModel>();
        services.AddSingleton<TilePicker>();
        services.AddSingleton<IDocumentService, DocumentService>();

        services.AddSingleton<IMouseTool, ArrowTool>();
        services.AddSingleton<IMouseTool, PencilTool>();
        services.AddSingleton<IMouseTool, SelectTool>();
        services.AddSingleton<IMouseTool, ClipboardTool>();

        // services
        services.AddTransient<IDialogService, DialogService>();

        var serviceProvider = services.BuildServiceProvider();
        this.Resources[typeof(IServiceProvider)] = serviceProvider;
        Services = serviceProvider;
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = (IServiceProvider)this.Resources[typeof(IServiceProvider)];

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Schematic-viewer mode: skip the main editor window.
            if (PendingSchematicPath != null)
            {
                var viewer = SchematicViewerWindow.CreateFromPath(PendingSchematicPath);
                desktop.MainWindow = viewer;
                base.OnFrameworkInitializationCompleted();
                return;
            }

            var vm         = services.GetRequiredService<MainWindowViewModel>();
            var mainWindow = new MainWindow { DataContext = vm };
            desktop.MainWindow = mainWindow;

            // macOS: file opened via Finder / double-click (IActivatableLifetime).
            if (this.TryGetFeature<IActivatableLifetime>() is { } activatable)
            {
                activatable.Activated += async (_, e) =>
                {
                    if (e is FileActivatedEventArgs fileArgs)
                    {
                        foreach (var f in fileArgs.Files)
                        {
                            var path = f.Path.LocalPath;
                            if (path.EndsWith(".TEditSch", StringComparison.OrdinalIgnoreCase))
                                OpenSchematicViewer(path, mainWindow);
                            else
                                await mainWindow.LoadWorldFromPath(path);
                        }
                    }
                };
            }

            // Windows / Linux: file path passed as CLI argument.
            var args = desktop.Args ?? Array.Empty<string>();

            var worldArg = args
                .Select(a => Path.GetFullPath(a))
                .FirstOrDefault(a => a.EndsWith(".wld", StringComparison.OrdinalIgnoreCase) && File.Exists(a));

            var schArg = args
                .Select(a => Path.GetFullPath(a))
                .FirstOrDefault(a => a.EndsWith(".TEditSch", StringComparison.OrdinalIgnoreCase) && File.Exists(a));

            if (worldArg != null)
                mainWindow.Opened += async (_, _) => await mainWindow.LoadWorldFromPath(worldArg);

            if (schArg != null)
                mainWindow.Opened += (_, _) => OpenSchematicViewer(schArg, mainWindow);
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            var vm = services.GetRequiredService<MainWindowViewModel>();
            singleViewPlatform.MainView = new MainWindow { DataContext = vm };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void OpenSchematicViewer(string path, Window owner)
    {
        var viewer = SchematicViewerWindow.CreateFromPath(path);
        viewer.Show(owner);
    }
}
