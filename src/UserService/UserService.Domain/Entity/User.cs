using System.ComponentModel;
using CommunalService.Domain.Entity;
using FreeSql.DataAnnotations;

namespace UserService.Domain.Entity;

public sealed class User:BaseEntity
{
    [Column(StringLength = 64), Description("用户名")]
    /// <summary>登录名。</summary>
    public string UserName { get; set; }

    [Column(StringLength = 64), Description("邮箱")]
    /// <summary>邮箱。</summary>
    public string Email { get; set; }

    [Column(StringLength = 18), Description("手机号")]
    /// <summary>手机号。</summary>
    public string Phone { get; set; }
    
    [Column(StringLength = 255), Description("密码")]
    /// <summary>口令散列（SHA256(password+salt)）。</summary>
    public string pwd { get; set; }

    [Description("Salt"), Column(StringLength = 50)]
    /// <summary>口令盐值（随机生成，随账号落库）。</summary>
    public string Salt { get; set; }


    [Description("是否启用")] public bool IsEnabled { get; set; } = true;

    [Column(StringLength = 255), Description("头像")]
    /// <summary>头像</summary>
    public string Avatar { get; set; } = string.Empty;

    [Description("性别：0 未设置 / 1 男 / 2 女")] public int Gender { get; set; }

    [Column(IsNullable = true), Description("生日")] public DateTime? Birth { get; set; }
}
