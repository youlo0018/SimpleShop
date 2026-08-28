namespace OrderService.Domain.Entity;

public enum OrderState
{
    AwaitPayment = 10,
    Paid = 20,
    Shipped = 40,
    Completed = 50,
    Refunded = 60,
    Cancelled = 90,
    Closed = 91
}
