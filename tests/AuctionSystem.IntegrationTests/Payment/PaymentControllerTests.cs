using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AuctionSystem.Application.Users.PaymentMethods;
using AuctionSystem.Domain.Users;
using AuctionSystem.IntegrationTests.Infrastructure;
using Xunit.Sdk;

namespace AuctionSystem.IntegrationTests.Payment;

public sealed class PaymentControllerTests
{
    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync();
        throw new XunitException($"HTTP {(int)response.StatusCode} {response.StatusCode}: {body}");
    }

    [Fact]
    public async Task PaymentCrud_WithValidData_ReturnsSuccess()
    {
        await using var factory = new PaymentWebApplicationFactory();
        var client = factory.CreateClient();

        var userId = await factory.SeedUserAsync();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TestJwtTokenFactory.Create(userId, "payment-user@example.com", UserRole.Bidder.ToString()));

        var addRequest = new
        {
            Type = "Card",
            Provider = "Visa",
            Last4 = "4242",
            ExpiryMonth = 10,
            ExpiryYear = DateTime.UtcNow.Year + 1,
            HolderName = "John Doe",
            IsDefault = true
        };

        var addResponse = await client.PostAsJsonAsync("/api/payment", addRequest);
        await EnsureSuccessAsync(addResponse);

        var added = await addResponse.Content.ReadFromJsonAsync<PaymentMethodDto>();
        Assert.NotNull(added);
        Assert.Equal(userId, added!.UserId);
        Assert.Equal("Visa", added.Provider);

        var getResponse = await client.GetAsync("/api/payment");
        await EnsureSuccessAsync(getResponse);

        var methods = await getResponse.Content.ReadFromJsonAsync<IReadOnlyList<PaymentMethodDto>>();
        Assert.NotNull(methods);
        Assert.Single(methods!);
        Assert.Equal(added.Id, methods[0].Id);

        var deleteResponse = await client.DeleteAsync($"/api/payment/{added.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getAfterDeleteResponse = await client.GetAsync("/api/payment");
        await EnsureSuccessAsync(getAfterDeleteResponse);

        var methodsAfterDelete = await getAfterDeleteResponse.Content.ReadFromJsonAsync<IReadOnlyList<PaymentMethodDto>>();
        Assert.NotNull(methodsAfterDelete);
        Assert.Empty(methodsAfterDelete!);
    }

    [Fact]
    public async Task AddPaymentMethod_WithAuthenticatedUserMissing_ReturnsFailure()
    {
        await using var factory = new PaymentWebApplicationFactory();
        var client = factory.CreateClient();
        var userId = Guid.NewGuid();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TestJwtTokenFactory.Create(userId, "payment-user@example.com", UserRole.Bidder.ToString()));

        var addRequest = new
        {
            Type = "Card",
            Provider = "Mastercard",
            Last4 = "1111",
            ExpiryMonth = 1,
            ExpiryYear = DateTime.UtcNow.Year + 1,
            HolderName = "Jane Doe",
            IsDefault = false
        };

        var response = await client.PostAsJsonAsync("/api/payment", addRequest);

        Assert.False(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task RemovePaymentMethod_WithUnknownMethod_ReturnsFailure()
    {
        await using var factory = new PaymentWebApplicationFactory();
        var client = factory.CreateClient();

        var userId = await factory.SeedUserAsync();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TestJwtTokenFactory.Create(userId, "payment-user@example.com", UserRole.Bidder.ToString()));

        var response = await client.DeleteAsync($"/api/payment/{Guid.NewGuid()}");

        Assert.False(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task DeletePaymentMethod_BelongingToAnotherUser_ReturnsFailure()
    {
        await using var factory = new PaymentWebApplicationFactory();
        var ownerClient = factory.CreateClient();
        var intruderClient = factory.CreateClient();

        var ownerUserId = await factory.SeedUserAsync(email: "owner@example.com");
        var intruderUserId = await factory.SeedUserAsync(email: "intruder@example.com");

        ownerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TestJwtTokenFactory.Create(ownerUserId, "owner@example.com", UserRole.Bidder.ToString()));
        intruderClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TestJwtTokenFactory.Create(intruderUserId, "intruder@example.com", UserRole.Bidder.ToString()));

        var addResponse = await ownerClient.PostAsJsonAsync("/api/payment", new
        {
            Type = "Card",
            Provider = "Visa",
            Last4 = "4242",
            ExpiryMonth = 9,
            ExpiryYear = DateTime.UtcNow.Year + 1,
            HolderName = "Owner User",
            IsDefault = true
        });
        await EnsureSuccessAsync(addResponse);

        var added = await addResponse.Content.ReadFromJsonAsync<PaymentMethodDto>();
        Assert.NotNull(added);

        var intruderDeleteResponse = await intruderClient.DeleteAsync($"/api/payment/{added!.Id}");

        Assert.False(intruderDeleteResponse.IsSuccessStatusCode);

        var ownerGetResponse = await ownerClient.GetAsync("/api/payment");
        await EnsureSuccessAsync(ownerGetResponse);
        var ownerMethods = await ownerGetResponse.Content.ReadFromJsonAsync<IReadOnlyList<PaymentMethodDto>>();

        var intruderGetResponse = await intruderClient.GetAsync("/api/payment");
        await EnsureSuccessAsync(intruderGetResponse);
        var intruderMethods = await intruderGetResponse.Content.ReadFromJsonAsync<IReadOnlyList<PaymentMethodDto>>();

        Assert.NotNull(ownerMethods);
        Assert.Single(ownerMethods!);
        Assert.NotNull(intruderMethods);
        Assert.Empty(intruderMethods!);
    }
}
