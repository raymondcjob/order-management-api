using Microsoft.EntityFrameworkCore;
using OrderManagementApi.Data;
using OrderManagementApi.Dtos;
using OrderManagementApi.Models;

namespace OrderManagementApi.Services;

public class OrderService : IOrderService
{
    private readonly AppDbContext _context;

    public OrderService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<int> CreateOrderAsync()
    {
        var order = new Order
        {
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        return order.Id;
    }

    public async Task<OrderResponseDto?> GetOrderByIdAsync(int id)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order is null)
        {
            return null;
        }

        return new OrderResponseDto
        {
            Id = order.Id,
            CreatedAt = order.CreatedAt,
            Status = order.Status.ToString(),
            TotalAmount = order.Items.Sum(i => i.Quantity * i.UnitPrice),
            Items = order.Items.Select(i => new OrderItemResponseDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductName = i.Product?.Name ?? string.Empty,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList()
        };
    }

    public async Task<(bool Success, string Message)> AddItemToOrderAsync(int orderId, AddOrderItemDto dto)
    {
        if (dto.Quantity <= 0)
        {
            return (false, "Quantity must be greater than 0.");
        }

        var order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order is null)
        {
            return (false, $"Order with id {orderId} was not found.");
        }

        if (order.Status != OrderStatus.Pending)
        {
            return (false, "Items can only be added to a pending order.");
        }

        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == dto.ProductId);

        if (product is null)
        {
            return (false, $"Product with id {dto.ProductId} was not found.");
        }

        var existingItem = order.Items.FirstOrDefault(i => i.ProductId == dto.ProductId);

        if (existingItem is not null)
        {
            existingItem.Quantity += dto.Quantity;
        }
        else
        {
            var orderItem = new OrderItem
            {
                OrderId = order.Id,
                ProductId = product.Id,
                Quantity = dto.Quantity,
                UnitPrice = product.Price
            };

            _context.OrderItems.Add(orderItem);
        }

        await _context.SaveChangesAsync();

        return (true, "Item added to order successfully.");
    }

    public async Task<(bool Success, string Message, CheckoutResponseDto? Response)> CheckoutOrderAsync(int orderId)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order is null)
        {
            return (false, $"Order with id {orderId} was not found.", null);
        }

        if (order.Status != OrderStatus.Pending)
        {
            return (false, "Only pending orders can be checked out.", null);
        }

        if (!order.Items.Any())
        {
            return (false, "Cannot checkout an order with no items.", null);
        }

        foreach (var item in order.Items)
        {
            if (item.Product is null)
            {
                return (false, $"Product data is missing for order item {item.Id}.", null);
            }

            if (item.Quantity > item.Product.StockQuantity)
            {
                return (false, $"Not enough stock for product '{item.Product.Name}'. Available stock: {item.Product.StockQuantity}.", null);
            }
        }

        foreach (var item in order.Items)
        {
            item.Product!.StockQuantity -= item.Quantity;
        }

        order.Status = OrderStatus.Paid;
        await _context.SaveChangesAsync();

        var response = new CheckoutResponseDto
        {
            OrderId = order.Id,
            Status = order.Status.ToString(),
            TotalAmount = order.Items.Sum(i => i.Quantity * i.UnitPrice),
            Message = "Order checked out successfully."
        };

        return (true, response.Message, response);
    }

    public async Task<(bool Success, string Message, string? UpdatedStatus)> UpdateOrderStatusAsync(int orderId, UpdateOrderStatusDto dto)
    {
        var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId);

        if (order is null)
        {
            return (false, $"Order with id {orderId} was not found.", null);
        }

        if (string.IsNullOrWhiteSpace(dto.NewStatus))
        {
            return (false, "NewStatus is required.", null);
        }

        var parseSucceeded = Enum.TryParse<OrderStatus>(dto.NewStatus.Trim(), true, out var newStatus);

        if (!parseSucceeded)
        {
            return (false, $"'{dto.NewStatus}' is not a valid order status.", null);
        }

        if (!IsValidStatusTransition(order.Status, newStatus))
        {
            return (false, $"Invalid status transition from '{order.Status}' to '{newStatus}'.", null);
        }

        order.Status = newStatus;
        await _context.SaveChangesAsync();

        return (true, $"Order status updated to {order.Status}.", order.Status.ToString());
    }

    private static bool IsValidStatusTransition(OrderStatus currentStatus, OrderStatus newStatus)
    {
        return (currentStatus, newStatus) switch
        {
            (OrderStatus.Pending, OrderStatus.Paid) => true,
            (OrderStatus.Paid, OrderStatus.Shipped) => true,
            (OrderStatus.Shipped, OrderStatus.Completed) => true,
            (OrderStatus.Pending, OrderStatus.Cancelled) => true,
            (OrderStatus.Paid, OrderStatus.Cancelled) => true,
            _ => false
        };
    }
}