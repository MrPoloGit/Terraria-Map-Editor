using System.IO;
using Avalonia.Media.Imaging;
using SkiaSharp;
using TEdit.Editor.Clipboard;
using TEdit.Terraria;
using TEdit5.Controls.WorldRenderEngine;

namespace TEdit5.Controls;

/// <summary>
/// Renders a ClipboardBuffer to a bitmap for thumbnail display and schematic viewing.
/// </summary>
public static class SchematicRenderer
{
    private static readonly TEdit.Common.TEditColor _background = new(30, 30, 46, 255);

    /// <summary>
    /// Renders every tile at 1 pixel each, then scales down to a thumbnail of at most maxSize pixels on the longest side.
    /// </summary>
    public static Bitmap RenderThumbnail(ClipboardBuffer buffer, int maxSize = 200)
    {
        var full = RenderFull(buffer);
        int w = buffer.Size.X, h = buffer.Size.Y;

        if (w <= maxSize && h <= maxSize)
            return ToAvaloniaBitmap(full);

        double scale = w >= h ? (double)maxSize / w : (double)maxSize / h;
        int tw = (int)(w * scale);
        int th = (int)(h * scale);

        using var thumb = new SKBitmap(tw, th, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(thumb);
        canvas.Clear();
        canvas.DrawBitmap(full, new SKRect(0, 0, tw, th),
            new SKPaint { FilterQuality = SKFilterQuality.Low });

        return ToAvaloniaBitmap(thumb);
    }

    /// <summary>
    /// Renders every tile at 1 pixel each — suitable for the viewer world.
    /// </summary>
    public static SKBitmap RenderFull(ClipboardBuffer buffer)
    {
        int w = buffer.Size.X, h = buffer.Size.Y;
        var bmp = new SKBitmap(w, h, SKColorType.Bgra8888, SKAlphaType.Premul);

        for (int x = 0; x < w; x++)
        for (int y = 0; y < h; y++)
        {
            var tile  = buffer.Tiles[x, y];
            var color = PixelMap.GetTileColor(tile, _background);
            bmp.SetPixel(x, y, color.ToSKColor().WithAlpha(255));
        }

        return bmp;
    }

    public static Bitmap ToAvaloniaBitmap(SKBitmap skBitmap)
    {
        using var image = SKImage.FromBitmap(skBitmap);
        using var data  = image.Encode(SKEncodedImageFormat.Png, 100);
        using var ms    = new MemoryStream();
        data.SaveTo(ms);
        ms.Position = 0;
        return new Bitmap(ms);
    }
}
