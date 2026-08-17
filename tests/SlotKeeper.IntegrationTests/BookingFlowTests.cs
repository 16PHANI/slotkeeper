using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using SlotKeeper.Api.Dtos;
using SlotKeeper.Domain.Enums;
using Xunit;

namespace SlotKeeper.IntegrationTests;

public class BookingFlowTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public BookingFlowTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> CreateClientAsync(UserRole role, string label)
    {
        var client = _factory.CreateClient();
        var token = await _factory.CreateUserAndGetTokenAsync(
            $"{label}-{Guid.NewGuid():N}@example.com", "CorrectHorseBatteryStaple1!", label, role);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private async Task<Guid> CreateResourceAsync(HttpClient adminClient, int slotMinutes = 30, int maxPerDay = 3)
    {
        var response = await adminClient.PostAsJsonAsync("/api/resources", new CreateResourceRequest(
            "Meeting Room A", "Glass-walled room on the third floor", "Building 1, Floor 3", slotMinutes, maxPerDay));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<ResourceResponse>();
        return body!.Id;
    }

    [Fact]
    public async Task Member_CanBookAResource_WhenTheSlotIsFree()
    {
        var admin = await CreateClientAsync(UserRole.Admin, "admin");
        var member = await CreateClientAsync(UserRole.Member, "member");
        var resourceId = await CreateResourceAsync(admin);

        var start = DateTime.UtcNow.Date.AddDays(3).AddHours(9);

        var response = await member.PostAsJsonAsync("/api/bookings",
            new CreateBookingRequest(resourceId, start, start.AddMinutes(30)));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SecondMember_CannotBookTheSameSlot_AndGetsAConflict()
    {
        var admin = await CreateClientAsync(UserRole.Admin, "admin");
        var firstMember = await CreateClientAsync(UserRole.Member, "member-one");
        var secondMember = await CreateClientAsync(UserRole.Member, "member-two");
        var resourceId = await CreateResourceAsync(admin);

        var start = DateTime.UtcNow.Date.AddDays(4).AddHours(10);

        var firstResponse = await firstMember.PostAsJsonAsync("/api/bookings",
            new CreateBookingRequest(resourceId, start, start.AddMinutes(30)));
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var secondResponse = await secondMember.PostAsJsonAsync("/api/bookings",
            new CreateBookingRequest(resourceId, start, start.AddMinutes(30)));

        secondResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CancellingABooking_FreesTheSlotForSomeoneElse()
    {
        var admin = await CreateClientAsync(UserRole.Admin, "admin");
        var firstMember = await CreateClientAsync(UserRole.Member, "member-one");
        var secondMember = await CreateClientAsync(UserRole.Member, "member-two");
        var resourceId = await CreateResourceAsync(admin);

        var start = DateTime.UtcNow.Date.AddDays(5).AddHours(11);

        var firstResponse = await firstMember.PostAsJsonAsync("/api/bookings",
            new CreateBookingRequest(resourceId, start, start.AddMinutes(30)));
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var firstBooking = await firstResponse.Content.ReadFromJsonAsync<BookingResponse>();

        var cancelResponse = await firstMember.DeleteAsync($"/api/bookings/{firstBooking!.Id}");
        cancelResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var secondResponse = await secondMember.PostAsJsonAsync("/api/bookings",
            new CreateBookingRequest(resourceId, start, start.AddMinutes(30)));

        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Member_CannotExceedTheDailyBookingLimit()
    {
        var admin = await CreateClientAsync(UserRole.Admin, "admin");
        var member = await CreateClientAsync(UserRole.Member, "member");
        var resourceId = await CreateResourceAsync(admin, maxPerDay: 1);

        var day = DateTime.UtcNow.Date.AddDays(6);

        var first = await member.PostAsJsonAsync("/api/bookings",
            new CreateBookingRequest(resourceId, day.AddHours(9), day.AddHours(9).AddMinutes(30)));
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        var second = await member.PostAsJsonAsync("/api/bookings",
            new CreateBookingRequest(resourceId, day.AddHours(14), day.AddHours(14).AddMinutes(30)));

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Member_CannotCreateAResource()
    {
        var member = await CreateClientAsync(UserRole.Member, "member");

        var response = await member.PostAsJsonAsync("/api/resources",
            new CreateResourceRequest("Should Not Work", "n/a", "n/a", 30, 3));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
