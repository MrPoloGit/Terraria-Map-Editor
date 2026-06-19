using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Reactive;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using TEdit.Editor.Clipboard;
using TEdit.Geometry;
using TEdit.Editor.Undo;
using TEdit5.Services;

namespace TEdit5.ViewModels;

public partial class ClipboardViewModel : ReactiveObject
{
    private readonly IDocumentService _documentService;

    [Reactive] private bool _instantPaste;
    [Reactive] private ClipboardBufferEntry? _activeBuffer;

    public ObservableCollection<ClipboardBufferEntry> LoadedBuffers { get; } = new();
    public PasteOptions PasteOptions { get; } = new();

    public event Action<ClipboardBufferEntry>? ExportRequested;
    public event Action? ImportRequested;

    public ReactiveCommand<Unit, Unit> EmptyClipboardCommand { get; }
    public ReactiveCommand<Unit, Unit> ImportSchematicCommand { get; }
    public ReactiveCommand<Unit, Unit> CopySelectionCommand { get; }

    public ClipboardViewModel(IDocumentService documentService)
    {
        _documentService = documentService;
        EmptyClipboardCommand    = ReactiveCommand.Create(ClearAll);
        ImportSchematicCommand   = ReactiveCommand.Create(() => ImportRequested?.Invoke());
        CopySelectionCommand     = ReactiveCommand.Create(CopySelection);
    }

    public void ImportFromPath(string path)
    {
        var buffer = ClipboardBuffer.Load(path);
        if (buffer == null) return;

        if (string.IsNullOrWhiteSpace(buffer.Name))
            buffer.Name = Path.GetFileNameWithoutExtension(path);

        var entry = new ClipboardBufferEntry(buffer, this);
        LoadedBuffers.Add(entry);
        ActiveBuffer = entry;
    }

    public void SetActive(ClipboardBufferEntry entry)
    {
        ActiveBuffer = entry;
    }

    public void RequestExport(ClipboardBufferEntry entry)
    {
        ExportRequested?.Invoke(entry);
    }

    public void ExportToPath(ClipboardBufferEntry entry, string path)
    {
        entry.Buffer.Save(path, null);
    }

    public void RemoveEntry(ClipboardBufferEntry entry)
    {
        LoadedBuffers.Remove(entry);
        if (ActiveBuffer == entry)
            ActiveBuffer = LoadedBuffers.Count > 0 ? LoadedBuffers[^1] : null;
    }

    public void ClearAll()
    {
        LoadedBuffers.Clear();
        ActiveBuffer = null;
    }

    private void CopySelection()
    {
        var doc = _documentService.SelectedDocument;
        if (doc?.World == null) return;
        if (!doc.Selection.IsActive) return;

        var area = doc.Selection.SelectionArea;
        if (area.Width <= 0 || area.Height <= 0) return;

        var buffer = ClipboardBuffer.GetSelectionBuffer(doc.World, area);
        buffer.Name = $"Selection {area.Width}x{area.Height}";

        var entry = new ClipboardBufferEntry(buffer, this);
        LoadedBuffers.Add(entry);
        ActiveBuffer = entry;
    }

    public void PasteAtPosition(TEdit.Terraria.World world, Vector2Int32 anchor, IUndoManager? undo = null)
    {
        ActiveBuffer?.Buffer.Paste(world, anchor, undo, PasteOptions);
    }
}
