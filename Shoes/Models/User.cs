namespace Shoes.Models;

public class User
{
    public int Id { get; set; }
    public int RoleId { get; set; }
    public string LastName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string Login { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    
    // Navigation property
    public string? RoleName { get; set; }
    
    public string FullName => $"{LastName} {FirstName} {MiddleName}".Trim();
}

