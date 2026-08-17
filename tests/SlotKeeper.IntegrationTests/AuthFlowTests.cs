using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SlotKeeper.Api.Dtos;
using Xunit;

namespace SlotKeeper.IntegrationTests;

public class AuthFlowTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthFlowTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_ThenLogin_ReturnsAWorkingToken()
    {
        var email = $"user-{Guid.NewGuid():N}@example.com";

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest(email, "CorrectHorseBatteryStaple1!", "Test User"));

        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(email, "CorrectHorseBatteryStaple1!"));

        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        body.Should().NotBeNull();
        body!.Token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var email = $"user-{Guid.NewGuid():N}@example.com";

        await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest(email, "CorrectHorseBatteryStaple1!", "Test User"));

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(email, "TotallyWrongPassword"));

        loginResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsConflict()
    {
        var email = $"user-{Guid.NewGuid():N}@example.com";
        var payload = new RegisterRequest(email, "CorrectHorseBatteryStaple1!", "Test User");

        await _client.PostAsJsonAsync("/api/auth/register", payload);
        var secondResponse = await _client.PostAsJsonAsync("/api/auth/register", payload);

        secondResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GetBookings_WithoutAToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/bookings/mine");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
