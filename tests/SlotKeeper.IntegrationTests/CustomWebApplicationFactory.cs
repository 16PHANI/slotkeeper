using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SlotKeeper.Api.Auth;
using SlotKeeper.Domain.Entities;
using SlotKeeper.Domain.Enums;
using SlotKeeper.Infrastructure;

namespace SlotKeeper.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public CustomWebApplicationFactory()
    {
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<SlotKeeperDbContext>));

            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<SlotKeeperDbContext>(options =>
            {
                options.UseSqlite(_connection);
            });
        });
    }

    public async Task<string> CreateUserAndGetTokenAsync(string email, string password, string displayName, UserRole role)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SlotKeeperDbContext>();
        var tokenGenerator = scope.ServiceProvider.GetRequiredService<JwtTokenGenerator>();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email.ToLowerInvariant(),
            DisplayName = displayName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = role,
            CreatedUtc = DateTime.UtcNow
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return tokenGenerator.GenerateToken(user);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _connection.Dispose();
        }
    }
}
