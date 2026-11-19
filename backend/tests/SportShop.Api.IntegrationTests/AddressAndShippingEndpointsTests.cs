using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SportShop.Api.Controllers;
using SportShop.Api.Models;
using SportShop.Application.Features.Addresses.DTOs;
using SportShop.Application.Features.Auth.Login;
using SportShop.Application.Features.Auth.Register;
using SportShop.Application.Features.Shipping.DTOs;
using SportShop.Domain.Catalog;
using SportShop.Infrastructure.Persistence;
using Xunit;

namespace SportShop.Api.IntegrationTests;

public class AddressAndShippingEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AddressAndShippingEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateTestClient()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false
        });
        client.DefaultRequestHeaders.Add("X-Forwarded-For", $"192.168.53.{Random.Shared.Next(1, 10000)}");
        return client;
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = CreateTestClient();
        var email = $"addr_user_{Guid.NewGuid():N}@sportshop.dev";
        var password = "Password123!";

        var regResp = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterCommand(email, password, "Address Tester", "08123456789"));
        regResp.EnsureSuccessStatusCode();

        var loginResp = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginCommand(email, password));
        loginResp.EnsureSuccessStatusCode();
        var loginData = await loginResp.Content.ReadFromJsonAsync<LoginResponse>();

        client.WithBearerToken(loginData!.AccessToken);
        return client;
    }

    private async Task<Guid> SeedProductAndVariantAsync(int stockQuantity = 10, decimal price = 200_000m, int weightGram = 600)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var suffix = Guid.NewGuid().ToString()[..8];
        var category = new Category(Guid.NewGuid(), $"Cat-{suffix}", $"cat-{suffix}");
        var brand = new Brand(Guid.NewGuid(), $"Brand-{suffix}", $"brand-{suffix}");
        db.Categories.Add(category);
        db.Brands.Add(brand);

        var product = new Product(
            Guid.NewGuid(),
            category.Id,
            brand.Id,
            $"Produk-{suffix}",
            $"produk-{suffix}",
            "Deskripsi produk test",
            price);
        db.Products.Add(product);

        var variant = new ProductVariant(
            Guid.NewGuid(),
            product.Id,
            $"SKU-{suffix}",
            price,
            weightGram: weightGram,
            stockQuantity: stockQuantity,
            optionSize: "M",
            optionColor: "Merah");
        db.ProductVariants.Add(variant);

        await db.SaveChangesAsync();
        return variant.Id;
    }

    [Fact]
    public async Task GetProvinces_ShouldReturnActiveProvinces()
    {
        var client = CreateTestClient();

        var response = await client.GetAsync("/api/v1/shipping/provinces");

        response.EnsureSuccessStatusCode();
        var provinces = await response.Content.ReadFromJsonAsync<List<ShippingRateDto>>();
        provinces.Should().NotBeNull();
        provinces!.Count.Should().BeGreaterThanOrEqualTo(30);
    }

    [Fact]
    public async Task Address_CrudFlow_AndSingleDefault_ShouldWorkProperly()
    {
        var client = await CreateAuthenticatedClientAsync();

        // 1. Create first address -> should automatically be default
        var createFirstReq = new AddressesController.CreateAddressRequest(
            Label: "Rumah",
            RecipientName: "Budi Santoso",
            Phone: "081234567890",
            ProvinceCode: "JK",
            City: "Jakarta Selatan",
            District: "Kebayoran Baru",
            PostalCode: "12110",
            Street: "Jl. Senopati No. 1",
            IsDefault: false);

        var createFirstResp = await client.PostAsJsonAsync("/api/v1/addresses", createFirstReq);
        createFirstResp.EnsureSuccessStatusCode();
        var addr1 = await createFirstResp.Content.ReadFromJsonAsync<AddressDto>();
        addr1.Should().NotBeNull();
        addr1!.IsDefault.Should().BeTrue(); // Auto default for first address!
        addr1.ProvinceName.Should().Be("DKI Jakarta");

        // 2. Create second address marked as default -> should unset first address default
        var createSecondReq = new AddressesController.CreateAddressRequest(
            Label: "Kantor",
            RecipientName: "Budi Kantor",
            Phone: "081234567891",
            ProvinceCode: "JB",
            City: "Bandung",
            District: "Coblong",
            PostalCode: "40132",
            Street: "Jl. Dago No. 10",
            IsDefault: true);

        var createSecondResp = await client.PostAsJsonAsync("/api/v1/addresses", createSecondReq);
        createSecondResp.EnsureSuccessStatusCode();
        var addr2 = await createSecondResp.Content.ReadFromJsonAsync<AddressDto>();
        addr2.Should().NotBeNull();
        addr2!.IsDefault.Should().BeTrue();

        // 3. List addresses -> should have 2, default (addr2) first
        var listResp = await client.GetAsync("/api/v1/addresses");
        listResp.EnsureSuccessStatusCode();
        var list = await listResp.Content.ReadFromJsonAsync<List<AddressDto>>();
        list.Should().HaveCount(2);
        list![0].Id.Should().Be(addr2.Id);
        list[0].IsDefault.Should().BeTrue();
        list[1].Id.Should().Be(addr1.Id);
        list[1].IsDefault.Should().BeFalse();

        // 4. Update addr1 details and set default back to addr1
        var updateReq = new AddressesController.UpdateAddressRequest(
            Label: "Rumah Utama",
            RecipientName: "Budi S.",
            Phone: "081234567890",
            ProvinceCode: "JK",
            City: "Jakarta Selatan",
            District: "Tebet",
            PostalCode: "12810",
            Street: "Jl. Tebet Barat No. 5",
            IsDefault: true);

        var updateResp = await client.PutAsJsonAsync($"/api/v1/addresses/{addr1.Id}", updateReq);
        updateResp.EnsureSuccessStatusCode();
        var updatedAddr1 = await updateResp.Content.ReadFromJsonAsync<AddressDto>();
        updatedAddr1!.IsDefault.Should().BeTrue();
        updatedAddr1.Label.Should().Be("Rumah Utama");

        // 5. Delete addr2 (soft delete)
        var deleteResp = await client.DeleteAsync($"/api/v1/addresses/{addr2.Id}");
        deleteResp.EnsureSuccessStatusCode();

        // Deleted address is no longer returned in list
        var listAfterDeleteResp = await client.GetAsync("/api/v1/addresses");
        var listAfterDelete = await listAfterDeleteResp.Content.ReadFromJsonAsync<List<AddressDto>>();
        listAfterDelete.Should().NotBeNull();
        listAfterDelete!.Should().HaveCount(1);
        listAfterDelete![0].Id.Should().Be(addr1.Id);
    }

    [Fact]
    public async Task Address_ResourceOwnership_OtherUserAddress_ShouldReturn404()
    {
        var clientA = await CreateAuthenticatedClientAsync();
        var clientB = await CreateAuthenticatedClientAsync();

        // User A creates address
        var createReq = new AddressesController.CreateAddressRequest(
            Label: "Rumah A",
            RecipientName: "User A",
            Phone: "081234567890",
            ProvinceCode: "JK",
            City: "Jakarta",
            District: null,
            PostalCode: "12345",
            Street: "Jl. A",
            IsDefault: true);

        var createResp = await clientA.PostAsJsonAsync("/api/v1/addresses", createReq);
        createResp.EnsureSuccessStatusCode();
        var addrA = await createResp.Content.ReadFromJsonAsync<AddressDto>();

        // User B attempts GET User A's address -> 404
        var getResp = await clientB.GetAsync($"/api/v1/addresses/{addrA!.Id}");
        getResp.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await getResp.Content.ReadFromJsonAsync<SportShopProblemDetails>();
        problem!.Code.Should().Be("address.not_found");

        // User B attempts PUT User A's address -> 404
        var updateResp = await clientB.PutAsJsonAsync($"/api/v1/addresses/{addrA.Id}", createReq);
        updateResp.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // User B attempts DELETE User A's address -> 404
        var deleteResp = await clientB.DeleteAsync($"/api/v1/addresses/{addrA.Id}");
        deleteResp.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // User B attempts SetDefault User A's address -> 404
        var setDefaultResp = await clientB.PostAsync($"/api/v1/addresses/{addrA.Id}/set-default", null);
        setDefaultResp.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // User B attempts shipping quote with User A's address -> 404
        var quoteResp = await clientB.PostAsJsonAsync("/api/v1/shipping/quote", new ShippingController.ShippingQuoteRequest(addrA.Id));
        quoteResp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ShippingQuote_ShouldCalculateFee_BasedOnWeightAndAddress()
    {
        var client = await CreateAuthenticatedClientAsync();

        // 1. Create address in DKI Jakarta ("JK": base 10,000, perKg 5,000)
        var createAddrReq = new AddressesController.CreateAddressRequest(
            Label: "Rumah",
            RecipientName: "Penerima",
            Phone: "08123456789",
            ProvinceCode: "JK",
            City: "Jakarta Selatan",
            District: "Pasar Minggu",
            PostalCode: "12520",
            Street: "Jl. Ragunan No. 10",
            IsDefault: true);

        var addrResp = await client.PostAsJsonAsync("/api/v1/addresses", createAddrReq);
        addrResp.EnsureSuccessStatusCode();
        var address = await addrResp.Content.ReadFromJsonAsync<AddressDto>();

        // 2. Seed product with weight 600g and price 200,000 IDR
        var variantId = await SeedProductAndVariantAsync(stockQuantity: 10, price: 200_000m, weightGram: 600);

        // Add 2 items to cart: total weight = 2 * 600g = 1200g -> ceil(1.2) = 2kg billable
        var addResp = await client.PostAsJsonAsync("/api/v1/cart/items", new CartController.AddCartItemRequest(variantId, 2));
        addResp.EnsureSuccessStatusCode();

        // 3. Request quote for address
        var quoteResp = await client.PostAsJsonAsync("/api/v1/shipping/quote", new ShippingController.ShippingQuoteRequest(address!.Id));
        quoteResp.EnsureSuccessStatusCode();
        var quote = await quoteResp.Content.ReadFromJsonAsync<ShippingQuoteDto>();

        quote.Should().NotBeNull();
        quote!.AddressId.Should().Be(address.Id);
        quote.ProvinceCode.Should().Be("JK");
        quote.ProvinceName.Should().Be("DKI Jakarta");
        quote.TotalWeightGram.Should().Be(1200);
        quote.BillableWeightKg.Should().Be(2);
        quote.BaseFee.Should().Be(10_000m);
        quote.PerKgFee.Should().Be(5_000m);
        // Shipping fee = 10,000 + (2 * 5,000) = 20,000
        quote.ShippingFee.Should().Be(20_000m);
        quote.CartSubtotal.Should().Be(400_000m);
        quote.EstimatedTotal.Should().Be(420_000m);
    }
}
