using Avalonia.Controls;
using Shoes.ViewModels;

namespace Shoes.Views;

public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();
        
        var viewModel = new LoginWindowViewModel();
        DataContext = viewModel;
        
        viewModel.LoginSuccessful += (s, e) =>
        {
            var productsWindow = new ProductsWindow();
            productsWindow.Show();
            Close();
        };
        
        viewModel.GuestLoginRequested += (s, e) =>
        {
            var productsWindow = new ProductsWindow();
            productsWindow.Show();
            Close();
        };
    }
}

