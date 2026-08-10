namespace CommunalService.Domain.Enums;

  
    // 这不是一个 enum，而是一个密封的类，模拟可扩展的枚举
public  record BaseApiResponseCode
{
    // 预定义静态实例（这就是你的“枚举值”）
    public static BaseApiResponseCode Success { get; } = new(200, "Success");
    public static BaseApiResponseCode BadRequest { get; } = new(400, "BadRequest");
    public static BaseApiResponseCode Unauthorized { get; } = new(401, "Unauthorized");
    public static BaseApiResponseCode Forbidden { get; } = new(403, "Forbidden");
    public static BaseApiResponseCode NotFound { get; } = new(404, "NotFound");
    public static BaseApiResponseCode InternalServerError { get; } = new(500, "InternalServerError");

    // 属性
    public int Value { get; }
    public string Name { get; }

    // 私有构造函数，防止外部实例化
    public BaseApiResponseCode(int value, string name)
    {
        Value = value;
        Name = name;
    }

  
}



