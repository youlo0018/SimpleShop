using CommunalService.Domain;

namespace CustomerService.Domin.Enums;

public sealed  record ApiResponseCode:BaseApiResponseCode
{
    public ApiResponseCode(int value, string name) : base(value, name)
    {
    }

    public static BaseApiResponseCode Success1 { get; } = new(201, "Success");
   
}