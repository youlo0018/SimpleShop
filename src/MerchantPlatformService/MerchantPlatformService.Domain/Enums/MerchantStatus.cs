namespace MerchantPlatformService.Domain.Enums;

public enum MerchantStatus
{
    Draft = 0,
    PendingReview = 10,
    Approved = 20,
    Rejected = 30,
    Disabled = 40
}

public enum PlatformConfigType
{
    System = 0,
    Business = 1,
    Feature = 2
}
