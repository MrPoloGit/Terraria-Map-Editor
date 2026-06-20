using System.Reactive;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using ReactiveUI;
using TEdit.Editor.Clipboard;
using TEdit5.Controls;

namespace TEdit5.ViewModels;

public class ClipboardBufferEntry : ReactiveObject
{
    private readonly ClipboardViewModel _parent;

    private ClipboardBuffer _buffer;
    public ClipboardBuffer Buffer
    {
        get => _buffer;
        set
        {
            this.RaiseAndSetIfChanged(ref _buffer, value);
            this.RaisePropertyChanged(nameof(Name));
            RegenerateThumbnail(value);
        }
    }

    private Bitmap? _previewBitmap;
    public Bitmap? PreviewBitmap
    {
        get => _previewBitmap;
        private set => this.RaiseAndSetIfChanged(ref _previewBitmap, value);
    }

    public string Name => Buffer?.Name ?? "(unnamed)";

    public ClipboardBufferEntry(ClipboardBuffer buffer, ClipboardViewModel parent)
    {
        _parent  = parent;
        _buffer  = buffer;

        SetActiveCommand       = ReactiveCommand.Create(() => _parent.SetActive(this));
        ViewSchematicCommand   = ReactiveCommand.Create(() => _parent.OpenViewer(this));
        ExportSchematicCommand = ReactiveCommand.Create(() => _parent.RequestExport(this));
        RemoveSchematicCommand = ReactiveCommand.Create(() => _parent.RemoveEntry(this));
        FlipXCommand  = ReactiveCommand.Create(FlipX);
        FlipYCommand  = ReactiveCommand.Create(FlipY);
        RotateCommand = ReactiveCommand.Create(Rotate);

        RegenerateThumbnail(buffer);
    }

    private void FlipX()
    {
        var flipped = Buffer.FlipX();
        flipped.Name = Buffer.Name;
        Buffer = flipped;
    }

    private void FlipY()
    {
        var flipped = Buffer.FlipY();
        flipped.Name = Buffer.Name;
        Buffer = flipped;
    }

    private void Rotate()
    {
        var rotated = Buffer.Rotate();
        rotated.Name = Buffer.Name;
        Buffer = rotated;
    }

    private void RegenerateThumbnail(ClipboardBuffer buf)
    {
        _ = Task.Run(() => SchematicRenderer.RenderThumbnail(buf, maxSize: 192))
            .ContinueWith(t =>
            {
                if (!t.IsFaulted)
                    Dispatcher.UIThread.Post(() => PreviewBitmap = t.Result);
            });
    }

    public ReactiveCommand<Unit, Unit> SetActiveCommand { get; }
    public ReactiveCommand<Unit, Unit> ViewSchematicCommand { get; }
    public ReactiveCommand<Unit, Unit> ExportSchematicCommand { get; }
    public ReactiveCommand<Unit, Unit> RemoveSchematicCommand { get; }
    public ReactiveCommand<Unit, Unit> FlipXCommand { get; }
    public ReactiveCommand<Unit, Unit> FlipYCommand { get; }
    public ReactiveCommand<Unit, Unit> RotateCommand { get; }
}
