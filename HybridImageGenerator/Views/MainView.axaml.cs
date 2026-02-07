using Avalonia;
using Avalonia.Controls;
using Avalonia.Reactive;
using HybridImageGenerator.Models;
using HybridImageGenerator.ViewModels;

namespace HybridImageGenerator.Views;

public partial class MainView : UserControl {
    public MainView() {
        InitializeComponent();
        var observable = MainImageControl.GetObservable(BoundsProperty);
        var subscription = observable.Subscribe(new AnonymousObserver<Rect>(rect => {
            if (DataContext is MainViewModel vm) {
                vm.ControlsBounds = rect;
            }
        }));
        
        SaveControl.Loaded += (_, _) => {
            if (DataContext is MainViewModel vm) {
                // TODO: this kinda breaks MVVM model
                vm.ImageEditor.Converter = new ShaderToImageConverter(SaveControl);
            }
        };
        
        Unloaded += (_, _) => subscription.Dispose();
    }
}