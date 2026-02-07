using Avalonia;
using Avalonia.Controls;
using Avalonia.Reactive;
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
        
        Unloaded += (_, _) => subscription.Dispose();
    }
}