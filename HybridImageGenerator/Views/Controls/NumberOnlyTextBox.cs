using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;

namespace HybridImageGenerator.Views.Controls;

public class NumberOnlyTextBox : TextBox {
    protected override Type StyleKeyOverride => typeof(TextBox);
    
    public static readonly StyledProperty<ushort> ValueProperty =
        AvaloniaProperty.Register<NumberOnlyTextBox, ushort>(nameof(Value), defaultBindingMode: BindingMode.TwoWay);

    public ushort Value {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public NumberOnlyTextBox() {
        PastingFromClipboard += OnPaste;
        Text = "0";
    }

    private async void OnPaste(object? sender, RoutedEventArgs e) {
        e.Handled = true;
        
        IClipboard? clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard is null) return;

        string? clipboardText = null;
        try {
            clipboardText = await clipboard.TryGetTextAsync();
        } catch (TimeoutException) {} // suppress

        if (!TryInsertInput(clipboardText, out string result))
            return;
        
        Text = result;
        UpdateCaret(Math.Min(SelectionStart, SelectionEnd) + clipboardText!.Length);
    }

    private void UpdateCaret(int newPos) {
        if (CaretIndex == newPos)
            CaretIndex = newPos == 0 ? 1 : 0; // guarantee that the caret will be updated

        CaretIndex = SelectionEnd = SelectionStart = newPos;
    }
    
    protected override void OnTextInput(TextInputEventArgs e) {
        if (TryInsertInput(e.Text, out _)) 
            base.OnTextInput(e);
        else
            e.Handled = true;
    }

    private bool TryInsertInput(string? input, out string result) {
        result = Text ?? "";
        if (string.IsNullOrEmpty(input)) return false;
        if (!input.All(char.IsDigit)) return false;
        
        bool selectionActive = SelectionStart != SelectionEnd;
        if (selectionActive) {
            (int start, int length) selection = GetSelectionRange();
            result = result.Remove(selection.start, selection.length).Insert(selection.start, input);
        }
        else
            result = result.Insert(CaretIndex, input);
        
        return ushort.TryParse(result, out _);
    }
    
    private (int start, int length) GetSelectionRange() 
        => (Math.Min(SelectionStart, SelectionEnd), Math.Abs(SelectionEnd - SelectionStart));

    protected override void OnLostFocus(RoutedEventArgs e) {
        base.OnLostFocus(e);

        string text = Text ?? string.Empty;
        bool needTrimming = text.StartsWith('0');
        if (text is "0" || (!needTrimming && (text is not ""))) return;

        string trimmed = text.TrimStart('0');
        Text = trimmed is "" ? "0" : trimmed;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change) {
        base.OnPropertyChanged(change);
        
        if (change.Property == TextProperty) {
            ushort result;
            if (string.IsNullOrEmpty(Text))
                result = 0;
            else if (!ushort.TryParse(Text, out result)) 
                return;

            if (Value != result) 
                Value = result;
        }
        else if (change.Property == ValueProperty) {
            ushort newValue = change.GetNewValue<ushort>();
            if (!ushort.TryParse(Text, out ushort currentTextValue) || currentTextValue != newValue)
                Text = newValue.ToString();
        }
    }
}