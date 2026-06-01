namespace Shoes.Models;

public class PickupPoint
{
    public int Id { get; set; }
    public string PointCode { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string HouseNumber { get; set; } = string.Empty;
    
    public string FullAddress => $"{City}, {Street}, {HouseNumber}";
}

