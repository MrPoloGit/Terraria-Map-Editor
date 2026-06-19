using System;
using System.ComponentModel;
using TEdit.Terraria;

namespace TEdit.Editor;

/// <summary>
/// Procedural world generation — extracted from the WPF WorldViewModel for shared use.
/// </summary>
public static class WorldGenerator
{
    // ─── Public entry point ───────────────────────────────────────────────────

    public static World Generate(World w, IProgress<ProgressChangedEventArgs>? progress = null)
    {
        if (string.IsNullOrEmpty(w.Seed))
            w.Seed = new Random().Next(0, int.MaxValue).ToString();

        var rand = new Random(w.Seed.GetHashCode());
        var perlin = new PerlinNoise(rand.Next());

        w.SpawnX = w.TilesWide / 2;
        w.SpawnY = (int)Math.Max(0, w.GroundLevel - 10);
        w.BottomWorld = w.TilesHigh * 16;
        w.RightWorld  = w.TilesWide * 16;
        w.Tiles       = new Tile[w.TilesWide, w.TilesHigh];

        Report(progress, 0, "Generating hills…");
        GenerateHills(w, perlin, w.GenerateUnderworld, w.GenerateWalls, progress);

        Report(progress, 0, "Cleaning up surface…");
        CleanupGeneration(w, w.GenerateGrass, w.GenerateWalls);

        if (w.GenerateCaves)
            GenerateCaves(w, rand, w.CaveNoise, w.CaveMultiplier, w.CaveDensity,
                          w.SurfaceCaves, w.GenerateUnderworld, w.GenerateGrass, w.GenerateWalls, progress);

        if (w.GenerateUnderworld && w.GenerateAsh)
            GenerateUnderworld(w, rand, w.GenerateLava,
                               w.UnderworldRoofNoise, w.UnderworldFloorNoise, w.UnderworldLavaNoise, progress);

        Report(progress, 100, "Done.");
        return w;
    }

    // ─── Hill generation ──────────────────────────────────────────────────────

    private static void GenerateHills(World w, PerlinNoise perlin,
        bool generateUnderworld, bool generateWalls,
        IProgress<ProgressChangedEventArgs>? progress)
    {
        for (int y = 0; y < w.TilesHigh; y++)
        {
            Report(progress, Pct(y, w.TilesHigh), "Generating hills…");
            for (int x = 0; x < w.TilesWide; x++)
            {
                double hillHeight = w.GroundLevel - perlin.Noise(x * 0.01, 0) * w.HillSize;
                int fy = w.TilesHigh - 1 - y;

                if (fy >= hillHeight)
                {
                    if (generateUnderworld && fy >= w.TilesHigh - 200)
                    {
                        w.Tiles[x, fy] = new Tile { IsActive = false };
                    }
                    else
                    {
                        var tileType = fy < w.RockLevel ? Tile.TileType.DirtBlock : Tile.TileType.StoneBlock;
                        w.Tiles[x, fy] = new Tile { IsActive = true, Type = (ushort)tileType };

                        if (generateWalls)
                        {
                            if (fy < w.RockLevel && fy != (int)hillHeight)
                                w.Tiles[x, fy].Wall = (ushort)Tile.WallType.DirtWall;
                            else if (fy >= w.RockLevel)
                                w.Tiles[x, fy].Wall = (ushort)Tile.WallType.StoneWall;
                        }
                    }
                }
                else
                {
                    w.Tiles[x, fy] = new Tile { IsActive = false };
                }
            }
        }
    }

    // ─── Surface cleanup ──────────────────────────────────────────────────────

    private static void CleanupGeneration(World w, bool generateGrass, bool generateWalls)
    {
        int minHillY = -1;

        for (int x = 0; x < w.TilesWide; x++)
        {
            int topmostLayer = -1;
            for (int y = 0; y < w.TilesHigh; y++)
            {
                if (w.Tiles[x, y].IsActive) { topmostLayer = y; break; }
            }

            if (topmostLayer < 0 || topmostLayer >= w.TilesHigh) continue;

            if (topmostLayer > minHillY && topmostLayer < w.RockLevel)
                minHillY = topmostLayer;

            if (generateGrass)
            {
                for (int y = topmostLayer; y < w.TilesHigh; y++)
                {
                    if (w.Tiles[x, y].Type == (ushort)Tile.TileType.DirtBlock)
                    {
                        w.Tiles[x, y] = new Tile { IsActive = true, Type = (ushort)Tile.TileType.GrassBlock };
                        bool leftAir  = x - 1 >= 0        && !w.Tiles[x - 1, y].IsActive;
                        bool rightAir = x + 1 < w.TilesWide && !w.Tiles[x + 1, y].IsActive;
                        if (!leftAir && !rightAir) break;
                    }
                }
            }

            if (generateWalls)
            {
                for (int y = topmostLayer; y < w.TilesHigh; y++)
                {
                    w.Tiles[x, y].Wall = (ushort)Tile.WallType.Sky;
                    if (x - 1 >= 0)        w.Tiles[x - 1, y].Wall = (ushort)Tile.WallType.Sky;
                    if (x + 1 < w.TilesWide) w.Tiles[x + 1, y].Wall = (ushort)Tile.WallType.Sky;
                    bool leftAir  = x - 1 >= 0        && !w.Tiles[x - 1, y].IsActive;
                    bool rightAir = x + 1 < w.TilesWide && !w.Tiles[x + 1, y].IsActive;
                    if (!leftAir && !rightAir) break;
                }
            }
        }

        w.GroundLevel = minHillY;
    }

    // ─── Cave generation ──────────────────────────────────────────────────────

    private static void GenerateCaves(World w, Random rand,
        double caveNoise, double caveMultiplier, double caveDensity,
        bool surfaceCaves, bool skipUnderworld,
        bool generateGrass, bool generateWalls,
        IProgress<ProgressChangedEventArgs>? progress)
    {
        int width = w.TilesWide, height = w.TilesHigh;
        double noiseThreshold = caveMultiplier * caveDensity;
        var perlin = new PerlinNoise(rand.Next());

        // Build noise map
        double[,] caveMap = new double[width, height];
        for (int y = 0; y < height; y++)
        {
            Report(progress, Pct(y, height), "Generating cave noise…");
            for (int x = 0; x < width; x++)
                caveMap[x, y] = perlin.Noise(x * caveNoise, y * caveNoise);
        }

        // Cellular automata refinement
        for (int iter = 0; iter < 5; iter++)
        {
            double[,] next = new double[width, height];
            for (int y = 1; y < height - 1; y++)
            {
                Report(progress, Pct(y, height), "Refining caves…");
                for (int x = 1; x < width - 1; x++)
                {
                    int neighbors = CountActiveNeighbors(caveMap, x, y);
                    next[x, y] = caveMap[x, y] > noiseThreshold ? 1.0
                                 : neighbors >= 4 ? 1.0 : 0.0;
                }
            }
            caveMap = next;
        }

        double topDirtLayer = w.GroundLevel + perlin.Noise(0, 0) * w.HillSize;

        // Apply to world
        for (int y = 0; y < height; y++)
        {
            Report(progress, Pct(y, height), "Placing caves…");
            if (!surfaceCaves && y < w.RockLevel) continue;
            if (skipUnderworld && y > height - 200) continue;

            for (int x = 0; x < width; x++)
            {
                if (caveMap[x, y] <= 0.5) continue;

                for (int dy = (int)-caveMultiplier; dy <= (int)caveMultiplier; dy++)
                for (int dx = (int)-caveMultiplier; dx <= (int)caveMultiplier; dx++)
                {
                    int nx = x + dx, ny = y + dy;
                    if ((uint)nx >= (uint)width || (uint)ny >= (uint)height) continue;

                    w.Tiles[nx, ny].IsActive = false;

                    if (generateGrass && w.Tiles[nx, ny].Type != (ulong)Tile.TileType.GrassBlock)
                        w.Tiles[nx, ny].Type = (ushort)Tile.TileType.DirtBlock;

                    if (generateWalls && w.Tiles[nx, ny].IsActive)
                    {
                        if (y < w.RockLevel && y != topDirtLayer)
                            w.Tiles[nx, ny].Wall = (ushort)Tile.WallType.DirtWall;
                        else if (y >= w.RockLevel)
                            w.Tiles[nx, ny].Wall = (ushort)Tile.WallType.StoneWall;
                    }
                }
            }
        }
    }

    private static int CountActiveNeighbors(double[,] map, int x, int y)
    {
        int count = 0;
        for (int dy = -1; dy <= 1; dy++)
        for (int dx = -1; dx <= 1; dx++)
        {
            if (dx == 0 && dy == 0) continue;
            int nx = x + dx, ny = y + dy;
            if ((uint)nx < (uint)map.GetLength(0) && (uint)ny < (uint)map.GetLength(1))
                if (map[nx, ny] > 0.5) count++;
        }
        return count;
    }

    // ─── Underworld generation ────────────────────────────────────────────────

    private static void GenerateUnderworld(World w, Random rand,
        bool generateLava,
        double roofNoiseScale, double floorNoiseScale, double lavaNoiseScale,
        IProgress<ProgressChangedEventArgs>? progress)
    {
        int width = w.TilesWide, height = w.TilesHigh;
        int uwStart = height - 200;
        int roofBase  = uwStart + 20;
        int floorBase = height - 80;

        var groundNoise = new PerlinNoise(rand.Next());
        var roofNoise   = new PerlinNoise(rand.Next());
        var lavaNoise   = new PerlinNoise(rand.Next());

        for (int y = uwStart; y < height; y++)
        {
            Report(progress, Pct(y, height), "Generating underworld…");
            for (int x = 0; x < width; x++)
            {
                double groundH = floorBase - groundNoise.Noise(x * floorNoiseScale, 0) * 10;
                double roofH   = roofBase  - roofNoise.Noise(x * roofNoiseScale, 0)   * 15;

                if (y >= groundH)
                {
                    w.Tiles[x, y] = new Tile { IsActive = true, Type = (ushort)Tile.TileType.AshBlock };
                    double lv = lavaNoise.Noise(x * lavaNoiseScale, y * lavaNoiseScale);
                    if (generateLava && y >= floorBase + 2 && lv > 0.1)
                    {
                        w.Tiles[x, y].LiquidType   = LiquidType.Lava;
                        w.Tiles[x, y].LiquidAmount  = 255;
                    }
                }
                else if (y < groundH && y >= roofH)
                {
                    w.Tiles[x, y] = new Tile { IsActive = false };
                }
                else
                {
                    w.Tiles[x, y] = new Tile { IsActive = true, Type = (ushort)Tile.TileType.AshBlock };
                }
            }
        }
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static int Pct(int current, int total) =>
        total == 0 ? 0 : (int)((double)current / total * 100);

    private static void Report(IProgress<ProgressChangedEventArgs>? progress, int pct, string msg) =>
        progress?.Report(new ProgressChangedEventArgs(pct, msg));

    // ─── Perlin noise (self-contained, no XNA) ────────────────────────────────

    private sealed class PerlinNoise
    {
        private static readonly int[] _default = {
            151,160,137,91,90,15,131,13,201,95,96,53,194,233,7,225,140,36,103,30,
            69,142,8,99,37,240,21,10,23,190,6,148,247,120,234,75,0,26,197,62,94,
            252,219,203,117,35,11,32,57,177,33,88,237,149,56,87,174,20,125,136,171,
            168,68,175,74,165,71,134,139,48,27,166,77,146,158,231,83,111,229,122,60,
            211,133,230,220,105,92,41,55,46,245,40,244,102,143,54,65,25,63,161,1,216,
            80,73,209,76,132,187,208,89,18,169,200,196,135,130,116,188,159,86,164,100,
            109,198,173,186,3,64,52,217,226,250,124,123,5,202,38,147,118,126,255,82,
            85,212,207,206,59,227,47,16,58,17,182,189,28,42,223,183,170,213,119,248,
            152,2,44,154,163,70,221,153,101,155,167,43,172,9,129,22,39,253,19,98,108,
            110,79,113,224,232,178,185,112,104,218,246,97,228,251,34,242,193,238,210,
            144,12,191,179,162,241,81,51,145,235,249,14,239,107,49,192,214,31,181,199,
            106,157,184,84,204,176,115,121,50,45,127,4,150,254,138,236,205,93,222,114,
            67,29,24,72,243,141,128,195,78,66,215,61,156,180
        };

        private readonly int[] _p;

        public PerlinNoise(int seed)
        {
            _p = new int[512];
            var r = new Random(seed);
            for (int i = 0; i < 256; i++) _p[i] = _default[i];
            for (int i = 0; i < 256; i++)
            {
                int j = r.Next(256);
                (_p[i], _p[j]) = (_p[j], _p[i]);
            }
            for (int i = 0; i < 256; i++) _p[256 + i] = _p[i];
        }

        public double Noise(double x, double y)
        {
            int X = (int)Math.Floor(x) & 255;
            int Y = (int)Math.Floor(y) & 255;
            x -= Math.Floor(x);
            y -= Math.Floor(y);
            double u = Fade(x), v = Fade(y);
            int A = (_p[X]     + Y) & 255;
            int B = (_p[X + 1] + Y) & 255;
            return Lerp(v,
                Lerp(u, Grad(_p[A],     x,     y), Grad(_p[B],     x - 1, y)),
                Lerp(u, Grad(_p[A + 1], x,     y - 1), Grad(_p[B + 1], x - 1, y - 1)));
        }

        private static double Fade(double t) => t * t * t * (t * (t * 6 - 15) + 10);
        private static double Lerp(double t, double a, double b) => a + t * (b - a);
        private static double Grad(int hash, double x, double y)
        {
            int h = hash & 15;
            double u = h < 8 ? x : y;
            double v = h < 4 ? y : h is 12 or 14 ? x : 0;
            return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
        }
    }
}
