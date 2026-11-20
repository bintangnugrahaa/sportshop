namespace SportShop.Domain.Payments;

public enum PaymentStatus
{
    Pending,
    Settlement,
    Capture,
    Deny,
    Cancel,
    Expire,
    Failure,
    Refund
}
