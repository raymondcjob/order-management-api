using OrderManagementApi.Dtos;

namespace OrderManagementApi.Services;

public interface IOrderService
{
    Task<OrderResponseDto?> GetOrderByIdAsync(int id);
    Task<int> CreateOrderAsync();
    Task<(bool Success, string Message)> AddItemToOrderAsync(int orderId, AddOrderItemDto dto);
    Task<(bool Success, string Message, CheckoutResponseDto? Response)> CheckoutOrderAsync(int orderId);
    Task<(bool Success, string Message, string? UpdatedStatus)> UpdateOrderStatusAsync(int orderId, UpdateOrderStatusDto dto);
}