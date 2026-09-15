using MechanicShop.Domain.Common;
using MechanicShop.Domain.Common.Results;
using MechanicShop.Domain.Inventory.Events;

namespace MechanicShop.Domain.Inventory;

public sealed class InventoryItem : AuditableEntity, ISoftDelete
{
    public string Name { get; private set; } = null!;
    public decimal Cost { get; private set; }
    public int StockQuantity { get; private set; }
    public int ReorderLevel { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAtUtc { get; private set; }

    // Concurrency token for Optimistic Locking
    public byte[] RowVersion { get; private set; } = null!;

    private readonly List<InventoryTransaction> _transactions = [];
    public IReadOnlyCollection<InventoryTransaction> Transactions => _transactions.AsReadOnly();

    private InventoryItem() { }

    private InventoryItem(Guid id, string name, decimal cost, int stockQuantity, int reorderLevel)
        : base(id)
    {
        Name = name;
        Cost = cost;
        StockQuantity = stockQuantity;
        ReorderLevel = reorderLevel;
    }

    public static Result<InventoryItem> Create(Guid id, string? name, decimal cost, int stockQuantity, int reorderLevel)
    {
        if (id == Guid.Empty) return Error.Validation("Inventory.IdRequired", "Id is required.");
        if (string.IsNullOrWhiteSpace(name)) return InventoryErrors.NameRequired;
        if (cost <= 0 || cost > 100_000) return InventoryErrors.CostInvalid;
        if (stockQuantity < 0) return InventoryErrors.StockInvalid;
        if (reorderLevel < 0) return InventoryErrors.ReorderLevelInvalid;

        return new InventoryItem(id, name.Trim(), cost, stockQuantity, reorderLevel);
    }

    public Result<Updated> ReserveStock(int quantity, Guid? workOrderId = null, Guid? workOrderTaskId = null)
    {
        if (quantity <= 0) return InventoryErrors.QuantityInvalid;
        if (StockQuantity < quantity) return InventoryErrors.InsufficientStock(quantity, StockQuantity);

        StockQuantity -= quantity;

        var tx = new InventoryTransaction(Id, InventoryTransactionType.Reserved, quantity, workOrderId, workOrderTaskId);
        _transactions.Add(tx);

        AddDomainEvent(new InventoryItemStockReservedDomainEvent(Id, quantity, workOrderId, workOrderTaskId, StockQuantity));

        if (StockQuantity <= ReorderLevel)
        {
            AddDomainEvent(new InventoryItemLowStockDomainEvent(Id, StockQuantity, ReorderLevel));
        }

        return Result.Updated;
    }

    public Result<Updated> ReleaseStock(int quantity, Guid? workOrderId = null, Guid? workOrderTaskId = null)
    {
        if (quantity <= 0) return InventoryErrors.QuantityInvalid;

        StockQuantity += quantity;

        var tx = new InventoryTransaction(Id, InventoryTransactionType.Released, quantity, workOrderId, workOrderTaskId);
        _transactions.Add(tx);

        AddDomainEvent(new InventoryItemStockReleasedDomainEvent(Id, quantity, workOrderId, workOrderTaskId, StockQuantity));

        return Result.Updated;
    }

    public Result<Updated> Restock(int quantity)
    {
        if (quantity <= 0) return InventoryErrors.QuantityInvalid;

        StockQuantity += quantity;

        var tx = new InventoryTransaction(Id, InventoryTransactionType.Restocked, quantity);
        _transactions.Add(tx);

        AddDomainEvent(new InventoryItemRestockedDomainEvent(Id, quantity, StockQuantity));

        return Result.Updated;
    }

    public Result<Updated> AdjustStock(int quantity, string reason, Guid? performedBy = null)
    {
        if (quantity == 0) return InventoryErrors.InvalidAdjustment;
        if (string.IsNullOrWhiteSpace(reason)) return InventoryErrors.AdjustmentReasonRequired;
        if (StockQuantity + quantity < 0) return InventoryErrors.InvalidAdjustment;

        StockQuantity += quantity;

        var tx = new InventoryTransaction(Id, InventoryTransactionType.Adjustment, quantity, null, null, performedBy, reason.Trim());
        _transactions.Add(tx);

        AddDomainEvent(new InventoryItemStockAdjustedDomainEvent(Id, quantity, StockQuantity, performedBy, reason.Trim()));
        if (StockQuantity <= ReorderLevel && quantity < 0)
        {
            AddDomainEvent(new InventoryItemLowStockDomainEvent(Id, StockQuantity, ReorderLevel));
        }

        return Result.Updated;
    }

    public Result<Updated> UpdateDetails(string? name, decimal cost, int reorderLevel)
    {
        if (string.IsNullOrWhiteSpace(name)) return InventoryErrors.NameRequired;
        if (cost <= 0 || cost > 100_000) return InventoryErrors.CostInvalid;
        if (reorderLevel < 0) return InventoryErrors.ReorderLevelInvalid;

        Name = name.Trim();
        Cost = cost;
        ReorderLevel = reorderLevel;

        return Result.Updated;
    }

    public Result<Updated> Delete(TimeProvider? timeProvider = null)
    {
        if (IsDeleted) return InventoryErrors.AlreadyDeleted;

        IsDeleted = true;
        DeletedAtUtc = timeProvider?.GetUtcNow() ?? DateTimeOffset.UtcNow;

        return Result.Updated;
    }

    public Result<Updated> Restore()
    {
        if (!IsDeleted) return Result.Updated;

        IsDeleted = false;
        DeletedAtUtc = null;

        return Result.Updated;
    }
}