using TechnicalTest.Domain.Exceptions;

namespace TechnicalTest.Domain.Entities;

public class Availability
{
    private Availability()
    {
    }

    public Availability(int bankBranchId, int productId)
    {
        UpdateBankBranch(bankBranchId);
        UpdateProduct(productId);
    }

    public int Id { get; private set; }
    public int BankBranchId { get; private set; }
    public int ProductId { get; private set; }

    public void Update(int bankBranchId, int productId)
    {
        UpdateBankBranch(bankBranchId);
        UpdateProduct(productId);
    }

    private void UpdateBankBranch(int bankBranchId)
    {
        if (bankBranchId <= 0)
        {
            throw new DomainException("El identificador de la sucursal bancaria es inválido.");
        }

        BankBranchId = bankBranchId;
    }

    private void UpdateProduct(int productId)
    {
        if (productId <= 0)
        {
            throw new DomainException("El identificador del producto es inválido.");
        }

        ProductId = productId;
    }
}



