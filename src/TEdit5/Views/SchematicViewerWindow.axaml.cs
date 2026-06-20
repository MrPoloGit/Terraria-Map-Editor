using System.IO;
using Avalonia.Controls;
using TEdit.Editor.Clipboard;
using TEdit5.ViewModels;

namespace TEdit5.Views;

public partial class SchematicViewerWindow : Window
{
    public SchematicViewerWindow()
    {
        InitializeComponent();
    }

    public static SchematicViewerWindow CreateFromBuffer(ClipboardBuffer buffer)
    {
        var vm = new SchematicViewerViewModel(buffer);
        return new SchematicViewerWindow { DataContext = vm };
    }

    public static SchematicViewerWindow CreateFromPath(string path)
    {
        var buffer = ClipboardBuffer.Load(path)
                     ?? new ClipboardBuffer(new TEdit.Geometry.Vector2Int32(1, 1), true);

        if (string.IsNullOrWhiteSpace(buffer.Name))
            buffer.Name = Path.GetFileNameWithoutExtension(path);

        return CreateFromBuffer(buffer);
    }
}
