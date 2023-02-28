namespace CryptoSbmScanner;

public partial class MainPage : ContentPage
{
    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    private async void Copy_Clicked(object sender, EventArgs e)
    {
        MenuFlyoutItem item = (MenuFlyoutItem)sender;
        await Clipboard.Default.SetTextAsync(item.CommandParameter.ToString());
    }
}