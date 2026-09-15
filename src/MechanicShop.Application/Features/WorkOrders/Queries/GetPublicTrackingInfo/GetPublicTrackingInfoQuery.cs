using MechanicShop.Application.Features.WorkOrders.Dtos;
using MechanicShop.Domain.Common.Results;

using MediatR;

namespace MechanicShop.Application.Features.WorkOrders.Queries.GetPublicTrackingInfo;

public sealed record GetPublicTrackingInfoQuery(Guid TrackingToken) : IRequest<Result<PublicTrackingDto>>;