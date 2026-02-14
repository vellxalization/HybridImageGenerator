using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using HybridImageGenerator.Models.ErrorHandling;
using HybridImageGenerator.Models.ImageProcessing;
using HybridImageGenerator.Models.ImageProcessing.Editor;
using HybridImageGenerator.Models.ImageProcessing.Saving;
using HybridImageGenerator.ViewModels;
using HybridImageGenerator.ViewModels.ErrorHandling;
using HybridImageGenerator.Views;

namespace HybridImageGenerator;

public partial class App : Application {
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
        ErrorDispatcher dispatcher = new();
        ImageEditor editor = new(new EditedImageSaver());
        
        MainWindow mainWindow = new();
        ImageFileService imageFileService = new(StorageProviderGetter);
        EditorViewModel editorViewModel = new(imageFileService, editor, dispatcher);
        mainWindow.DataContext = new MainViewModel(editorViewModel, new DesktopErrorViewModel(dispatcher));
        
        return mainWindow;
        
        IStorageProvider StorageProviderGetter() => mainWindow.StorageProvider;
    }
    
    private static MainView CreateWebMainView() {
        ErrorDispatcher dispatcher = new();
        ImageEditor editor = new(new EditedImageSaver());
        
        MainView mainView = new();
        ImageFileService imageFileService = new(StorageProviderGetter);
        EditorViewModel editorViewModel = new(imageFileService, editor, dispatcher);
        mainView.DataContext = new MainViewModel(editorViewModel, new WebErrorViewModel(dispatcher));
        
        return mainView;
        
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