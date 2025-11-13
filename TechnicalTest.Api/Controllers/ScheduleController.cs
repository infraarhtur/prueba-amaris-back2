using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechnicalTest.Application.DTOs;
using TechnicalTest.Application.Interfaces;

namespace TechnicalTest.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ScheduleController(IScheduleService scheduleService) : ControllerBase
{
    private const string GetScheduleByIdRouteName = "GetScheduleById";

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<ScheduleDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<ScheduleDto>>> GetSchedulesAsync(CancellationToken cancellationToken)
    {
        var schedules = await scheduleService.GetAsync(cancellationToken).ConfigureAwait(false);
        return Ok(schedules);
    }

    [HttpGet("{id:int}", Name = GetScheduleByIdRouteName)]
    [ProducesResponseType(typeof(ScheduleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ScheduleDto>> GetScheduleByIdAsync(int id, CancellationToken cancellationToken)
    {
        var schedule = await scheduleService.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return Ok(schedule);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ScheduleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ScheduleDto>> CreateScheduleAsync([FromBody] ScheduleCreateRequestDto request, CancellationToken cancellationToken)
    {
        var created = await scheduleService.CreateAsync(request, cancellationToken).ConfigureAwait(false);
        return CreatedAtRoute(GetScheduleByIdRouteName, new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ScheduleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ScheduleDto>> UpdateScheduleAsync(int id, [FromBody] ScheduleUpdateRequestDto request, CancellationToken cancellationToken)
    {
        var updated = await scheduleService.UpdateAsync(id, request, cancellationToken).ConfigureAwait(false);
        return Ok(updated);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteScheduleAsync(int id, CancellationToken cancellationToken)
    {
        await scheduleService.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
        return NoContent();
    }
}


