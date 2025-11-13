using TechnicalTest.Domain.Enums;
using TechnicalTest.Domain.Exceptions;

namespace TechnicalTest.Domain.Entities;

public class Product
{
    private Product()
    {
        Name = string.Empty;
    }

    public Product(int id, string name, decimal minimumAmount, ProductCategory category)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("El nombre del producto es obligatorio.");
        }

        if (minimumAmount <= 0)
        {
            throw new DomainException("El monto mínimo del producto debe ser mayor a cero.");
        }

        Id = id;
        Name = name;
        MinimumAmount = minimumAmount;
        Category = category;
    }

    public int Id { get; private set; }
    public string Name { get; private set; }
    public decimal MinimumAmount { get; private set; }
    public ProductCategory Category { get; private set; }
}


