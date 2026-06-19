using Avalonia.Platform.Storage;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using TEdit5.ViewModels;
using TEdit.Editor;
using TEdit.Terraria;

namespace TEdit5.Services;

public interface IDialogService
{
    Task<string> OpenFileDialogAsync();
}

public class DialogService : IDialogService
{
    public async Task<string> OpenFileDialogAsync() => await Task.FromResult("test");
}

public interface IDocumentService
{
    ObservableCollection<DocumentViewModel> Documents { get; }
    DocumentViewModel? SelectedDocument { get; set; }
    Task LoadWorldAsync(IStorageFile file, IProgress<ProgressChangedEventArgs>? progress = null);
    Task CreateWorldAsync(TEdit.Terraria.World worldTemplate, IProgress<ProgressChangedEventArgs>? progress = null);
}


public partial class DocumentService : ReactiveObject, IDocumentService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public ObservableCollection<DocumentViewModel> Documents { get; } = new();

    [Reactive] private DocumentViewModel? _selectedDocument;

    public DocumentService(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }


    public async Task LoadWorldAsync(IStorageFile file, IProgress<ProgressChangedEventArgs>? progress = null)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var toolSelection = scope.ServiceProvider.GetRequiredService<ToolSelectionViewModel>();
        var tilePicker    = scope.ServiceProvider.GetRequiredService<TilePicker>();

        (var world, var errors) = await World.LoadWorldAsync(file.TryGetLocalPath(), progress: progress);

        if (world != null)
            Documents.Add(new DocumentViewModel(world, toolSelection, tilePicker));

        if (errors != null)
            Debug.WriteLine("Error loading world: " + errors);
    }

    public async Task CreateWorldAsync(TEdit.Terraria.World worldTemplate, IProgress<ProgressChangedEventArgs>? progress = null)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var toolSelection = scope.ServiceProvider.GetRequiredService<ToolSelectionViewModel>();
        var tilePicker    = scope.ServiceProvider.GetRequiredService<TilePicker>();

        var world = await Task.Run(() => TEdit.Editor.WorldGenerator.Generate(worldTemplate, progress));

        Documents.Add(new DocumentViewModel(world, toolSelection, tilePicker));
        SelectedDocument = Documents[^1];
    }
}
