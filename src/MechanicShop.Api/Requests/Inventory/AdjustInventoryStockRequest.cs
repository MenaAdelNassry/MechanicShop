public record AdjustInventoryStockRequest
{
    public int QuantityAdjustment { get; set; } // +5 or -2 directly
    public string Reason { get; set; } = string.Empty;
}