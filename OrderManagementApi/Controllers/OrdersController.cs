using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderManagementApi.Dtos;
using OrderManagementApi.Models;
using OrderManagementApi.Services;

namespace OrderManagementApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpPost]
    public async Task<ActionResult<Order>> CreateOrder(CreateOrderDto dto)
    {
        var orderId = await _orderService.CreateOrderAsync();

        return CreatedAtAction(nameof(GetOrderById), new { id = orderId }, new
        {
            id = orderId,
            message = "Order created successfully."
        });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<OrderResponseDto>> GetOrderById(int id)
    {
        var order = await _orderService.GetOrderByIdAsync(id);

        if (order is null)
        {
            return NotFound(new { message = $"Order with id {id} was not found." });
        }

        return Ok(order);
    }

    [HttpPost("{id}/items")]
    public async Task<ActionResult> AddItemToOrder(int id, AddOrderItemDto dto)
    {
        var result = await _orderService.AddItemToOrderAsync(id, dto);

        if (!result.Success)
        {
            if (result.Message.Contains("not found"))
            {
                return NotFound(new { message = result.Message });
            }

            return BadRequest(new { message = result.Message });
        }

        return Ok(new { message = result.Message });
    }

    [HttpPost("{id}/checkout")]
    public async Task<ActionResult<CheckoutResponseDto>> CheckoutOrder(int id)
    {
        var result = await _orderService.CheckoutOrderAsync(id);

        if (!result.Success)
        {
            if (result.Message.Contains("not found"))
            {
                return NotFound(new { message = result.Message });
            }

            return BadRequest(new { message = result.Message });
        }

        return Ok(result.Response);
    }

    [HttpPut("{id}/status")]
    public async Task<ActionResult> UpdateOrderStatus(int id, UpdateOrderStatusDto dto)
    {
        var result = await _orderService.UpdateOrderStatusAsync(id, dto);

        if (!result.Success)
        {
            if (result.Message.Contains("not found"))
            {
                return NotFound(new { message = result.Message });
            }

            return BadRequest(new { message = result.Message });
        }

        return Ok(new
        {
            message = result.Message,
            orderId = id,
            status = result.UpdatedStatus
        });
    }
}