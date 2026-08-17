using SlotKeeper.Domain.Entities;

namespace SlotKeeper.Api.Services;

public interface IAuthService
{
    Task<User> RegisterAsync(string email, string password, string displayName, CancellationToken ct);
    Task<User> ValidateCredentialsAsync(string email, string password, CancellationToken ct);
}
