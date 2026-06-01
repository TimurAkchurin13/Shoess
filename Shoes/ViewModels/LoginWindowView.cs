using System;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Shoes.Services;

namespace Shoes.ViewModels;

public partial class LoginWindowViewModel : ViewModelBase
{
    private readonly DatabaseService _databaseService;
    
    [ObservableProperty]
    private string _username = string.Empty;
    
    [ObservableProperty]
    private string _password = string.Empty;
    
    [ObservableProperty]
    private string _errorMessage = string.Empty;
    
    [ObservableProperty]
    private bool _isLoading = false;
    
    public event EventHandler? LoginSuccessful;
    public event EventHandler? GuestLoginRequested;
    
    public LoginWindowViewModel()
    {
        _databaseService = new DatabaseService();
    }
    
    [RelayCommand]
    private async Task Login()
    {
        ErrorMessage = string.Empty;
        
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Введите логин и пароль";
            return;
        }
        
        IsLoading = true;
        
        try
        {
            var user = await _databaseService.AuthenticateUser(Username, Password);
            
            if (user != null)
            {
                CurrentUserService.Instance.CurrentUser = user;
                CurrentUserService.Instance.IsGuest = false;
                
                // Обновляем права доступа
                System.Diagnostics.Debug.WriteLine($"=== УСПЕШНЫЙ ВХОД ===");
                System.Diagnostics.Debug.WriteLine($"Пользователь: {user.Login}");
                System.Diagnostics.Debug.WriteLine($"Роль из БД: '{user.RoleName}'");
                System.Diagnostics.Debug.WriteLine($"IsAdmin: {CurrentUserService.Instance.IsAdmin}");
                System.Diagnostics.Debug.WriteLine($"IsManager: {CurrentUserService.Instance.IsManager}");
                System.Diagnostics.Debug.WriteLine($"IsClient: {CurrentUserService.Instance.IsClient}");
                System.Diagnostics.Debug.WriteLine($"CanCreateOrder: {CurrentUserService.Instance.CanCreateOrder}");
                System.Diagnostics.Debug.WriteLine($"CanViewMyOrders: {CurrentUserService.Instance.CanViewMyOrders}");
                
                LoginSuccessful?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                ErrorMessage = "Неверный логин или пароль";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка подключения к базе данных: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    [RelayCommand]
    private void LoginAsGuest()
    {
        CurrentUserService.Instance.CurrentUser = null;
        CurrentUserService.Instance.IsGuest = true;
        GuestLoginRequested?.Invoke(this, EventArgs.Empty);
    }
}

