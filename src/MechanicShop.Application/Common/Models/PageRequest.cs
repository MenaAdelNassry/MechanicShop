namespace MechanicShop.Application.Common.Models;

public record PageRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}