namespace OrderManagementApi.Dtos;

public class OrderResponseDto
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<OrderItemResponseDto> Items { get; set; } = new();
}