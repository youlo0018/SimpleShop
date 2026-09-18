namespace CommunalService.Domain.Enums;

  
    // 这不是一个 enum，而是一个密封的类，模拟可扩展的枚举
public  record BaseApiResponseCode
{
    // 预定义静态实例（这就是你的“枚举值”）
    /// <summary>是否成功。</summary>
    /// <summary>成功（200）。</summary>
    public static BaseApiResponseCode Success { get; } = new(200, "Success");
    /// <summary>请求参数/业务校验失败（400）。</summary>
    public static BaseApiResponseCode BadRequest { get; } = new(400, "BadRequest");
    /// <summary>未登录或登录态失效（401）。</summary>
    public static BaseApiResponseCode Unauthorized { get; } = new(401, "Unauthorized");
    /// <summary>已登录但无权限（403）。</summary>
    public static BaseApiResponseCode Forbidden { get; } = new(403, "Forbidden");
    /// <summary>资源不存在（404）。</summary>
    public static BaseApiResponseCode NotFound { get; } = new(404, "NotFound");
    /// <summary>服务内部错误（500）。</summary>
    public static BaseApiResponseCode InternalServerError { get; } = new(500, "InternalServerError");

    // 属性
    /// <summary>业务码数值。</summary>
    public int Value { get; }
    /// <summary>名称。</summary>
    public string Name { get; }

    // 私有构造函数，防止外部实例化
    /// <summary>构造业务码（数值 + 名称）。</summary>
    public BaseApiResponseCode(int value, string name)
    {
        Value = value;
        Name = name;
    }

  
}



