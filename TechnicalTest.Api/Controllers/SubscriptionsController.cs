using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using TechnicalTest.Application.DTOs;
using TechnicalTest.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace TechnicalTest.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SubscriptionsController(IProductManagementService productManagementService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SubscriptionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<SubscriptionDto>>> GetSubscriptionsAsync(CancellationToken cancellationToken)
    {
        var subscriptions = await productManagementService.GetSubscriptionsAsync(cancellationToken);
        return Ok(subscriptions);
    }

    [HttpPost]
    [ProducesResponseType(typeof(SubscriptionDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<SubscriptionDto>> SubscribeAsync([FromBody] SubscriptionRequestDto request, CancellationToken cancellationToken)
    {
        var subscription = await productManagementService.SubscribeAsync(request, cancellationToken);
        return Created($"api/subscriptions/{subscription.Id}", subscription);
    }

    [HttpPost("{subscriptionId:guid}/cancel")]
    [ProducesResponseType(typeof(SubscriptionDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SubscriptionDto>> CancelAsync(Guid subscriptionId, CancellationToken cancellationToken)
    {
        var subscription = await productManagementService.CancelSubscriptionAsync(subscriptionId, cancellationToken);
        return Ok(subscription);
    }
}

