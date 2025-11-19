using ABCRetailers.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

public class OrdersFunctions
{
    private readonly IOrdersService _ordersService;
    private readonly ILogger<OrdersFunctions> _logger;

    public OrdersFunctions(ILogger<OrdersFunctions> logger, IOrdersService ordersService)
    {
        _logger = logger;
        _ordersService = ordersService;  // Much cleaner!
    }

    [Function("GetOrderStatus")]
    public async Task<IActionResult> GetOrderStatus(string id)
    {
        var order = await _ordersService.GetOrderAsync(id);
        return order != null ? new OkObjectResult(order) : new NotFoundResult();
    }
}
