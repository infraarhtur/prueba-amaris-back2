using BTGPactual.Fondos.Domain.Entities;
using BTGPactual.Fondos.Domain.Enums;

namespace BTGPactual.Fondos.Domain.Data;

public static class FundCatalog
{
    public static IReadOnlyCollection<Fund> GetDefaultFunds() =>
    [
        new Fund(1, "FPV_BTG_PACTUAL_RECAUDADORA", 75_000m, FundCategory.FPV),
        new Fund(2, "FPV_BTG_PACTUAL_ECOPTROL", 125_000m, FundCategory.FPV),
        new Fund(3, "DEUDAPRIVADA", 50_000m, FundCategory.FIC),
        new Fund(4, "FDO-ACCIONES", 250_000m, FundCategory.FIC),
        new Fund(5, "FPV_BTG_PACTUAL_DINAMICA", 100_000m, FundCategory.FPV)
    ];
}

