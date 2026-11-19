using FluentAssertions;
using SportShop.Domain.Common;
using SportShop.Domain.Users;
using Xunit;

namespace SportShop.Domain.Tests;

public sealed class AddressDomainTests
{
    private static Address CreateValidAddress(Guid? userId = null, bool isDefault = false)
    {
        return new Address(
            Guid.NewGuid(),
            userId ?? Guid.NewGuid(),
            "Rumah",
            "Budi Santoso",
            "081234567890",
            "JK",
            "Jakarta Selatan",
            "Kebayoran Baru",
            "12110",
            "Jl. Sudirman No. 123",
            isDefault);
    }

    [Fact]
    public void Address_Creation_ShouldThrow_WhenUserIdIsEmpty()
    {
        var act = () => new Address(
            Guid.NewGuid(),
            Guid.Empty,
            "Rumah",
            "Budi",
            "0812",
            "JK",
            "Jakarta",
            null,
            "12110",
            "Jl. Test");

        act.Should().Throw<DomainException>()
            .Where(e => e.Code == "address.invalid_user");
    }

    [Theory]
    [InlineData("", "Budi", "0812", "JK", "Jakarta", "12110", "Jl. Test", "address.invalid_label")]
    [InlineData("Rumah", "", "0812", "JK", "Jakarta", "12110", "Jl. Test", "address.invalid_recipient")]
    [InlineData("Rumah", "Budi", "", "JK", "Jakarta", "12110", "Jl. Test", "address.invalid_phone")]
    [InlineData("Rumah", "Budi", "0812", "", "Jakarta", "12110", "Jl. Test", "address.invalid_province")]
    [InlineData("Rumah", "Budi", "0812", "JK", "", "12110", "Jl. Test", "address.invalid_city")]
    [InlineData("Rumah", "Budi", "0812", "JK", "Jakarta", "", "Jl. Test", "address.invalid_postal_code")]
    [InlineData("Rumah", "Budi", "0812", "JK", "Jakarta", "12110", "", "address.invalid_street")]
    public void Address_Creation_ShouldThrow_WhenRequiredFieldIsBlank(
        string label,
        string recipient,
        string phone,
        string province,
        string city,
        string postalCode,
        string street,
        string expectedErrorCode)
    {
        var act = () => new Address(
            Guid.NewGuid(),
            Guid.NewGuid(),
            label,
            recipient,
            phone,
            province,
            city,
            null,
            postalCode,
            street);

        act.Should().Throw<DomainException>()
            .Where(e => e.Code == expectedErrorCode);
    }

    [Fact]
    public void Address_UpdateDetails_ShouldUpdateFields()
    {
        var address = CreateValidAddress();
        var now = DateTime.UtcNow;

        address.UpdateDetails(
            "Kantor",
            "Budi Baru",
            "0899999999",
            "JB",
            "Bandung",
            "Coblong",
            "40132",
            "Jl. Dago No. 1",
            isDefault: true,
            now);

        address.Label.Should().Be("Kantor");
        address.RecipientName.Should().Be("Budi Baru");
        address.ProvinceCode.Should().Be("JB");
        address.City.Should().Be("Bandung");
        address.IsDefault.Should().BeTrue();
        address.UpdatedAt.Should().Be(now);
    }

    [Fact]
    public void Address_MarkDeleted_ShouldSetIsDeletedAndUnsetDefault()
    {
        var address = CreateValidAddress(isDefault: true);
        var now = DateTime.UtcNow;

        address.MarkDeleted(now);

        address.IsDeleted.Should().BeTrue();
        address.IsDefault.Should().BeFalse();
        address.UpdatedAt.Should().Be(now);
    }
}
