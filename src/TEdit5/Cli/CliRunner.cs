using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

    private const string UsageInspect = """
        inspect-schematic  --schematic <file.TEditSch>
        """;

    private const string UsageView = """
        view-schematic  --schematic <file.TEditSch>
        (Opens a GUI viewer — cannot be combined with other headless flags.)
        """;

    public static async Task<int> RunAsync(string[] args)
    {
        if (args.Length == 0) return ShowHelp();

        return args[0].ToLowerInvariant() switch
        {
            "paste-schematic"    => await PasteSchematicAsync(args[1..]),
            "export-schematic"   => await ExportSchematicAsync(args[1..]),
            "inspect-schematic"  => InspectSchematic(args[1..]),
            "--help" or "-h"     => ShowHelp(),
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
            PasteTiles     = !flags.ContainsKey("no-tiles"),
            PasteWalls     = !flags.ContainsKey("no-walls"),
            PasteLiquids   = !flags.ContainsKey("no-liquids"),
            PasteWires     = !flags.ContainsKey("no-wires"),
            PasteSprites   = !flags.ContainsKey("no-sprites"),
            PasteEmpty     = !flags.ContainsKey("no-empty"),
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

    // ───────────────────────────── inspect-schematic ────────────────────────

    private static int InspectSchematic(string[] args)
    {
        var flags = ParseArgs(args);

        if (!flags.TryGetValue("schematic", out var schPath))
            return Error("inspect-schematic requires --schematic.\n" + UsageInspect);

        if (!File.Exists(schPath))
            return Error($"Schematic file not found: {schPath}");

        var buffer = ClipboardBuffer.Load(schPath);
        if (buffer == null)
            return Error("Failed to load schematic.");

        int w = buffer.Size.X, h = buffer.Size.Y;
        int total = w * h;

        Console.WriteLine($"Schematic : {buffer.Name}");
        Console.WriteLine($"File      : {schPath}");
        Console.WriteLine($"Size      : {w} × {h} tiles  ({total:N0} total)");
        Console.WriteLine($"Chests    : {buffer.Chests.Count}");
        Console.WriteLine($"Signs     : {buffer.Signs.Count}");
        Console.WriteLine($"Entities  : {buffer.TileEntities.Count}");
        Console.WriteLine();

        // Count tile types
        var tileCounts = new Dictionary<ushort, int>();
        var wallCounts = new Dictionary<ushort, int>();
        int emptyTiles = 0;

        for (int x = 0; x < w; x++)
        for (int y = 0; y < h; y++)
        {
            var t = buffer.Tiles[x, y];
            if (t.IsActive)
                tileCounts[t.Type] = (tileCounts.TryGetValue(t.Type, out var tc) ? tc : 0) + 1;
            else
                emptyTiles++;

            if (t.Wall != 0)
                wallCounts[t.Wall] = (wallCounts.TryGetValue(t.Wall, out var wc) ? wc : 0) + 1;
        }

        Console.WriteLine($"Tile breakdown ({tileCounts.Count} distinct types, {emptyTiles:N0} air):");
        foreach (var kv in tileCounts.OrderByDescending(k => k.Value).Take(20))
        {
            double pct = kv.Value * 100.0 / total;
            Console.WriteLine($"  Tile {kv.Key,5}:  {kv.Value,7:N0}  ({pct:F1}%)");
        }

        if (wallCounts.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine($"Wall breakdown ({wallCounts.Count} distinct types):");
            foreach (var kv in wallCounts.OrderByDescending(k => k.Value).Take(10))
            {
                double pct = kv.Value * 100.0 / total;
                Console.WriteLine($"  Wall {kv.Key,5}:  {kv.Value,7:N0}  ({pct:F1}%)");
            }
        }

        return 0;
    }

    // ───────────────────────────── helpers ──────────────────────────────────

    private static int ShowHelp()
    {
        Console.WriteLine("TEdit CLI — headless and viewer schematic operations\n");
        Console.WriteLine(UsagePaste);
        Console.WriteLine();
        Console.WriteLine(UsageExport);
        Console.WriteLine();
        Console.WriteLine(UsageInspect);
        Console.WriteLine();
        Console.WriteLine(UsageView);
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
                result[key] = args[++i];
            else
                result[key] = "";
        }
        return result;
    }

    /// <summary>
    /// Public helper used by Program.cs to extract a single flag from a partial arg array.
    /// </summary>
    public static string? ExtractFlag(string[] args, string key)
    {
        var flags = ParseArgs(args);
        return flags.TryGetValue(key, out var v) && v.Length > 0 ? v : null;
    }
}
