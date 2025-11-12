using System;
using System.ComponentModel.DataAnnotations;

namespace TechnicalTest.Application.DTOs;

public record SubscriptionRequestDto(
    [Required] int ProductId,
    [Required] Guid ClientId);

