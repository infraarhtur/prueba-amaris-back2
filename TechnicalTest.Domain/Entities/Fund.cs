using TechnicalTest.Domain.Enums;
using TechnicalTest.Domain.Exceptions;

namespace TechnicalTest.Domain.Entities;

public class Fund
{
    private Fund()
    {
        Name = string.Empty;
    }

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

    public int Id { get; private set; }
    public string Name { get; private set; }
    public decimal MinimumAmount { get; private set; }
    public FundCategory Category { get; private set; }
}

