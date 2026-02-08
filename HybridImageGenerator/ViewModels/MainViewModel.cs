using System.IO;
using System.Threading.Tasks;
using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HybridImageGenerator.Models;
using SkiaSharp;

namespace HybridImageGenerator.ViewModels;

public partial class MainViewModel(EditorViewModel editorViewModel) : ViewModelBase {
    [ObservableProperty]
    private EditorViewModel? _editorViewModel = editorViewModel;
}