namespace Shoes.Models;

public class OrderDetail
{
    public int Id { get; set; }
    public int OrderNumber { get; set; }
    public string Article { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; }
    public decimal TotalPrice { get; set; }
    
    // Navigation properties
    public string ProductName { get; set; } = string.Empty;
}

