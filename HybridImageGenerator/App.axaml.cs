using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using HybridImageGenerator.Models;
using HybridImageGenerator.Models.ImageProcessing;
using HybridImageGenerator.Models.ImageProcessing.Editor;
using HybridImageGenerator.Models.ImageProcessing.Saving;
using HybridImageGenerator.ViewModels;
using HybridImageGenerator.ViewModels.ErrorHandling;
using HybridImageGenerator.Views;

namespace HybridImageGenerator;

public partial class App : Application {
    private const int VerticalTaskBarWidth = 70; // because some win10 folks can use vertical sidebar
    private const int HorizontalTaskBarHeight = 60; // this should cover most taskbars on win10 and 11

    // we target default desktop discord client on 1920x1080 fullscreen
    private const int DefaultDiscordPCFullScreenInnerWidth = 1920 - VerticalTaskBarWidth;
    private const int DefaultDiscordPCFullScreenInnerHeight = 1080 - HorizontalTaskBarHeight;
    
    public override void Initialize() {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted() {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
            DisableAvaloniaDataAnnotationValidation();

            desktop.MainWindow = CreateDesktopMainWindow();
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform) {
            singleViewPlatform.MainView = CreateWebMainView();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static MainWindow CreateDesktopMainWindow() {
        ImageEditor editor = new(new EditedImageSaver());
        DiscordFullScreenRescaler rescaler = new(DefaultDiscordPCFullScreenInnerWidth, DefaultDiscordPCFullScreenInnerHeight);
        
        MainWindow mainWindow = new();
        ImageFileService imageFileService = new(StorageProviderGetter);
        EditorViewModel editorViewModel = new(imageFileService, editor, rescaler, ErrorVmCreator);
        mainWindow.DataContext = new MainViewModel(editorViewModel);
        
        return mainWindow;
        
        IStorageProvider StorageProviderGetter() => mainWindow.StorageProvider;
        ErrorViewModel ErrorVmCreator(ErrorDetails details) => new DesktopErrorViewModel(details);
    }
    
    private static MainView CreateWebMainView() {
        ImageEditor editor = new(new EditedImageSaver());
        DiscordFullScreenRescaler rescaler = new(DefaultDiscordPCFullScreenInnerWidth, DefaultDiscordPCFullScreenInnerHeight);
        
        MainView mainView = new();
        ImageFileService imageFileService = new(StorageProviderGetter);
        EditorViewModel editorViewModel = new(imageFileService, editor, rescaler, ErrorVmCreator);
        mainView.DataContext = new MainViewModel(editorViewModel);
        
        return mainView;
        
        ErrorViewModel ErrorVmCreator(ErrorDetails details) => new WebErrorViewModel(details);
        IStorageProvider StorageProviderGetter() => TopLevel.GetTopLevel(mainView)!.StorageProvider;
    }
    
    private void DisableAvaloniaDataAnnotationValidation() {
        // Get an array of plugins to remove
        DataAnnotationsValidationPlugin[] dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (DataAnnotationsValidationPlugin plugin in dataValidationPluginsToRemove) {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}