using Avalonia;
using ReactiveUI.Avalonia;
using Optris.Icons.Avalonia;
using Optris.Icons.Avalonia.MaterialDesign;
using System;
using System.IO;
using TEdit5.Cli;

namespace TEdit5;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Fully headless CLI verbs — never touch Avalonia.
        if (args.Length > 0 &&
            args[0] is "paste-schematic" or "export-schematic" or "inspect-schematic" or "--help" or "-h")
        {
            Environment.Exit(CliRunner.RunAsync(args).GetAwaiter().GetResult());
            return;
        }

        // view-schematic opens a GUI but starts in schematic-viewer mode.
        if (args.Length > 0 && args[0] == "view-schematic")
        {
            var path = CliRunner.ExtractFlag(args[1..], "schematic");
            if (path == null || !File.Exists(path))
            {
                Console.Error.WriteLine("view-schematic requires --schematic <path.TEditSch>");
                Environment.Exit(1);
                return;
            }
            App.PendingSchematicPath = path;
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(Array.Empty<string>());
            return;
        }

        // Bare .TEditSch path — open in schematic viewer.
        if (args.Length == 1
            && args[0].EndsWith(".TEditSch", StringComparison.OrdinalIgnoreCase)
            && File.Exists(args[0]))
        {
            App.PendingSchematicPath = Path.GetFullPath(args[0]);
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(Array.Empty<string>());
            return;
        }

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
    {
        IconProvider.Current
            .Register<MaterialDesignIconProvider>();

        return AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace()
                .UseReactiveUI(builder => { });
    }
}
