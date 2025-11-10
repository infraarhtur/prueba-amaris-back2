using BTGPactual.Fondos.Application.DTOs;
using BTGPactual.Fondos.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BTGPactual.Fondos.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SubscriptionsController(IFundManagementService fundManagementService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SubscriptionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<SubscriptionDto>>> GetSubscriptionsAsync(CancellationToken cancellationToken)
    {
        var subscriptions = await fundManagementService.GetSubscriptionsAsync(cancellationToken);
        return Ok(subscriptions);
    }

    [HttpPost]
    [ProducesResponseType(typeof(SubscriptionDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<SubscriptionDto>> SubscribeAsync([FromBody] SubscriptionRequestDto request, CancellationToken cancellationToken)
    {
        var subscription = await fundManagementService.SubscribeAsync(request, cancellationToken);
        return Created($"api/subscriptions/{subscription.SubscriptionId}", subscription);
    }

    [HttpPost("{subscriptionId:guid}/cancel")]
    [ProducesResponseType(typeof(SubscriptionDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SubscriptionDto>> CancelAsync(Guid subscriptionId, CancellationToken cancellationToken)
    {
        var subscription = await fundManagementService.CancelSubscriptionAsync(subscriptionId, cancellationToken);
        return Ok(subscription);
    }
}

