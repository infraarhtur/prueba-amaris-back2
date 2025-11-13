using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechnicalTest.Application.DTOs;
using TechnicalTest.Application.Interfaces;

namespace TechnicalTest.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SnsController : ControllerBase
{
    private readonly ISnsSubscriptionService _snsSubscriptionService;
    private readonly ILogger<SnsController> _logger;

    public SnsController(
        ISnsSubscriptionService snsSubscriptionService,
        ILogger<SnsController> logger)
    {
        _snsSubscriptionService = snsSubscriptionService ?? throw new ArgumentNullException(nameof(snsSubscriptionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Suscribe un número de celular al topic SNS para recibir notificaciones SMS
    /// </summary>
    /// <param name="request">Solicitud con el número de teléfono a suscribir</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>ARN de la suscripción creada</returns>
    [HttpPost("subscribe")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<string>> SubscribePhoneNumberAsync(
        [FromBody] SnsSubscribeRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var subscriptionArn = await _snsSubscriptionService.SubscribePhoneNumberAsync(
                request.PhoneNumber,
                cancellationToken);

            _logger.LogInformation(
                "Número de teléfono {PhoneNumber} suscrito exitosamente. SubscriptionArn: {SubscriptionArn}",
                request.PhoneNumber,
                subscriptionArn);

            return Ok(new { SubscriptionArn = subscriptionArn, Message = "Número suscrito exitosamente" });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Error de validación al suscribir número de teléfono: {PhoneNumber}", request.PhoneNumber);
            return BadRequest(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al suscribir número de teléfono: {PhoneNumber}", request.PhoneNumber);
            return StatusCode(500, new { Message = "Error interno al procesar la suscripción" });
        }
    }

    /// <summary>
    /// Lista todas las suscripciones activas del topic SNS
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Lista de suscripciones</returns>
    [HttpGet("subscriptions")]
    [ProducesResponseType(typeof(IEnumerable<SnsSubscriptionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<SnsSubscriptionDto>>> ListSubscriptionsAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            var subscriptions = await _snsSubscriptionService.ListSubscriptionsAsync(cancellationToken);
            return Ok(subscriptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al listar suscripciones SNS");
            return StatusCode(500, new { Message = "Error interno al listar suscripciones" });
        }
    }

    /// <summary>
    /// Cancela una suscripción SNS
    /// </summary>
    /// <param name="subscriptionArn">ARN de la suscripción a cancelar</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Resultado de la operación</returns>
    [HttpDelete("subscriptions/{subscriptionArn}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> UnsubscribeAsync(
        [FromRoute] string subscriptionArn,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(subscriptionArn))
        {
            return BadRequest(new { Message = "El ARN de suscripción es obligatorio" });
        }

        try
        {
            await _snsSubscriptionService.UnsubscribeAsync(subscriptionArn, cancellationToken);
            _logger.LogInformation("Suscripción SNS cancelada exitosamente: {SubscriptionArn}", subscriptionArn);
            return Ok(new { Message = "Suscripción cancelada exitosamente" });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Error de validación al cancelar suscripción: {SubscriptionArn}", subscriptionArn);
            return BadRequest(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cancelar suscripción SNS: {SubscriptionArn}", subscriptionArn);
            return StatusCode(500, new { Message = "Error interno al cancelar la suscripción" });
        }
    }
}

