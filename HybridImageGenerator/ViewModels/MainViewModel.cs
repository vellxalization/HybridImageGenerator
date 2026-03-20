using CommunityToolkit.Mvvm.ComponentModel;

namespace HybridImageGenerator.ViewModels;

public partial class MainViewModel(EditorViewModel editorViewModel) : ViewModelBase {
    [ObservableProperty]
    private EditorViewModel _editorViewModel = editorViewModel;
}