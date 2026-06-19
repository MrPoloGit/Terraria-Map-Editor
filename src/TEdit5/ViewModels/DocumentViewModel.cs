using System.ComponentModel;
using Avalonia;
using TEdit5.Controls;
using TEdit.Editor;
using TEdit.Editor.Undo;
using TEdit.Terraria;

namespace TEdit5.ViewModels;

public partial class DocumentViewModel : ReactiveObject
{
    [Reactive] private int _zoom = 100;
    [Reactive] private int _minZoom = 7;
    [Reactive] private int _maxZoom = 6400;
    [Reactive] private Point _cursorTileCoordinate;
    [Reactive] private SkiaWorldRenderBox.SelectionModes _selectionMode;
    [Reactive] private Rect _selectionRect;

    public ToolSelectionViewModel ToolSelection { get; }
    public TilePicker TilePicker { get; }
    public ISelection Selection { get; }
    [Reactive] private World _world;
    [Reactive] private WorldEditor _worldEditor;

    public DocumentViewModel(World world, ToolSelectionViewModel toolSelection, TilePicker tilePicker)
    {
        _world = world;
        ToolSelection = toolSelection;
        TilePicker = tilePicker;
        Selection = new Selection();
        IUndoManager undoManager = null;

        _worldEditor = new WorldEditor(tilePicker, new TEdit.Editor.TileMaskSettings(), World, Selection, undoManager, (x, y, height, width) => { });

        // Mirror Selection.SelectionArea → SelectionRect so the render box can display it
        if (Selection is INotifyPropertyChanged npc)
        {
            npc.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName is null or nameof(ISelection.SelectionArea))
                {
                    var a = Selection.SelectionArea;
                    SelectionRect = new Rect(a.X, a.Y, a.Width, a.Height);
                }
            };
        }
    }
}
