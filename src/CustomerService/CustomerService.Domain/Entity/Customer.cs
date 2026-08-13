using System.ComponentModel;
using CommunalService.Domain.Entity;
using FreeSql.DataAnnotations;

namespace CustomerService.Domain.Entity;

[Index("uk_CustomeNo", "CustomeNo", true)]
[Table(Name = "customer")]
public sealed class Customer : BaseEntity
{
    [Column(StringLength = 24), Description("用户编号")]
    public string CustomeNo { get; set; }

    [Column(StringLength = 10), Description("用户名")]
    public string CustomerName { get; set; }

    [Column(StringLength = 64), Description("邮箱")]
    public string Email { get; set; }

    [Column(StringLength = 18), Description("手机号")]
    public string Phone { get; set; }

    [Description("性别")] public int Gender { get; set; }

    [Column(IsNullable = true), Description("生日")]
    public DateTime? Birth { get; set; } = null;

    [Description("是否注销")] public bool IsCancel { get; set; } = false;

    [Column(StringLength = 255), Description("密码")]
    public string pwd { get; set; }

    [Description("Salt"), Column(StringLength = 50)]
    public string Salt { get; set; }

    [Description("是否同意所有用户协议")] public bool IsAllAgreeAgreement { get; set; } = false;
    [Description("注册来源")] public int RegisterSource { get; set; }
}