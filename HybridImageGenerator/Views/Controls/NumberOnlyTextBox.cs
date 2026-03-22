using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;

namespace HybridImageGenerator.Views.Controls;

public class NumberOnlyTextBox : TextBox {
    protected override Type StyleKeyOverride => typeof(TextBox);
    
    public ushort Value {
        get;
        private set;
    }

    public NumberOnlyTextBox() {
        PastingFromClipboard += OnPaste;
        Text = "0";
    }

    private async void OnPaste(object? sender, RoutedEventArgs args) {
        args.Handled = true; // suppress paste event until we verify it
        
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard == null) return;
        
        string? text = null;
        try {
            text = await clipboard.TryGetTextAsync();
        }
        catch (TimeoutException) { } // suppress
        
        if (text is null) return;
        if (!text.All(char.IsDigit)) return;
        if (!ushort.TryParse(Text!.Insert(CaretIndex, text), out var result)) return;
        Value = result;
        
        if (Text is "0") {
            string trimmed = text.TrimStart('0');
            Text = text.TrimStart('0');
            CaretIndex += trimmed.Length;
            return;
        }
            
        args.Handled = false;
    }
    
    protected override void OnTextInput(TextInputEventArgs e) {
        if (string.IsNullOrWhiteSpace(e.Text)) return;
        if (e.Text.Length > 1 || !char.IsDigit(e.Text[0])) return;
        
        string currentText = Text ?? "";
        if (!ushort.TryParse(currentText.Insert(CaretIndex, e.Text), out ushort result)) return;
        
        base.OnTextInput(e);
        if (Text!.StartsWith('0'))
            Text = Text[1..];
        
        Value = result;
    }

    protected override void OnKeyDown(KeyEventArgs e) {
        base.OnKeyDown(e);
        
        if (e.Key is not (Key.Back or Key.Delete)) return;
        if (Text != string.Empty) return;
        
        Text = "0"; 
        CaretIndex = 1;
    }
}