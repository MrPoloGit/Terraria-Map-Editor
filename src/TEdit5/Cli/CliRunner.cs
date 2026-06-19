using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using TEdit.Editor.Clipboard;
using TEdit.Geometry;
using TEdit.Terraria;

namespace TEdit5.Cli;

/// <summary>
/// Headless CLI handler. Called from Program.Main when a CLI verb is detected.
/// Exits the process when done; never reaches Avalonia.
/// </summary>
public static class CliRunner
{
    private const string UsagePaste = """
        paste-schematic  --world <world.wld>  --schematic <file.TEditSch>
                         --x <tile-x>  --y <tile-y>
                         [--output <out.wld>]
                         [--no-tiles] [--no-walls] [--no-liquids]
                         [--no-wires] [--no-sprites] [--no-empty] [--no-replace]
        """;

    private const string UsageExport = """
        export-schematic  --world <world.wld>
                          --x <tile-x>  --y <tile-y>
                          --width <w>   --height <h>
                          --output <out.TEditSch>
        """;

    public static async Task<int> RunAsync(string[] args)
    {
        if (args.Length == 0) return ShowHelp();

        return args[0].ToLowerInvariant() switch
        {
            "paste-schematic"  => await PasteSchematicAsync(args[1..]),
            "export-schematic" => await ExportSchematicAsync(args[1..]),
            "--help" or "-h"   => ShowHelp(),
            _ => Error($"Unknown command: {args[0]}")
        };
    }

    // ───────────────────────────── paste-schematic ──────────────────────────

    private static async Task<int> PasteSchematicAsync(string[] args)
    {
        var flags = ParseArgs(args);

        if (!flags.TryGetValue("world", out var worldPath) ||
            !flags.TryGetValue("schematic", out var schPath) ||
            !flags.TryGetValue("x", out var xStr) ||
            !flags.TryGetValue("y", out var yStr))
        {
            return Error("paste-schematic requires --world, --schematic, --x and --y.\n" + UsagePaste);
        }

        if (!int.TryParse(xStr, out int x) || !int.TryParse(yStr, out int y))
            return Error("--x and --y must be integers.");

        if (!File.Exists(worldPath)) return Error($"World file not found: {worldPath}");
        if (!File.Exists(schPath))   return Error($"Schematic file not found: {schPath}");

        Console.WriteLine($"Loading world: {worldPath}");
        var (world, err) = await World.LoadWorldAsync(worldPath);
        if (world == null) return Error($"Failed to load world: {err?.Message}");

        Console.WriteLine($"Loading schematic: {schPath}");
        var buffer = ClipboardBuffer.Load(schPath);
        if (buffer == null) return Error("Failed to load schematic.");

        var opts = new PasteOptions
        {
            PasteTiles    = !flags.ContainsKey("no-tiles"),
            PasteWalls    = !flags.ContainsKey("no-walls"),
            PasteLiquids  = !flags.ContainsKey("no-liquids"),
            PasteWires    = !flags.ContainsKey("no-wires"),
            PasteSprites  = !flags.ContainsKey("no-sprites"),
            PasteEmpty    = !flags.ContainsKey("no-empty"),
            PasteOverTiles = !flags.ContainsKey("no-replace"),
        };

        Console.WriteLine($"Pasting {buffer.Size.X}×{buffer.Size.Y} schematic at ({x},{y})");
        buffer.Paste(world, new Vector2Int32(x, y), null, opts);

        var outPath = flags.TryGetValue("output", out var op) ? op : worldPath;
        Console.WriteLine($"Saving world: {outPath}");
        await World.SaveAsync(world, outPath);

        Console.WriteLine("Done.");
        return 0;
    }

    // ───────────────────────────── export-schematic ─────────────────────────

    private static async Task<int> ExportSchematicAsync(string[] args)
    {
        var flags = ParseArgs(args);

        if (!flags.TryGetValue("world",  out var worldPath) ||
            !flags.TryGetValue("x",      out var xStr)      ||
            !flags.TryGetValue("y",      out var yStr)      ||
            !flags.TryGetValue("width",  out var wStr)      ||
            !flags.TryGetValue("height", out var hStr)      ||
            !flags.TryGetValue("output", out var outPath))
        {
            return Error("export-schematic requires --world, --x, --y, --width, --height and --output.\n" + UsageExport);
        }

        if (!int.TryParse(xStr, out int x)  || !int.TryParse(yStr, out int y)  ||
            !int.TryParse(wStr, out int w)   || !int.TryParse(hStr, out int h))
            return Error("--x, --y, --width and --height must be integers.");

        if (!File.Exists(worldPath)) return Error($"World file not found: {worldPath}");

        Console.WriteLine($"Loading world: {worldPath}");
        var (world, err) = await World.LoadWorldAsync(worldPath);
        if (world == null) return Error($"Failed to load world: {err?.Message}");

        Console.WriteLine($"Exporting region ({x},{y}) {w}×{h}");
        var area = new RectangleInt32(x, y, w, h);
        var buffer = ClipboardBuffer.GetSelectionBuffer(world, area);
        buffer.Name = Path.GetFileNameWithoutExtension(outPath);

        Console.WriteLine($"Saving schematic: {outPath}");
        buffer.Save(outPath, null);

        Console.WriteLine("Done.");
        return 0;
    }

    // ───────────────────────────── helpers ──────────────────────────────────

    private static int ShowHelp()
    {
        Console.WriteLine("TEdit CLI — headless schematic operations\n");
        Console.WriteLine(UsagePaste);
        Console.WriteLine();
        Console.WriteLine(UsageExport);
        return 0;
    }

    private static int Error(string msg)
    {
        Console.Error.WriteLine("Error: " + msg);
        return 1;
    }

    /// <summary>
    /// Parses ["--key", "value", "--flag"] into {"key":"value", "flag":""}.
    /// </summary>
    private static Dictionary<string, string> ParseArgs(string[] args)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (!arg.StartsWith("--")) continue;
            var key = arg[2..];
            if (i + 1 < args.Length && !args[i + 1].StartsWith("--"))
            {
                result[key] = args[++i];
            }
            else
            {
                result[key] = "";
            }
        }
        return result;
    }
}
