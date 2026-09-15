using MechanicShop.Domain.Common.Results;

namespace MechanicShop.Domain.Common;

public interface ISoftDelete
{
    bool IsDeleted { get; }
    Result<Updated> Delete(TimeProvider? timeProvider = null);
    DateTimeOffset? DeletedAtUtc { get; }
}