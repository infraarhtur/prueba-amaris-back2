using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TechnicalTest.Application.Interfaces.Repositories;
using TechnicalTest.Application.Interfaces.Security;
using TechnicalTest.Infrastructure;
using TechnicalTest.Infrastructure.Authentication;
using TechnicalTest.Infrastructure.Persistence;

namespace TechnicalTest.Tests.Infrastructure;

public class DependencyInjectionTests
{
    [Fact]
    public void AddInfrastructure_ShouldConfigureServicesAndJwtSettings()
    {
        var services = new ServiceCollection();
        var settings = new Dictionary<string, string?>
        {
            ["ConnectionStrings:Default"] = "Host=localhost;Database=test;Username=user;Password=pass",
            ["Jwt:Issuer"] = "issuer",
            ["Jwt:Audience"] = "audience",
            ["Jwt:Key"] = "ThisIsASufficientlyLongJwtSigningKey!",
            ["Jwt:ExpirationMinutes"] = "30"
        };
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        services.AddInfrastructure(configuration);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var scopedProvider = scope.ServiceProvider;

        scopedProvider.GetRequiredService<AppDbContext>().Should().NotBeNull();
        scopedProvider.GetRequiredService<IAvailabilityRepository>().Should().NotBeNull();
        scopedProvider.GetRequiredService<IClientRepository>().Should().NotBeNull();
        scopedProvider.GetRequiredService<IBankBranchRepository>().Should().NotBeNull();
        scopedProvider.GetRequiredService<IProductRepository>().Should().NotBeNull();
        scopedProvider.GetRequiredService<ISubscriptionRepository>().Should().NotBeNull();
        scopedProvider.GetRequiredService<IScheduleRepository>().Should().NotBeNull();
        scopedProvider.GetRequiredService<IUserRepository>().Should().NotBeNull();
        scopedProvider.GetRequiredService<IPasswordHasher>().Should().BeOfType<PasswordHasher>();
        scopedProvider.GetRequiredService<IJwtProvider>().Should().BeOfType<JwtProvider>();

        var options = scopedProvider.GetRequiredService<IOptions<JwtSettings>>();
        options.Value.Issuer.Should().Be("issuer");
        options.Value.Audience.Should().Be("audience");
        options.Value.Key.Should().Be("ThisIsASufficientlyLongJwtSigningKey!");
        options.Value.ExpirationMinutes.Should().Be(30);
    }

    [Fact]
    public void AddInfrastructure_ShouldThrow_WhenConnectionStringMissing()
    {
        var services = new ServiceCollection();
        IConfiguration configuration = new ConfigurationBuilder().Build();

        Action act = () => services.AddInfrastructure(configuration);

        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("Connection string 'DefaultConnection' is not configured.");
    }
}

