using System;
using System.Collections.Generic;

namespace Shoes.Models;

public class Order
{
    public int OrderNumber { get; set; }
    public DateTime OrderDate { get; set; }
    public DateTime DeliveryDate { get; set; }
    public int PickupPointId { get; set; }
    public int ClientId { get; set; }
    public string ReceiptCode { get; set; } = string.Empty;
    public string OrderStatus { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    
    // Navigation properties
    public string PickupPointAddress { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    
    // Order details
    public List<OrderDetail> OrderDetails { get; set; } = new();
}

