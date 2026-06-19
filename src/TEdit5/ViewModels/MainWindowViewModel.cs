using TEdit5.Controls.WorldRenderEngine.Layers;
using TEdit5.Services;

namespace TEdit5.ViewModels;

public partial class MainWindowViewModel : ReactiveObject
{
    public IDocumentService DocumentService { get; }
    public ClipboardViewModel Clipboard { get; }

    [Reactive] private DocumentViewModel? _selectedDocument;
    [Reactive] private ToolSelectionViewModel _toolSelection;
    [Reactive] private int _progressPercentage;
    [Reactive] private string _progressText = string.Empty;
    [Reactive] private RenderLayerVisibility _renderLayerVisibility = new();

    public MainWindowViewModel(
        IDocumentService documentService,
        ToolSelectionViewModel toolSelection,
        ClipboardViewModel clipboard)
    {
        DocumentService = documentService;
        _toolSelection = toolSelection;
        Clipboard = clipboard;

        // Keep DocumentService.SelectedDocument in sync when the tab selection changes
        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SelectedDocument))
                DocumentService.SelectedDocument = SelectedDocument;
        };
    }
}
