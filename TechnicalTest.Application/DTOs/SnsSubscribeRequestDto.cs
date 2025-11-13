using System.ComponentModel.DataAnnotations;

namespace TechnicalTest.Application.DTOs;

public class SnsSubscribeRequestDto
{
    [Required(ErrorMessage = "El número de teléfono es obligatorio")]
    [RegularExpression(@"^\+[1-9]\d{1,14}$", ErrorMessage = "El número de teléfono debe tener el formato internacional: +[código país][número] (ejemplo: +573208965783)")]
    public string PhoneNumber { get; set; } = string.Empty;
}

