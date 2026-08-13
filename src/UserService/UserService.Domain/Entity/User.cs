using System.ComponentModel;
using CommunalService.Domain.Entity;
using FreeSql.DataAnnotations;

namespace UserService.Domain.Entity;

public sealed class User:BaseEntity
{
    [Column(StringLength = 10), Description("用户名")]
    public string UserName { get; set; }

    [Column(StringLength = 64), Description("邮箱")]
    public string Email { get; set; }

    [Column(StringLength = 18), Description("手机号")]
    public string Phone { get; set; }
    
    [Description("是否注销")] public bool IsCancel { get; set; } = false;

    [Column(StringLength = 255), Description("密码")]
    public string pwd { get; set; }

    [Description("Salt"), Column(StringLength = 50)]
    public string Salt { get; set; }

    [Description("是否同意所有用户协议")] public bool IsAllAgreeAgreement { get; set; } = false;

    [Description("是否启用")] public bool IsEnabled { get; set; } = true;
   
    [Description("操作员")] public long OperationId { get; set; }

}