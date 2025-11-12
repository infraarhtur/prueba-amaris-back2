using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TechnicalTest.Domain.Data;
using TechnicalTest.Domain.Entities;
using TechnicalTest.Infrastructure.Persistence;

namespace TechnicalTest.Tests.Infrastructure;

public class AppDbContextTests
{
    [Fact]
    public async Task OnModelCreating_ShouldSeedInitialData()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new AppDbContext(options);
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();

        var user = await context.Users.SingleAsync();
        user.Email.Should().Be("demo.client@amaris.com");

        var client = await context.Clients.SingleAsync();
        client.UserId.Should().Be(user.Id);

        var products = await context.Products.OrderBy(product => product.Id).ToListAsync();
        products.Should().HaveSameCount(ProductCatalog.GetDefaultProducts());
        products.Select(product => product.Name)
            .Should()
            .BeEquivalentTo(ProductCatalog.GetDefaultProducts().Select(product => product.Name));
    }
}

