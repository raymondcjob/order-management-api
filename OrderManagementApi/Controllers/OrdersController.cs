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
}