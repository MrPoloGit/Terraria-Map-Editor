using System.Reactive;
using ReactiveUI;
using TEdit.Editor.Clipboard;

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
        }
    }

    public string Name => Buffer?.Name ?? "(unnamed)";

    public ClipboardBufferEntry(ClipboardBuffer buffer, ClipboardViewModel parent)
    {
        _parent = parent;
        _buffer = buffer;

        SetActiveCommand  = ReactiveCommand.Create(() => _parent.SetActive(this));
        ExportSchematicCommand = ReactiveCommand.Create(() => _parent.RequestExport(this));
        RemoveSchematicCommand = ReactiveCommand.Create(() => _parent.RemoveEntry(this));
        FlipXCommand  = ReactiveCommand.Create(() => { Buffer = Buffer.FlipX();  Buffer.Name = Name + " (FlipX)";  });
        FlipYCommand  = ReactiveCommand.Create(() => { Buffer = Buffer.FlipY();  Buffer.Name = Name + " (FlipY)";  });
        RotateCommand = ReactiveCommand.Create(() => { Buffer = Buffer.Rotate(); Buffer.Name = Name + " (Rot)"; });
    }

    public ReactiveCommand<Unit, Unit> SetActiveCommand { get; }
    public ReactiveCommand<Unit, Unit> ExportSchematicCommand { get; }
    public ReactiveCommand<Unit, Unit> RemoveSchematicCommand { get; }
    public ReactiveCommand<Unit, Unit> FlipXCommand { get; }
    public ReactiveCommand<Unit, Unit> FlipYCommand { get; }
    public ReactiveCommand<Unit, Unit> RotateCommand { get; }
}
