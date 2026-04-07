using System;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
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
    public const int DefaultDiscordPCFullScreenInnerWidth = 1920 - VerticalTaskBarWidth;
    public const int DefaultDiscordPCFullScreenInnerHeight = 1080 - HorizontalTaskBarHeight;
    
    public override void Initialize() {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted() {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
            DisableAvaloniaDataAnnotationValidation();

            desktop.MainWindow = CreateDesktopMainWindow();
            AppDomain.CurrentDomain.UnhandledException += (_, e) => LogFatalDesktop((e.ExceptionObject as Exception)!, "EX_");
            Dispatcher.UIThread.UnhandledException += (_, e) => LogFatalDesktop(e.Exception, "UI_");
            TaskScheduler.UnobservedTaskException += (_, e) => LogFatalDesktop(e.Exception, "TK_");
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform) {
            singleViewPlatform.MainView = CreateWebMainView();
            AppDomain.CurrentDomain.UnhandledException += (_, e) => LogFatalWeb((e.ExceptionObject as Exception)!, "EX_");
            Dispatcher.UIThread.UnhandledException += (_, e) => LogFatalWeb(e.Exception, "UI_");
            TaskScheduler.UnobservedTaskException += (_, e) => LogFatalWeb(e.Exception, "TK_");
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static MainWindow CreateDesktopMainWindow() {
        ImageEditor editor = new(new EditedImageSaver());
        DiscordImageRescaler rescaler = new(DefaultDiscordPCFullScreenInnerWidth, DefaultDiscordPCFullScreenInnerHeight);
        
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
        DiscordImageRescaler rescaler = new(DefaultDiscordPCFullScreenInnerWidth, DefaultDiscordPCFullScreenInnerHeight);
        
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
    
    private static void LogFatalDesktop(Exception ex, string tag = "") {
        using var file = File.CreateText($"{tag}HybridImageGenerator_{DateTimeOffset.UtcNow.ToFileTime()}.log");
        string msg = ex.ToString().Replace(Environment.UserName, "%USERNAME%", StringComparison.OrdinalIgnoreCase);
        file.WriteLine(msg);
    }
    
    private static void LogFatalWeb(Exception ex, string tag = "") {
        string msg = $"{tag}_{ex.ToString().Replace(Environment.UserName, "%USERNAME%", StringComparison.OrdinalIgnoreCase)}";
        Console.WriteLine(msg);
    }
}