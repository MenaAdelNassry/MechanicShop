using System.Security.Claims;

using FluentAssertions;

using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.Identity.Dtos;
using MechanicShop.Application.Features.Identity.Queries.GetUserInfo;
using MechanicShop.Application.SubcutaneousTests.Common;
using MechanicShop.Domain.Common.Results;

using Microsoft.Extensions.Logging;

using NSubstitute;

using Xunit;

namespace MechanicShop.Application.SubcutaneousTests.Features.Identity.Queries.GetUserInfo;

[Collection(WebAppFactoryCollection.CollectionName)]
public class GetUserByIdQueryTests : BaseSubcutaneousTest
{
    private readonly IIdentityService _mockIdentityService;
    private readonly ILogger<GetUserByIdQueryHandler> _mockLogger;
    private readonly GetUserByIdQueryHandler _handler;

    public GetUserByIdQueryTests(WebAppFactory factory)
        : base(factory)
    {
        _mockIdentityService = Substitute.For<IIdentityService>();
        _mockLogger = Substitute.For<ILogger<GetUserByIdQueryHandler>>();

        _handler = new GetUserByIdQueryHandler(_mockLogger, _mockIdentityService);
    }

    [Fact]
    public async Task Handle_WhenUserExists_ShouldReturnAppUserDtoSuccessfully()
    {
        // Arrange
        var userId = Guid.CreateVersion7().ToString();
        var query = new GetUserByIdQuery(userId);

        var expectedUserDto = new AppUserDto(
            UserId: userId,
            Email: "mechanic@shop.com",
            Roles: new List<string> { "Manager" },
            Claims: new List<Claim>());

        // Using implicit conversion: Task<Result<AppUserDto>> accepts expectedUserDto directly
        _mockIdentityService.GetUserByIdAsync(userId)
            .Returns(Task.FromResult<Result<AppUserDto>>(expectedUserDto));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.UserId.Should().Be(userId);
        result.Value.Email.Should().Be("mechanic@shop.com");
        result.Value.Roles.Should().ContainSingle().Which.Should().Be("Manager");
    }

    [Fact]
    public async Task Handle_WhenUserDoesNotExist_ShouldReturnFailureError()
    {
        // Arrange
        var userId = Guid.CreateVersion7().ToString();
        var query = new GetUserByIdQuery(userId);

        var userNotFoundError = Error.NotFound("User_NotFound", $"User with Id '{userId}' was not found.");

        // Using implicit conversion to mock the failure result directly
        _mockIdentityService.GetUserByIdAsync(userId)
            .Returns(Task.FromResult<Result<AppUserDto>>(userNotFoundError));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be("User_NotFound");
        result.TopError.Description.Should().Be($"User with Id '{userId}' was not found.");
    }
}