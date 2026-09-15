using FluentValidation.TestHelper;
using MechanicShop.Application.Features.WorkOrders.Queries.GetWorkOrderByIdQuery;
using Xunit;

using QueryType = MechanicShop.Application.Features.WorkOrders.Queries.GetWorkOrderByIdQuery.GetWorkOrderByIdQuery;

namespace MechanicShop.Application.SubcutaneousTests.Features.WorkOrders.Queries.GetWorkOrderByIdQuery;

public class GetWorkOrderByIdQueryValidatorTests
{
    private readonly GetAppointmentByIdQueryValidator _validator;

    public GetWorkOrderByIdQueryValidatorTests()
    {
        _validator = new GetAppointmentByIdQueryValidator();
    }

    [Fact]
    public void Validator_WhenWorkOrderIdIsEmpty_ShouldHaveValidationError()
    {
        var query = new QueryType(Guid.Empty);

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.WorkOrderId)
              .WithErrorCode("WorkOrderId_Is_Required")
              .WithErrorMessage("WorkOrderId is required.");
    }

    [Fact]
    public void Validator_WhenWorkOrderIdIsValid_ShouldNotHaveValidationErrors()
    {
        var query = new QueryType(Guid.CreateVersion7());

        var result = _validator.TestValidate(query);

        result.ShouldNotHaveAnyValidationErrors();
    }
}