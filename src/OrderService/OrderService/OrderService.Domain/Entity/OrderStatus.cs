namespace OrderService.Domain.Entity;

public enum OrderState
{
    AwaitPayment = 10,
    Paid = 20,
    Cancelled = 90,
    Closed = 91
}
