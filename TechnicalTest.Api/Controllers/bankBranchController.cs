using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechnicalTest.Application.DTOs;
using TechnicalTest.Application.Interfaces;

namespace TechnicalTest.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class bankBranchController(IBankBranchService bankBranchService) : ControllerBase
{
    private const string GetBankBranchByIdRouteName = "GetBankBranchById";
    private readonly IBankBranchService _bankBranchService = bankBranchService ?? throw new ArgumentNullException(nameof(bankBranchService));

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<BankBranchDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<BankBranchDto>>> GetBankBranchesAsync(CancellationToken cancellationToken)
    {
        var branches = await _bankBranchService.GetAsync(cancellationToken).ConfigureAwait(false);
        return Ok(branches);
    }

    [HttpGet("{id:int}", Name = GetBankBranchByIdRouteName)]
    [ProducesResponseType(typeof(BankBranchDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BankBranchDto>> GetBankBranchByIdAsync(int id, CancellationToken cancellationToken)
    {
        var branch = await _bankBranchService.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return Ok(branch);
    }

    [HttpPost]
    [ProducesResponseType(typeof(BankBranchDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BankBranchDto>> CreateBankBranchAsync([FromBody] BankBranchCreateRequestDto request, CancellationToken cancellationToken)
    {
        var created = await _bankBranchService.CreateAsync(request, cancellationToken).ConfigureAwait(false);
        return CreatedAtRoute(GetBankBranchByIdRouteName, new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(BankBranchDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BankBranchDto>> UpdateBankBranchAsync(int id, [FromBody] BankBranchUpdateRequestDto request, CancellationToken cancellationToken)
    {
        var updated = await _bankBranchService.UpdateAsync(id, request, cancellationToken).ConfigureAwait(false);
        return Ok(updated);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteBankBranchAsync(int id, CancellationToken cancellationToken)
    {
        await _bankBranchService.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
        return NoContent();
    }
}


