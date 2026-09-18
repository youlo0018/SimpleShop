namespace CustomerService.Domain.Enums;

/// <summary>客户注册来源：用于区分不同渠道的账号（运营分析与风控）。</summary>
public enum RegisterSource
{
    /// <summary>微信小程序注册（默认）。</summary>
    MiniApp = 1,

    /// <summary>H5/网页注册。</summary>
    Web = 2,

    /// <summary>后台代建（预留）。</summary>
    Admin = 3
}
