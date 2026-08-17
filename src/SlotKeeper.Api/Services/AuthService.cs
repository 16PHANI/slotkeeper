using Microsoft.EntityFrameworkCore;
using SlotKeeper.Domain.Entities;
using SlotKeeper.Domain.Exceptions;
using SlotKeeper.Infrastructure;

namespace SlotKeeper.Api.Services;

public class AuthService : IAuthService
{
    private readonly SlotKeeperDbContext _db;

    public AuthService(SlotKeeperDbContext db)
    {
        _db = db;
    }

    public async Task<User> RegisterAsync(string email, string password, string displayName, CancellationToken ct)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        var exists = await _db.Users.AnyAsync(u => u.Email == normalizedEmail, ct);
        if (exists)
        {
            throw new ConflictException("An account with this email already exists.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = normalizedEmail,
            DisplayName = displayName.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            CreatedUtc = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        return user;
    }

    public async Task<User> ValidateCredentialsAsync(string email, string password, CancellationToken ct)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await _db.Users.SingleOrDefaultAsync(u => u.Email == normalizedEmail, ct);

        if (user is null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            throw new InvalidCredentialsException("Email or password is incorrect.");
        }

        return user;
    }
}
