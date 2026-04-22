using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TradingApp.Domain;
using TradingApp.Domain.Models.Entities;
using TradingApp.Domain.Models.Enums;

namespace OrderExecutionProviderFunc;

public class OrderExecutionProvider
{
    private readonly ILogger<OrderExecutionProvider> _logger;
    private readonly TradingDbContext _tradingDbContext;
    public OrderExecutionProvider(ILogger<OrderExecutionProvider> logger,TradingDbContext tradingDbContext)
    {
        _logger = logger;
        _tradingDbContext = tradingDbContext;
    }

    [Function("OrderExecutionProvider")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
    {
        _logger.LogInformation("OrderExecutionProvider started.");

        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var payload = JsonSerializer.Deserialize<OrderPayload>(requestBody);

        if (payload == null)
        {
            return new BadRequestObjectResult("Invalid payload.");
        }

        var order = await _tradingDbContext.Orders.FirstOrDefaultAsync(x => x.ClientOrderId == payload.ClientOrderId);
        if (order == null)
        {
            return new NotFoundObjectResult("That order was not found.");
        }

        var random = new Random();
        order.Status = random.Next(2) == 0 ? OrderStatus.ACKNOWLEDGED : OrderStatus.REJECTED;
        order.UpdatedAt = DateTimeOffset.UtcNow;

        await _tradingDbContext.SaveChangesAsync();
        return new OkObjectResult(new { payload.ClientOrderId, Status = order.Status.ToString() });
    }
}