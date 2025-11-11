using System.ComponentModel.DataAnnotations;

namespace TechnicalTest.Application.DTOs;

public record ProductCreateRequestDto(
    [property: Required] int Id,
    [property: Required, MaxLength(200)] string Name,
    [property: Range(0.01, double.MaxValue)] decimal MinimumAmount,
    [property: Required] string Category);


