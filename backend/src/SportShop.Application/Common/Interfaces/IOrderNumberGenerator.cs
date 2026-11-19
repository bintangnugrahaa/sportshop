namespace SportShop.Application.Common.Interfaces;

public interface IOrderNumberGenerator
{
    string Generate(DateTime? timestamp = null);
}
