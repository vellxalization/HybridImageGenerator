using CommunityToolkit.Mvvm.ComponentModel;

namespace HybridImageGenerator.ViewModels;

public partial class MainViewModel() : ViewModelBase {
    [ObservableProperty]
    private EditorViewModel _editorViewModel;
    [ObservableProperty]
    private ErrorViewModel _errorViewModel;
    
    public MainViewModel(EditorViewModel editorViewModel, ErrorViewModel errorViewModel) : this() {
        _editorViewModel = editorViewModel;
        _errorViewModel = errorViewModel; 
    }
}