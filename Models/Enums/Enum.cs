namespace Online_Mobile_Recharge.Models.Enums
{
    public enum TransactionType
    {
        Prepaid,
        Postpaid
    }
    public enum TransactionStatus
    {
        Pending,
        Success,
        Failed
    }
    public enum PaymentMethod
    {
        Prepaid,
        Postpaid
    }
    public enum DndMode
    {
        AllowAll,
        BlockAll,
        Custom
    }
    public enum ProductType
    {
        Data,
        Card,
        CallerTune
    }

    public enum ProductSaleType
    {
        None,
        Percent,
        FixedAmount
    }
}
