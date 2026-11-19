using SportShop.Domain.Common;

namespace SportShop.Domain.Orders;

public record OrderShippingAddress
{
    public string RecipientName { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string ProvinceCode { get; init; } = string.Empty;
    public string ProvinceName { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string PostalCode { get; init; } = string.Empty;
    public string Street { get; init; } = string.Empty;

    public OrderShippingAddress()
    {
    }

    public OrderShippingAddress(
        string recipientName,
        string phone,
        string provinceCode,
        string provinceName,
        string city,
        string postalCode,
        string street)
    {
        if (string.IsNullOrWhiteSpace(recipientName))
        {
            throw new DomainException("order.invalid_recipient", "Nama penerima tidak boleh kosong.");
        }

        if (string.IsNullOrWhiteSpace(phone))
        {
            throw new DomainException("order.invalid_phone", "Nomor telepon penerima tidak boleh kosong.");
        }

        if (string.IsNullOrWhiteSpace(provinceCode))
        {
            throw new DomainException("order.invalid_province", "Provinsi tidak boleh kosong.");
        }

        if (string.IsNullOrWhiteSpace(provinceName))
        {
            throw new DomainException("order.invalid_province", "Nama provinsi tidak boleh kosong.");
        }

        if (string.IsNullOrWhiteSpace(city))
        {
            throw new DomainException("order.invalid_city", "Kota/kabupaten tidak boleh kosong.");
        }

        if (string.IsNullOrWhiteSpace(postalCode))
        {
            throw new DomainException("order.invalid_postal_code", "Kode pos tidak boleh kosong.");
        }

        if (string.IsNullOrWhiteSpace(street))
        {
            throw new DomainException("order.invalid_street", "Alamat jalan tidak boleh kosong.");
        }

        RecipientName = recipientName.Trim();
        Phone = phone.Trim();
        ProvinceCode = provinceCode.Trim().ToUpperInvariant();
        ProvinceName = provinceName.Trim();
        City = city.Trim();
        PostalCode = postalCode.Trim();
        Street = street.Trim();
    }
}
