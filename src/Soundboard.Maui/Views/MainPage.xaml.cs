using Soundboard.Maui.ViewModels;

namespace Soundboard.Maui.Views;

public partial class MainPage : ContentPage
{
    public MainPage(SoundboardViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;

        Loaded += async (_, _) => await vm.LoadAsync();
    }
}
