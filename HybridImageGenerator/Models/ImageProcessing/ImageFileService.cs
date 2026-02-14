using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;

namespace HybridImageGenerator.Models.ImageProcessing;

public class ImageFileService(Func<IStorageProvider> storageProviderGetter) {
    private readonly FilePickerFileType[] _openFilter = 
    [
        new("Images") { Patterns = ["*.png", "*.jpg", "*.jpeg", "*.webp", "*.bmp"] }
    ];
    
    private readonly FilePickerFileType[] _saveFilter = 
    [
        new("GIFs") { Patterns = ["*.gif"] }
    ];
    
    public async Task<Stream?> SelectOpenFile() {
        IStorageProvider storageProvider = storageProviderGetter();
        IReadOnlyList<IStorageFile> files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions {
            FileTypeFilter = _openFilter,
            AllowMultiple = false
        });

        if (files.Count < 1) return null;

        IStorageFile file = files[0];
        
        return await file.OpenReadAsync();
    }
    
    public async Task<Stream?> SelectSaveFile() {
        IStorageProvider storageProvider = storageProviderGetter();
        IStorageFile? file = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions {
            SuggestedFileName = "output",
            DefaultExtension = "gif",
            FileTypeChoices = _saveFilter
        });
        
        if (file is null) return null;
        
        return await file.OpenWriteAsync();
    }
}