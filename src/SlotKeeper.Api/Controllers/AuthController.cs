using Microsoft.AspNetCore.Mvc;
using SlotKeeper.Api.Auth;
using SlotKeeper.Api.Dtos;
using SlotKeeper.Api.Services;

namespace SlotKeeper.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly JwtTokenGenerator _tokenGenerator;
    private readonly JwtOptions _jwtOptions;

    public AuthController(IAuthService auth, JwtTokenGenerator tokenGenerator, JwtOptions jwtOptions)
    {
        _auth = auth;
        _tokenGenerator = tokenGenerator;
        _jwtOptions = jwtOptions;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken ct)
    {
        var user = await _auth.RegisterAsync(request.Email, request.Password, request.DisplayName, ct);
        var token = _tokenGenerator.GenerateToken(user);

        return Ok(new AuthResponse(token, user.DisplayName, user.Role.ToString(), DateTime.UtcNow.AddMinutes(_jwtOptions.ExpiryMinutes)));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        var user = await _auth.ValidateCredentialsAsync(request.Email, request.Password, ct);
        var token = _tokenGenerator.GenerateToken(user);

        return Ok(new AuthResponse(token, user.DisplayName, user.Role.ToString(), DateTime.UtcNow.AddMinutes(_jwtOptions.ExpiryMinutes)));
    }
}
