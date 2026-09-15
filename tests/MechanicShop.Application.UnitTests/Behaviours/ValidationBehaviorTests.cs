using FluentAssertions; // FluentAssertions imported

using FluentValidation;
using FluentValidation.Results;

using MechanicShop.Application.Common.Behaviours;
using MechanicShop.Application.Features.WorkOrders.Commands.CreateWorkOrder;
using MechanicShop.Application.Features.WorkOrders.Dtos;
using MechanicShop.Application.Features.WorkOrders.Mappers;
using MechanicShop.Domain.Common.Results;
using MechanicShop.Tests.Common.WorkOrders;

using MediatR;

using NSubstitute;

using Xunit;

namespace MechanicShop.Application.UnitTests.Behaviours;

public class ValidationBehaviorTests
{
    private readonly ValidationBehavior<CreateWorkOrderCommand, Result<WorkOrderDto>> _validationBehavior;
    private readonly IValidator<CreateWorkOrderCommand> _mockValidator;
    private readonly RequestHandlerDelegate<Result<WorkOrderDto>> _mockNextBehavior;

    public ValidationBehaviorTests()
    {
        _mockNextBehavior = Substitute.For<RequestHandlerDelegate<Result<WorkOrderDto>>>();
        _mockValidator = Substitute.For<IValidator<CreateWorkOrderCommand>>();

        _validationBehavior = new(_mockValidator);
    }

    [Fact]
    public async Task InvokeValidationBehavior_WhenValidatorResultIsValid_ShouldInvokeNextBehavior()
    {
        // Arrange
        var createWorkOrderCommand = WorkOrderCommandFactory.CreateCreateWorkOrderCommand();
        var workOrderResponse = WorkOrderFactory.CreateTestWorkOrder().Value.ToDto();

        _mockValidator
            .ValidateAsync(createWorkOrderCommand, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        // Act
        var result = await _validationBehavior.Handle(
            createWorkOrderCommand,
            _ => Task.FromResult<Result<WorkOrderDto>>(workOrderResponse),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(workOrderResponse);
    }

    [Fact]
    public async Task InvokeValidationBehavior_WhenValidatorResultIsNotValid_ShouldReturnListOfErrors()
    {
        // Arrange
        var createWorkOrderCommand = WorkOrderCommandFactory.CreateCreateWorkOrderCommand();

        List<ValidationFailure> validationFailures = [new(propertyName: "property1", errorMessage: "property1 is invalid")];

        _mockValidator
            .ValidateAsync(createWorkOrderCommand, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(validationFailures));

        // Act
        var result = await _validationBehavior.Handle(createWorkOrderCommand, _mockNextBehavior, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be("property1");
        result.TopError.Description.Should().Be("property1 is invalid");

        // Ensure that next behavior was never reached/invoked when validation failed
        await _mockNextBehavior.DidNotReceive().Invoke();
    }

    [Fact]
    public async Task InvokeValidationBehavior_WhenNoValidator_ShouldInvokeNextBehavior()
    {
        // Arrange
        var createWorkOrderCommand = WorkOrderCommandFactory.CreateCreateWorkOrderCommand();
        var validationBehavior = new ValidationBehavior<CreateWorkOrderCommand, Result<WorkOrderDto>>();

        var workOrderResponse = WorkOrderFactory.CreateTestWorkOrder().Value.ToDto();
        Result<WorkOrderDto> expectedResult = workOrderResponse;

        _mockNextBehavior.Invoke().Returns(expectedResult);

        // Act
        var result = await validationBehavior.Handle(
            createWorkOrderCommand,
            _mockNextBehavior,
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(workOrderResponse);

        // Asserting that the pipeline indeed continued without any validation interruption
        await _mockNextBehavior.Received(1).Invoke();
    }
}