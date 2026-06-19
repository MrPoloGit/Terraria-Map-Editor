using System;
using System.Collections.ObjectModel;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using TEdit.Terraria;

namespace TEdit5.ViewModels;

public partial class NewWorldViewModel : ReactiveObject
{
    // ── Basic settings ──────────────────────────────────────────────────────

    [Reactive] private string _title = "New World";
    [Reactive] private string _seed = "";

    // Size preset: 0=Small 1=Medium 2=Large 3=Custom
    [Reactive] private int _sizePreset = 1;
    [Reactive] private int _tilesWide = 4200;
    [Reactive] private int _tilesHigh = 1200;

    // ── Surface ──────────────────────────────────────────────────────────────

    [Reactive] private double _hillSize = 30;
    [Reactive] private bool _generateGrass = true;
    [Reactive] private bool _generateWalls = true;

    // ── Caves ────────────────────────────────────────────────────────────────

    [Reactive] private bool _generateCaves = true;
    [Reactive] private bool _surfaceCaves = false;

    // 0=Normal 1=Large 2=Labyrinth
    [Reactive] private int _cavePreset = 0;

    [Reactive] private double _caveNoise = 0.08;
    [Reactive] private double _caveMultiplier = 0.02;
    [Reactive] private double _caveDensity = 3.0;

    // ── Underworld ───────────────────────────────────────────────────────────

    [Reactive] private bool _generateUnderworld = true;
    [Reactive] private bool _generateAsh = true;
    [Reactive] private bool _generateLava = true;

    public ObservableCollection<string> SizePresets { get; } =
        new() { "Small (4200×1200)", "Medium (6400×1800)", "Large (8400×2400)", "Custom" };

    public ObservableCollection<string> CavePresets { get; } =
        new() { "Normal", "Large", "Labyrinth" };

    [Reactive] private bool _isCustomSize;

    public NewWorldViewModel()
    {
        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SizePreset))
            {
                IsCustomSize = SizePreset == 3;
                ApplySizePreset(SizePreset);
            }
            if (e.PropertyName == nameof(CavePreset))
                ApplyCavePreset(CavePreset);
        };
    }

    private void ApplySizePreset(int idx)
    {
        (TilesWide, TilesHigh) = idx switch
        {
            0 => (4200, 1200),
            1 => (6400, 1800),
            2 => (8400, 2400),
            _ => (TilesWide, TilesHigh) // Custom — keep existing
        };
    }

    private void ApplyCavePreset(int idx)
    {
        (CaveNoise, CaveMultiplier, CaveDensity) = idx switch
        {
            0 => (0.08, 0.02, 3.0),
            1 => (0.10, 0.03, 2.5),
            2 => (0.12, 0.04, 2.0),
            _ => (CaveNoise, CaveMultiplier, CaveDensity)
        };
    }

    /// <summary>
    /// Builds a World object pre-configured with all generation settings.
    /// </summary>
    public World BuildWorld()
    {
        var world = new World(TilesHigh, TilesWide, Title)
        {
            Version      = WorldConfiguration.CompatibleVersion,
            GroundLevel  = TilesHigh * 0.28,   // ~28% from top
            RockLevel    = TilesHigh * 0.40,    // ~40% from top
            Seed         = Seed,
            HillSize     = HillSize,
            GenerateGrass = GenerateGrass,
            GenerateWalls = GenerateWalls,
            GenerateCaves = GenerateCaves,
            SurfaceCaves  = SurfaceCaves,
            CaveNoise      = CaveNoise,
            CaveMultiplier = CaveMultiplier,
            CaveDensity    = CaveDensity,
            GenerateUnderworld = GenerateUnderworld,
            GenerateAsh  = GenerateAsh,
            GenerateLava = GenerateLava,
            UnderworldRoofNoise  = 0.15,
            UnderworldFloorNoise = 0.05,
            UnderworldLavaNoise  = 0.10,
        };

        world.ResetTime();
        world.CreationTime = DateTime.Now.ToBinary();

        // Default NPC names (mirrors WPF version)
        world.CharacterNames.Add(new TEdit.Terraria.Objects.NpcName(17,  "Harold"));
        world.CharacterNames.Add(new TEdit.Terraria.Objects.NpcName(18,  "Molly"));
        world.CharacterNames.Add(new TEdit.Terraria.Objects.NpcName(19,  "Dominique"));
        world.CharacterNames.Add(new TEdit.Terraria.Objects.NpcName(20,  "Felicitae"));
        world.CharacterNames.Add(new TEdit.Terraria.Objects.NpcName(22,  "Steve"));
        world.CharacterNames.Add(new TEdit.Terraria.Objects.NpcName(54,  "Fitz"));
        world.CharacterNames.Add(new TEdit.Terraria.Objects.NpcName(38,  "Gimut"));
        world.CharacterNames.Add(new TEdit.Terraria.Objects.NpcName(107, "Knogs"));
        world.CharacterNames.Add(new TEdit.Terraria.Objects.NpcName(108, "Fizban"));
        world.CharacterNames.Add(new TEdit.Terraria.Objects.NpcName(124, "Nancy"));
        world.CharacterNames.Add(new TEdit.Terraria.Objects.NpcName(160, "Truffle"));
        world.CharacterNames.Add(new TEdit.Terraria.Objects.NpcName(178, "Steampunker"));
        world.CharacterNames.Add(new TEdit.Terraria.Objects.NpcName(207, "Dye Trader"));
        world.CharacterNames.Add(new TEdit.Terraria.Objects.NpcName(208, "Party Girl"));
        world.CharacterNames.Add(new TEdit.Terraria.Objects.NpcName(209, "Cyborg"));
        world.CharacterNames.Add(new TEdit.Terraria.Objects.NpcName(227, "Painter"));
        world.CharacterNames.Add(new TEdit.Terraria.Objects.NpcName(228, "Witch Doctor"));
        world.CharacterNames.Add(new TEdit.Terraria.Objects.NpcName(229, "Pirate"));

        return world;
    }
}
