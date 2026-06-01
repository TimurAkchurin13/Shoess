using Shoes.Models;

namespace Shoes.Services;

public class CurrentUserService
{
    private static CurrentUserService? _instance;
    public static CurrentUserService Instance => _instance ??= new CurrentUserService();
    
    public User? CurrentUser { get; set; }
    public bool IsGuest { get; set; }
    
    private static string NormalizeRole(string? role) =>
        string.IsNullOrWhiteSpace(role) ? string.Empty : role.Trim().ToLowerInvariant();
    
    public bool IsAdmin => NormalizeRole(CurrentUser?.RoleName) == "администратор";
    public bool IsManager => NormalizeRole(CurrentUser?.RoleName) == "менеджер";
    public bool IsClient => !IsGuest 
                            && CurrentUser != null
                            && NormalizeRole(CurrentUser.RoleName) == "клиент";
    
    public bool CanFilterAndSearch => IsAdmin || IsManager;
    public bool CanEditProducts => IsAdmin;
    // Менеджер может только просматривать заказы, без изменений
    public bool CanEditOrders => IsAdmin;
    public bool CanViewOrders => IsAdmin || IsManager;
    public bool CanViewMyOrders => IsClient; // Клиент может видеть свои заказы
    public bool CanViewReports => false; // Пользователь не должен иметь доступ к окну по отчетам
    
    // Оформлять заказ могут клиент и администратор (но не гость и не менеджер)
    public bool CanCreateOrder => !IsGuest && CurrentUser != null && (IsClient || IsAdmin);
}

