using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Shoes.Models;
using Shoes.Services;
using Shoes.ViewModels;

namespace Shoes.Views;

public partial class ProductsWindow : Window
{
    private ProductsWindowViewModel? _viewModel;
    
    public ProductsWindow()
    {
        InitializeComponent();
        
        _viewModel = new ProductsWindowViewModel();
        DataContext = _viewModel;
        
        _viewModel.UpdatePermissions();
        
    }

    private void ProductCard_Tapped(object? sender, TappedEventArgs e)
    {
        if (sender is Border border && border.DataContext is Product product)
        {
            if (_viewModel != null)
            {
                _viewModel.SelectedProduct = product;
            }
        }
    }
    
    private void BackButton_Click(object? sender, RoutedEventArgs e)
    {
        CurrentUserService.Instance.CurrentUser = null;
        CurrentUserService.Instance.IsGuest = false;
        
        var loginWindow = new LoginWindow();
        loginWindow.Show();
        Close();
    }
}
