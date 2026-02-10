using CommunityToolkit.Mvvm.ComponentModel;

namespace HybridImageGenerator.ViewModels;

public partial class MainViewModel(EditorViewModel editorViewModel, ErrorViewModel errorViewModel) : ViewModelBase {
    [ObservableProperty]
    private EditorViewModel _editorViewModel = editorViewModel;
    [ObservableProperty]
    private ErrorViewModel _errorViewModel = errorViewModel;
}