using TechnicalTest.Domain.Enums;
using TechnicalTest.Domain.Exceptions;

namespace TechnicalTest.Domain.Entities;

public class Fund
{
    public Fund(int id, string name, decimal minimumAmount, FundCategory category)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("El nombre del fondo es obligatorio.");
        }

        if (minimumAmount <= 0)
        {
            throw new DomainException("El monto mínimo del fondo debe ser mayor a cero.");
        }

        Id = id;
        Name = name;
        MinimumAmount = minimumAmount;
        Category = category;
    }

    public int Id { get; }
    public string Name { get; }
    public decimal MinimumAmount { get; }
    public FundCategory Category { get; }
}

