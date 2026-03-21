using CommunityToolkit.Mvvm.Input;
using DialogHostAvalonia;

namespace HybridImageGenerator.ViewModels;

public partial class SafeZoneToolTipViewModel : ViewModelBase {
    [RelayCommand]
    private void Close() {
        DialogHost.GetDialogSession("MainDialogHost")?.Close();
    }
}