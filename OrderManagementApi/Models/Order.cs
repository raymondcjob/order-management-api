namespace OrderManagementApi.Models;

public class Order
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public List<OrderItem> Items { get; set; } = new();
}