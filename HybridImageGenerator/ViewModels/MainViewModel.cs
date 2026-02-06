using System;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using HybridImageGenerator.Models;

namespace HybridImageGenerator.ViewModels;

public partial class MainViewModel(ImageFileService fileService) : ViewModelBase {
    [RelayCommand]
    private async Task LoadMainImage() {
        await using Stream? file = await fileService.SelectOpenFile();
        if (file is null) return;
        
        Console.WriteLine(file.Length);
    }
    
    [RelayCommand]
    private async Task LoadHiddenImage() {
        await using Stream? file = await fileService.SelectOpenFile();
        if (file is null) return;
        
        Console.WriteLine(file.Length);
    }
    
    [RelayCommand]
    private async Task SaveImage() {
        await using Stream? file = await fileService.SelectSaveFile();
        if (file is null) return;
        
        Console.WriteLine(file.Length);
    }
}