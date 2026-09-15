using FluentValidation.TestHelper;

using MechanicShop.Domain.Workorders.Enums;

using Xunit;

using QueryType = MechanicShop.Application.Features.WorkOrders.Queries.GetWorkOrders.GetPublicTrackingInfoQuery;
using ValidatorType = MechanicShop.Application.Features.WorkOrders.Queries.GetWorkOrders.GetWorkOrdersQueryValidator;

namespace MechanicShop.Application.SubcutaneousTests.Features.WorkOrders.Queries.GetWorkOrders;

public class GetWorkOrdersQueryValidatorTests
{
    private readonly ValidatorType _validator;

    public GetWorkOrdersQueryValidatorTests()
    {
        _validator = new ValidatorType();
    }

    [Fact]
    public void Validator_WhenStateIsInvalidEnum_ShouldHaveValidationError()
    {
        var query = new QueryType(
            Page: 1,
            PageSize: 10,
            SearchTerm: null,
            State: (WorkOrderState)99);

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.State)
              .WithErrorMessage("Invalid work order state selected.");
    }

    [Fact]
    public void Validator_WhenStartDateToIsBeforeStartDateFrom_ShouldHaveValidationError()
    {
        var query = new QueryType(
            Page: 1,
            PageSize: 10,
            SearchTerm: null,
            StartDateFrom: DateTime.UtcNow.Date.AddDays(2),
            StartDateTo: DateTime.UtcNow.Date.AddDays(1));

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.StartDateTo)
              .WithErrorMessage("Start date 'To' must be greater than or equal to 'From'.");
    }

    [Fact]
    public void Validator_WhenSearchTermExceedsMaxLength_ShouldHaveValidationError()
    {
        var longSearchTerm = new string('A', 101);
        var query = new QueryType(
            Page: 1,
            PageSize: 10,
            SearchTerm: longSearchTerm);

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.SearchTerm)
              .WithErrorMessage("Search term cannot exceed 100 characters.");
    }

    [Fact]
    public void Validator_WhenQueryIsValid_ShouldNotHaveValidationErrors()
    {
        var query = new QueryType(
            Page: 1,
            PageSize: 10,
            SearchTerm: "Toyota",
            State: WorkOrderState.InProgress,
            StartDateFrom: DateTime.UtcNow.Date,
            StartDateTo: DateTime.UtcNow.Date.AddDays(1));

        var result = _validator.TestValidate(query);

        result.ShouldNotHaveAnyValidationErrors();
    }
}