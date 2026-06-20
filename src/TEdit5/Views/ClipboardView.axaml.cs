using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using System.Collections.Generic;
using System.Threading.Tasks;
using TEdit5.ViewModels;

namespace TEdit5.Views;

public partial class ClipboardView : UserControl
{
    private ClipboardViewModel Vm => (ClipboardViewModel)DataContext!;

    public ClipboardView()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(System.EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is ClipboardViewModel vm)
        {
            vm.ExportRequested  += OnExportRequested;
            vm.ViewerRequested  += OnViewerRequested;
        }
    }

    private async void OnExportRequested(ClipboardBufferEntry entry)
    {
        var topLevel = TopLevel.GetTopLevel(this)!;
        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export Schematic",
            SuggestedFileName = entry.Name,
            DefaultExtension = "TEditSch",
            FileTypeChoices = new List<FilePickerFileType>
            {
                new FilePickerFileType("TEdit Schematic") { Patterns = new[] { "*.TEditSch" } }
            }
        });

        if (file != null)
            Vm.ExportToPath(entry, file.Path.LocalPath);
    }

    private void OnViewerRequested(ClipboardBufferEntry entry)
    {
        var window = SchematicViewerWindow.CreateFromBuffer(entry.Buffer);
        var owner  = TopLevel.GetTopLevel(this) as Window;
        if (owner != null)
            window.Show(owner);
        else
            window.Show();
    }

    public async void ImportButton_Clicked(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this)!;
        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Import Schematic",
            AllowMultiple = false,
            FileTypeFilter = new List<FilePickerFileType>
            {
                new FilePickerFileType("TEdit Schematic") { Patterns = new[] { "*.TEditSch" } },
                new FilePickerFileType("All Files") { Patterns = new[] { "*" } }
            }
        });

        if (files.Count == 1)
            Vm.ImportFromPath(files[0].Path.LocalPath);
    }
}
