using System.Collections.Generic;
using TechnicalTest.Domain.Entities;
using TechnicalTest.Domain.Enums;

namespace TechnicalTest.Domain.Data;

public static class ProductCatalog
{
    public static IReadOnlyCollection<Product> GetDefaultProducts() =>
    [
        new Product(1, "FPV_BTG_PACTUAL_RECAUDADORA", 75_000m, ProductCategory.FPV),
        new Product(2, "FPV_BTG_PACTUAL_ECOPTROL", 125_000m, ProductCategory.FPV),
        new Product(3, "DEUDAPRIVADA", 50_000m, ProductCategory.FIC),
        new Product(4, "FDO-ACCIONES", 250_000m, ProductCategory.FIC),
        new Product(5, "FPV_BTG_PACTUAL_DINAMICA", 100_000m, ProductCategory.FPV)
    ];
}


