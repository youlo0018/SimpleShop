using System.ComponentModel;
using CommunalService.Domain.Entity;
using FreeSql.DataAnnotations;

namespace CustomerService.Domain.Entity;

[Index("uk_CustomeNo", "CustomeNo", true)]
[Index("uk_customer_platform_name", nameof(PlatformId) + "," + nameof(CustomerName), true)]
[Table(Name = "customer")]
public sealed class Customer : BaseEntity
{
    [Description("所属平台ID（不同平台同一登录名是独立账号）")]
    /// <summary>所属平台ID（不同平台同一登录名是独立账号）</summary>
    public long PlatformId { get; set; }

    [Column(StringLength = 24), Description("用户编号")]
    /// <summary>客户编号。</summary>
    public string CustomeNo { get; set; }

    [Column(StringLength = 64), Description("用户名")]
    /// <summary>客户登录名（平台内唯一）。</summary>
    public string CustomerName { get; set; }

    [Column(StringLength = 64), Description("邮箱")]
    /// <summary>邮箱。</summary>
    public string Email { get; set; }

    [Column(StringLength = 18), Description("手机号")]
    /// <summary>手机号。</summary>
    public string Phone { get; set; }
    
    [Column(StringLength = 255), Description("头像")]
    /// <summary>头像地址。</summary>
    public string Avatar { get; set; }

    [Description("性别")] public int Gender { get; set; }

    [Column(IsNullable = true), Description("生日")]
    /// <summary>生日</summary>
    public DateTime? Birth { get; set; } = null;

    [Description("是否注销")] public bool IsCancel { get; set; } = false;

    [Column(StringLength = 255), Description("密码")]
    /// <summary>口令散列（SHA256(password+salt)）。</summary>
    public string pwd { get; set; }

    [Description("Salt"), Column(StringLength = 50)]
    /// <summary>口令盐值（随机生成，随账号落库）。</summary>
    public string Salt { get; set; }

    [Description("是否同意所有用户协议")] public bool IsAllAgreeAgreement { get; set; } = false;
    [Description("注册来源")] public int RegisterSource { get; set; }
}
