using FluentAssertions;
using MechanicShop.Domain.Inventory;
using MechanicShop.Domain.Inventory.Events;
using Xunit;

namespace MechanicShop.Domain.UnitTests.Inventory;

public class InventoryItemTests
{
    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        var id = Guid.CreateVersion7();
        var result = InventoryItem.Create(id, "Brake Pad", 100m, 10, 2);

        result.IsSuccess.Should().BeTrue();
        var item = result.Value;
        item.Id.Should().Be(id);
        item.Name.Should().Be("Brake Pad");
        item.Cost.Should().Be(100m);
        item.StockQuantity.Should().Be(10);
        item.ReorderLevel.Should().Be(2);
    }

    [Fact]
    public void ReserveStock_WithSufficientStock_ShouldSucceed_And_RaiseEvents()
    {
        var item = InventoryItem.Create(Guid.CreateVersion7(), "Pad", 50m, 5, 2).Value;

        var result = item.ReserveStock(2, Guid.CreateVersion7(), Guid.CreateVersion7());

        result.IsSuccess.Should().BeTrue();
        item.StockQuantity.Should().Be(3);
        item.DomainEvents.Should().ContainSingle(e => e is InventoryItemStockReservedDomainEvent);
    }

    [Fact]
    public void ReserveStock_WithInsufficientStock_ShouldFail()
    {
        var item = InventoryItem.Create(Guid.CreateVersion7(), "Pad", 50m, 1, 2).Value;

        var result = item.ReserveStock(2, Guid.CreateVersion7(), Guid.CreateVersion7());

        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be(InventoryErrors.InsufficientStock(2, 1).Code);
    }

    [Fact]
    public void ReserveStock_WhenDroppingBelowReorder_ShouldRaiseLowStockEvent()
    {
        var item = InventoryItem.Create(Guid.CreateVersion7(), "Pad", 50m, 3, 2).Value;

        var result = item.ReserveStock(1, Guid.CreateVersion7(), Guid.CreateVersion7());

        result.IsSuccess.Should().BeTrue();
        item.DomainEvents.Should().Contain(e => e is InventoryItemLowStockDomainEvent);
    }

    [Fact]
    public void ReleaseStock_ShouldIncreaseStock_And_RaiseEvent()
    {
        var item = InventoryItem.Create(Guid.CreateVersion7(), "Pad", 50m, 2, 1).Value;

        var result = item.ReleaseStock(1, Guid.CreateVersion7(), Guid.CreateVersion7());

        result.IsSuccess.Should().BeTrue();
        item.StockQuantity.Should().Be(3);
        item.DomainEvents.Should().Contain(e => e is InventoryItemStockReleasedDomainEvent);
    }

    [Fact]
    public void Restock_ShouldIncreaseStock_And_RaiseEvent()
    {
        var item = InventoryItem.Create(Guid.CreateVersion7(), "Pad", 50m, 2, 1).Value;

        var result = item.Restock(5);

        result.IsSuccess.Should().BeTrue();
        item.StockQuantity.Should().Be(7);
        item.DomainEvents.Should().Contain(e => e is InventoryItemRestockedDomainEvent);
    }

    [Fact]
    public void AdjustStock_ShouldChangeStock_And_RaiseEvent()
    {
        var item = InventoryItem.Create(Guid.CreateVersion7(), "Pad", 50m, 2, 1).Value;

        var result = item.AdjustStock(-1, "Damage", Guid.CreateVersion7());

        result.IsSuccess.Should().BeTrue();
        item.StockQuantity.Should().Be(1);
        item.DomainEvents.Should().Contain(e => e is InventoryItemStockAdjustedDomainEvent);
    }

    [Fact]
    public void Archive_ShouldSetIsDeleted()
    {
        var item = InventoryItem.Create(Guid.CreateVersion7(), "Pad", 50m, 2, 1).Value;

        var result = item.Delete();

        result.IsSuccess.Should().BeTrue();
        item.IsDeleted.Should().BeTrue();
    }

    [Theory]
    [InlineData("", 100, 10, 2)] // Name is empty
    [InlineData("Pad", 0, 10, 2)] // Cost is 0
    [InlineData("Pad", -10, 10, 2)] // Cost is negative
    [InlineData("Pad", 100, -1, 2)] // Initial stock is negative
    [InlineData("Pad", 100, 10, -1)] // Reorder level is negative
    public void Create_WithInvalidData_ShouldFail(string name, decimal cost, int stockQuantity, int reorderLevel)
    {
        var result = InventoryItem.Create(Guid.CreateVersion7(), name, cost, stockQuantity, reorderLevel);

        result.IsError.Should().BeTrue();
    }

    [Fact]
    public void AdjustStock_WithoutReason_ShouldFail()
    {
        var item = InventoryItem.Create(Guid.CreateVersion7(), "Pad", 50m, 5, 2).Value;

        var result = item.AdjustStock(-1, null!, Guid.CreateVersion7());

        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be(InventoryErrors.AdjustmentReasonRequired.Code);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void ReserveStock_WithInvalidQuantity_ShouldFail(int invalidQuantity)
    {
        var item = InventoryItem.Create(Guid.CreateVersion7(), "Pad", 50m, 10, 2).Value;

        var result = item.ReserveStock(invalidQuantity, Guid.CreateVersion7(), Guid.CreateVersion7());

        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be(InventoryErrors.QuantityInvalid.Code);
    }

    [Fact]
    public void ReserveStock_ShouldAddTransactionToCollection()
    {
        var item = InventoryItem.Create(Guid.CreateVersion7(), "Pad", 50m, 10, 2).Value;

        item.ReserveStock(3, Guid.CreateVersion7(), Guid.CreateVersion7());

        item.Transactions.Should().ContainSingle(t => t.Type == InventoryTransactionType.Reserved && t.Quantity == 3);
    }
}
