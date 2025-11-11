using System.ComponentModel.DataAnnotations;

namespace TechnicalTest.Application.DTOs;

public class BankBranchUpdateRequestDto
{
    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(120)]
    public string City { get; set; } = string.Empty;
}


