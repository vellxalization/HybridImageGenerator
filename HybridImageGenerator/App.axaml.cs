using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using HybridImageGenerator.Models;
using HybridImageGenerator.ViewModels;
using HybridImageGenerator.Views;

namespace HybridImageGenerator;

public partial class App : Application {
    public override void Initialize() {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted() {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();
            var mainWindow = new MainWindow();
            IStorageProvider StorageProviderGetter() => mainWindow.StorageProvider;
            var imageFileService = new ImageFileService(StorageProviderGetter);
            mainWindow.DataContext = new MainViewModel(imageFileService);
            desktop.MainWindow = mainWindow;
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform) {
            var mainView = new MainView();
            IStorageProvider StorageProviderGetter() => TopLevel.GetTopLevel(mainView)!.StorageProvider;
            var imageFileService = new ImageFileService(StorageProviderGetter);
            mainView.DataContext = new MainViewModel(imageFileService);
            singleViewPlatform.MainView = mainView;
        }

        base.OnFrameworkInitializationCompleted();
    }
    
    private void DisableAvaloniaDataAnnotationValidation() {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove) {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}