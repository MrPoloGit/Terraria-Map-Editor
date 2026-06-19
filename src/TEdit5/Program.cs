using Avalonia;
using ReactiveUI.Avalonia;
using Optris.Icons.Avalonia;
using Optris.Icons.Avalonia.MaterialDesign;
using System;
using TEdit5.Cli;

namespace TEdit5;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // CLI verbs run headless — never touch Avalonia.
        if (args.Length > 0 &&
            args[0] is "paste-schematic" or "export-schematic" or "--help" or "-h")
        {
            Environment.Exit(CliRunner.RunAsync(args).GetAwaiter().GetResult());
            return;
        }

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
    {
        IconProvider.Current
            //.Register<FontAwesomeIconProvider>()
            .Register<MaterialDesignIconProvider>();

        return AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace()
                .UseReactiveUI(builder => { });
    }
}
