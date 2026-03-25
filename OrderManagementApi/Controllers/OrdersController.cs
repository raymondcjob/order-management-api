using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderManagementApi.Data;
using OrderManagementApi.Dtos;
using OrderManagementApi.Models;

namespace OrderManagementApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly AppDbContext _context;

    public OrdersController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<ActionResult<Order>> CreateOrder(CreateOrderDto dto)
    {
        var order = new Order
        {
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetOrderById), new { id = order.Id }, order);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<OrderResponseDto>> GetOrderById(int id)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order is null)
        {
            return NotFound(new { message = $"Order with id {id} was not found." });
        }

        var response = new OrderResponseDto
        {
            Id = order.Id,
            CreatedAt = order.CreatedAt,
            Status = order.Status,
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

        return Ok(response);
    }

    [HttpPost("{id}/items")]
    public async Task<ActionResult> AddItemToOrder(int id, AddOrderItemDto dto)
    {
        if (dto.Quantity <= 0)
        {
            return BadRequest(new { message = "Quantity must be greater than 0." });
        }

        var order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order is null)
        {
            return NotFound(new { message = $"Order with id {id} was not found." });
        }

        if (order.Status != "Pending")
        {
            return BadRequest(new { message = "Items can only be added to a pending order." });
        }

        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == dto.ProductId);

        if (product is null)
        {
            return NotFound(new { message = $"Product with id {dto.ProductId} was not found." });
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

        return Ok(new { message = "Item added to order successfully." });
    }

    [HttpPost("{id}/checkout")]
    public async Task<ActionResult<CheckoutResponseDto>> CheckoutOrder(int id)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order is null)
        {
            return NotFound(new { message = $"Order with id {id} was not found." });
        }

        if (order.Status != "Pending")
        {
            return BadRequest(new { message = "Only pending orders can be checked out." });
        }

        if (!order.Items.Any())
        {
            return BadRequest(new { message = "Cannot checkout an order with no items." });
        }

        foreach (var item in order.Items)
        {
            if (item.Product is null)
            {
                return BadRequest(new { message = $"Product data is missing for order item {item.Id}." });
            }

            if (item.Quantity > item.Product.StockQuantity)
            {
                return BadRequest(new
                {
                    message = $"Not enough stock for product '{item.Product.Name}'. Available stock: {item.Product.StockQuantity}."
                });
            }
        }

        foreach (var item in order.Items)
        {
            item.Product!.StockQuantity -= item.Quantity;
        }

        order.Status = "Paid";

        await _context.SaveChangesAsync();

        var response = new CheckoutResponseDto
        {
            OrderId = order.Id,
            Status = order.Status,
            TotalAmount = order.Items.Sum(i => i.Quantity * i.UnitPrice),
            Message = "Order checked out successfully."
        };

        return Ok(response);
    }

    [HttpPut("{id}/status")]
    public async Task<ActionResult> UpdateOrderStatus(int id, UpdateOrderStatusDto dto)
    {
        var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id);

        if (order is null)
        {
            return NotFound(new { message = $"Order with id {id} was not found." });
        }

        if (string.IsNullOrWhiteSpace(dto.NewStatus))
        {
            return BadRequest(new { message = "NewStatus is required." });
        }

        var normalizedStatus = dto.NewStatus.Trim();

        if (!IsValidStatusTransition(order.Status, normalizedStatus))
        {
            return BadRequest(new
            {
                message = $"Invalid status transition from '{order.Status}' to '{normalizedStatus}'."
            });
        }

        order.Status = normalizedStatus;
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = $"Order status updated to {order.Status}.",
            orderId = order.Id,
            status = order.Status
        });
    }
    private static bool IsValidStatusTransition(string currentStatus,string newStatus)
    {
        return (currentStatus, newStatus) switch
        {
            ("Pending", "Paid") => true,
            ("Paid", "Shipped") => true,
            ("Shipped", "Completed") => true,
            ("Pending", "Cancelled") => true,
            ("Paid", "Cancelled") => true,
            _ => false
        };
    }
}