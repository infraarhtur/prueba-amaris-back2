using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechnicalTest.Application.DTOs;
using TechnicalTest.Application.Interfaces;

namespace TechnicalTest.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AvailabilityController(IAvailabilityService availabilityService) : ControllerBase
{
    private const string GetAvailabilityByIdRouteName = "GetAvailabilityById";

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<AvailabilityDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<AvailabilityDto>>> GetAvailabilityAsync(CancellationToken cancellationToken)
    {
        var availability = await availabilityService.GetAsync(cancellationToken).ConfigureAwait(false);
        return Ok(availability);
    }

    [HttpGet("{id:int}", Name = GetAvailabilityByIdRouteName)]
    [ProducesResponseType(typeof(AvailabilityDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AvailabilityDto>> GetAvailabilityByIdAsync(int id, CancellationToken cancellationToken)
    {
        var availability = await availabilityService.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return Ok(availability);
    }

    [HttpPost]
    [ProducesResponseType(typeof(AvailabilityDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AvailabilityDto>> CreateAvailabilityAsync([FromBody] AvailabilityCreateRequestDto request, CancellationToken cancellationToken)
    {
        var created = await availabilityService.CreateAsync(request, cancellationToken).ConfigureAwait(false);
        return CreatedAtRoute(GetAvailabilityByIdRouteName, new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(AvailabilityDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AvailabilityDto>> UpdateAvailabilityAsync(int id, [FromBody] AvailabilityUpdateRequestDto request, CancellationToken cancellationToken)
    {
        var updated = await availabilityService.UpdateAsync(id, request, cancellationToken).ConfigureAwait(false);
        return Ok(updated);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAvailabilityAsync(int id, CancellationToken cancellationToken)
    {
        await availabilityService.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
        return NoContent();
    }
}



