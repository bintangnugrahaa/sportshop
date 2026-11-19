using SportShop.Domain.Common;
using SportShop.Domain.Shipping;

namespace SportShop.Domain.Users;

public class Address : IEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Label { get; set; } = string.Empty;
    public string RecipientName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string ProvinceCode { get; set; } = string.Empty;
    public ShippingRate? Province { get; set; }
    public string City { get; set; } = string.Empty;
    public string? District { get; set; }
    public string PostalCode { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Address()
    {
    }

    public Address(
        Guid id,
        Guid userId,
        string label,
        string recipientName,
        string phone,
        string provinceCode,
        string city,
        string? district,
        string postalCode,
        string street,
        bool isDefault = false,
        DateTime? createdAt = null)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("address.invalid_user", "User ID tidak boleh kosong.");
        }

        ValidateFields(label, recipientName, phone, provinceCode, city, postalCode, street);

        Id = id;
        UserId = userId;
        Label = label.Trim();
        RecipientName = recipientName.Trim();
        Phone = phone.Trim();
        ProvinceCode = provinceCode.Trim().ToUpperInvariant();
        City = city.Trim();
        District = district?.Trim();
        PostalCode = postalCode.Trim();
        Street = street.Trim();
        IsDefault = isDefault;
        IsDeleted = false;
        CreatedAt = createdAt ?? DateTime.UtcNow;
    }

    public void UpdateDetails(
        string label,
        string recipientName,
        string phone,
        string provinceCode,
        string city,
        string? district,
        string postalCode,
        string street,
        bool isDefault,
        DateTime now)
    {
        ValidateFields(label, recipientName, phone, provinceCode, city, postalCode, street);

        Label = label.Trim();
        RecipientName = recipientName.Trim();
        Phone = phone.Trim();
        ProvinceCode = provinceCode.Trim().ToUpperInvariant();
        City = city.Trim();
        District = district?.Trim();
        PostalCode = postalCode.Trim();
        Street = street.Trim();
        IsDefault = isDefault;
        UpdatedAt = now;
    }

    public void SetDefault(bool isDefault, DateTime now)
    {
        IsDefault = isDefault;
        UpdatedAt = now;
    }

    public void MarkDeleted(DateTime now)
    {
        IsDeleted = true;
        IsDefault = false;
        UpdatedAt = now;
    }

    private static void ValidateFields(
        string label,
        string recipientName,
        string phone,
        string provinceCode,
        string city,
        string postalCode,
        string street)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            throw new DomainException("address.invalid_label", "Label alamat tidak boleh kosong.");
        }

        if (string.IsNullOrWhiteSpace(recipientName))
        {
            throw new DomainException("address.invalid_recipient", "Nama penerima tidak boleh kosong.");
        }

        if (string.IsNullOrWhiteSpace(phone))
        {
            throw new DomainException("address.invalid_phone", "Nomor telepon penerima tidak boleh kosong.");
        }

        if (string.IsNullOrWhiteSpace(provinceCode))
        {
            throw new DomainException("address.invalid_province", "Provinsi tidak boleh kosong.");
        }

        if (string.IsNullOrWhiteSpace(city))
        {
            throw new DomainException("address.invalid_city", "Kota/kabupaten tidak boleh kosong.");
        }

        if (string.IsNullOrWhiteSpace(postalCode))
        {
            throw new DomainException("address.invalid_postal_code", "Kode pos tidak boleh kosong.");
        }

        if (string.IsNullOrWhiteSpace(street))
        {
            throw new DomainException("address.invalid_street", "Alamat jalan tidak boleh kosong.");
        }
    }
}
