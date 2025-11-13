using TechnicalTest.Domain.Exceptions;

namespace TechnicalTest.Domain.Entities;

public class BankBranch
{
    private BankBranch()
    {
        Name = string.Empty;
        City = string.Empty;
    }

    public BankBranch(string name, string city)
    {
        UpdateName(name);
        UpdateCity(city);
    }

    public int Id { get; private set; }
    public string Name { get; private set; }
    public string City { get; private set; }

    public void Update(string name, string city)
    {
        UpdateName(name);
        UpdateCity(city);
    }

    private void UpdateName(string value) =>
        Name = NormalizeRequiredText(value, nameof(Name));

    private void UpdateCity(string value) =>
        City = NormalizeRequiredText(value, nameof(City));

    private static string NormalizeRequiredText(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException($"El campo {fieldName} es requerido.");
        }

        return value.Trim();
    }
}


