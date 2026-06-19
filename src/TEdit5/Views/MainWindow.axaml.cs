using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using TEdit.Terraria;
using TEdit5.ViewModels;
using TEdit5.Services;

namespace TEdit5.Views;

public partial class MainWindow : Window
{
    protected MainWindowViewModel MainWindowViewModel => (MainWindowViewModel)this.DataContext!;

    public MainWindow()
    {
        InitializeComponent();
    }

    public async void NewWorldButton_Clicked(object sender, RoutedEventArgs args)
    {
        var vm = new NewWorldViewModel();
        var dialog = new NewWorldView { DataContext = vm };
        await dialog.ShowDialog(this);

        if (!dialog.Confirmed) return;

        var progress = new Progress<ProgressChangedEventArgs>(e =>
        {
            MainWindowViewModel.ProgressPercentage = e.ProgressPercentage;
            MainWindowViewModel.ProgressText = e.UserState?.ToString() ?? string.Empty;
        });

        await MainWindowViewModel.DocumentService.CreateWorldAsync(vm.BuildWorld(), progress);

        ((IProgress<ProgressChangedEventArgs>)progress).Report(new ProgressChangedEventArgs(0, string.Empty));
    }

    public async void LoadWorldButton_Clicked(object sender, RoutedEventArgs args)
    {
        await OpenWorldDialog();
    }

    public async void SaveWorldButton_Clicked(object sender, RoutedEventArgs args)
    {
        var doc = MainWindowViewModel.SelectedDocument;
        if (doc?.World == null) return;

        var topLevel = TopLevel.GetTopLevel(this)!;

        var suggestedName = string.Join("-", doc.World.Title.Split(Path.GetInvalidFileNameChars()));

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save World",
            SuggestedFileName = suggestedName,
            DefaultExtension = "wld",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("Terraria World") { Patterns = new[] { "*.wld" } }
            }
        });

        if (file == null) return;

        var progress = new Progress<ProgressChangedEventArgs>(e =>
        {
            MainWindowViewModel.ProgressPercentage = e.ProgressPercentage;
            MainWindowViewModel.ProgressText = e.UserState?.ToString() ?? string.Empty;
        });

        MainWindowViewModel.ProgressText = "Saving…";
        await World.SaveAsync(doc.World, file.Path.LocalPath, progress: progress);
        ((IProgress<ProgressChangedEventArgs>)progress).Report(new ProgressChangedEventArgs(0, $"Saved: {file.Name}"));
    }

    private async Task OpenWorldDialog()
    {
        var fileTypes = new List<FilePickerFileType>
        {
            new FilePickerFileType("World File")
            {
                Patterns = new [] { "*.wld" },
                AppleUniformTypeIdentifiers = new [] { "wld" },
                MimeTypes = new []{ "application/octet-stream" }
            },
        };

        // Get top level from the current control. Alternatively, you can use Window reference instead.
        var topLevel = TopLevel.GetTopLevel(this)!;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Text File",
            AllowMultiple = false,
            FileTypeFilter = fileTypes
        });

        if (files.Count == 1)
        {
            var file = files[0];

            await LoadWorld(file);
        }
    }
    public async Task LoadWorldFromPath(string path)
    {
        var topLevel = TopLevel.GetTopLevel(this)!;
        var file = await topLevel.StorageProvider.TryGetFileFromPathAsync(path);
        if (file != null)
            await LoadWorld(file);
    }

    private async Task LoadWorld(IStorageFile file)
    {
        var progress = new Progress<ProgressChangedEventArgs>(ProgressChangedEventArgs =>
               {
                   MainWindowViewModel.ProgressPercentage = ProgressChangedEventArgs.ProgressPercentage;
                   MainWindowViewModel.ProgressText = ProgressChangedEventArgs.UserState?.ToString() ?? string.Empty;
               });

        await MainWindowViewModel.DocumentService.LoadWorldAsync(file, progress);

        ((IProgress<ProgressChangedEventArgs>)progress).Report(new ProgressChangedEventArgs(0, string.Empty));
    }
}
