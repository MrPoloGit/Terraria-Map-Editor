using System;
using System.Reactive;
using Avalonia.Media.Imaging;
using ReactiveUI;
using TEdit.Editor.Clipboard;
using TEdit5.Controls;

namespace TEdit5.ViewModels;

public class SchematicViewerViewModel : ReactiveObject
{
    private readonly ClipboardBuffer _buffer;

    public Bitmap SchematicBitmap { get; }
    public string Title    { get; }
    public string SizeInfo { get; }

    private double _zoom;
    public double Zoom
    {
        get => _zoom;
        set
        {
            value = Math.Clamp(value, 0.25, 64);
            this.RaiseAndSetIfChanged(ref _zoom, value);
            this.RaisePropertyChanged(nameof(DisplayWidth));
            this.RaisePropertyChanged(nameof(DisplayHeight));
            this.RaisePropertyChanged(nameof(ZoomLabel));
        }
    }

    public double DisplayWidth  => _buffer.Size.X * _zoom;
    public double DisplayHeight => _buffer.Size.Y * _zoom;
    public string ZoomLabel     => $"{_zoom:0.##}×";

    public ReactiveCommand<Unit, Unit> ZoomInCommand  { get; }
    public ReactiveCommand<Unit, Unit> ZoomOutCommand { get; }
    public ReactiveCommand<Unit, Unit> ZoomFitCommand { get; }

    public SchematicViewerViewModel(ClipboardBuffer buffer)
    {
        _buffer   = buffer;
        Title     = buffer.Name ?? "Schematic";
        SizeInfo  = $"{buffer.Size.X} × {buffer.Size.Y} tiles";

        // Render at 1 pixel per tile; the Image control is scaled via DisplayWidth/Height.
        using var skBmp = SchematicRenderer.RenderFull(buffer);
        SchematicBitmap = SchematicRenderer.ToAvaloniaBitmap(skBmp);

        // Default zoom: fit the larger dimension inside ~600 px, minimum 1×.
        _zoom = Math.Max(1, Math.Min(16,
            600.0 / Math.Max(buffer.Size.X, buffer.Size.Y)));

        ZoomInCommand  = ReactiveCommand.Create(() => { Zoom = Zoom >= 1 ? Math.Round(Zoom) + 1 : Zoom * 2; });
        ZoomOutCommand = ReactiveCommand.Create(() => { Zoom = Zoom > 1  ? Math.Round(Zoom) - 1 : Zoom / 2; });
        ZoomFitCommand = ReactiveCommand.Create(() =>
        {
            Zoom = Math.Max(1, Math.Min(16,
                600.0 / Math.Max(buffer.Size.X, buffer.Size.Y)));
        });
    }
}
