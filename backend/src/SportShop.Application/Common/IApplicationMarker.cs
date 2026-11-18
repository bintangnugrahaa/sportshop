using SportShop.Domain.Common;

namespace SportShop.Application.Common;

public interface IApplicationMarker
{
    Type DomainType => typeof(IEntity);
}
