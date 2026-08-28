using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using CommunalService.Domain;
using CommunalService.Domain.Enums;
using CommunalService.Domain.Contracts.Messages;
using CommunalService.Domain.Contracts.Services;
using CommunalService.Domain.Infrastructure.Consul;
using Grpc.Net.Client;
using MagicOnion.Client;
using FreeSql;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using UserService.Domain.Entity;

namespace UserService.Api.Controllers;

using Base = CommunalService.Domain.BaseController;

public sealed record LoginRequest(string UserName, string Password);
public sealed record RegisterRequest(string UserName, string Password, string Email, string Phone, string Role = "customer");
public sealed record CreateUserRequest(string UserName, string Password, string Email, string Phone, string Role, long PlatformId = 0, long MerchantId = 0);
public sealed record UpdateUserRequest(long Id, string UserName, string Email, string Phone, string Role, long PlatformId = 0, long MerchantId = 0, string? Password = null);
public sealed record UpdateUserStatusRequest(long Id, bool IsEnabled);
public sealed record PagedUserQuery(string Keyword = "", int Page = 1, int PageSize = 10, string Role = "");
public sealed record SaveAddressRequest(long Id, long UserId, string ReceiverName, string ReceiverPhone, string Province,
    string City, string District, string Detail, bool IsDefault);
public sealed record DeleteByIdRequest(long Id);
public sealed record FavoriteRequest(long UserId, long ProductId);

/// <summary>
/// 当前服务同时支撑商城 App 用户和运营后台账号；演示网关未接入统一鉴权前，这里签发短令牌。
/// </summary>
public class UserController(IFreeSql freeSql, IConfiguration configuration, IServiceDiscovery serviceDiscovery, TenantContext tenant) : Base
{
    private const string DefaultTokenSecret = "SimpleShop.Dev.Token.Secret.2026";
    private static readonly Regex PhoneRegex = new("^1[3-9]\\d{9}$", RegexOptions.Compiled);
    private static readonly Regex EmailRegex = new("^[^\\s@]+@[^\\s@]+\\.[^\\s@]{2,}$", RegexOptions.Compiled);

    [HttpGet]
    public IActionResult SyncStructure()
    {
        freeSql.CodeFirst.SyncStructure<User>();
        freeSql.CodeFirst.SyncStructure<Address>();
        freeSql.CodeFirst.SyncStructure<Favorite>();
        return new OkResult();
    }

    [HttpGet]
    public async Task<ApiResponse> Addresses([FromQuery] long userId)
    {
        if (!tenant.HasWildcard && !tenant.IsCustomer) return Error(BaseApiResponseCode.Forbidden, "无权查询用户资料");
        var scopedUserId = tenant.HasWildcard ? userId : tenant.UserId;
        var items = await freeSql.Select<Address>()
            .Where(item => item.UserId == scopedUserId && !item.IsDeleted)
            .OrderByDescending(item => item.IsDefault)
            .OrderByDescending(item => item.CreatedAt)
            .ToListAsync();
        return Ok(items);
    }

    [HttpPost]
    public async Task<ApiResponse> SaveAddress([FromBody] SaveAddressRequest request)
    {
        if (!tenant.HasWildcard && !tenant.IsCustomer) return Error(BaseApiResponseCode.Forbidden, "无权管理用户资料");
        var scopedUserId = tenant.HasWildcard ? request.UserId : tenant.UserId;
        if (scopedUserId <= 0) return Error(BaseApiResponseCode.Unauthorized, "请先登录");

        Address address;
        if (request.Id > 0)
        {
            address = await freeSql.Select<Address>()
                .Where(item => item.Id == request.Id && item.UserId == scopedUserId && !item.IsDeleted)
                .FirstAsync();
            if (address is null)
                return Error(BaseApiResponseCode.NotFound, "地址不存在");
        }
        else
        {
            address = new Address { UserId = scopedUserId };
        }

        if (string.IsNullOrWhiteSpace(request.ReceiverName) || string.IsNullOrWhiteSpace(request.Detail))
            return Error(BaseApiResponseCode.BadRequest, "请完善收货人和详细地址");

        address.ReceiverName = request.ReceiverName;
        address.ReceiverPhone = request.ReceiverPhone;
        address.Province = request.Province;
        address.City = request.City;
        address.District = request.District;
        address.Detail = request.Detail;
        address.IsDefault = request.IsDefault;

        if (address.Id == 0)
            await freeSql.Insert(address).ExecuteAffrowsAsync();
        else
            await freeSql.Update<Address>().SetSource(address).ExecuteAffrowsAsync();

        // 默认地址唯一化，避免下单页出现多个默认项。
        if (address.IsDefault)
        {
            await freeSql.Update<Address>()
                .Where(item => item.UserId == address.UserId && item.Id != address.Id && !item.IsDeleted)
                .Set(item => item.IsDefault, false)
                .ExecuteAffrowsAsync();
        }

        return Ok(address);
    }

    [HttpPost]
    public async Task<ApiResponse> DeleteAddress([FromBody] DeleteByIdRequest request)
    {
        if (!tenant.HasWildcard && !tenant.IsCustomer) return Error(BaseApiResponseCode.Forbidden, "无权管理用户资料");
        var selection = freeSql.Select<Address>()
            .Where(item => item.Id == request.Id && !item.IsDeleted);
        if (!tenant.HasWildcard) selection = selection.Where(item => item.UserId == tenant.UserId);
        var address = await selection.FirstAsync();
        if (address is null) return Error(BaseApiResponseCode.NotFound, "地址不存在");

        var updated = await freeSql.Update<Address>()
            .Where(item => item.Id == address.Id && !item.IsDeleted)
            .Set(item => item.IsDeleted, true)
            .ExecuteAffrowsAsync() > 0;
        return updated ? Ok(new { success = true }) : Error(BaseApiResponseCode.NotFound, "地址不存在");
    }

    [HttpGet]
    public async Task<ApiResponse> Favorites([FromQuery] long userId)
    {
        var scopedUserId = tenant.HasWildcard ? userId : tenant.UserId;
        var items = await freeSql.Select<Favorite>()
            .Where(item => item.UserId == scopedUserId && !item.IsDeleted)
            .OrderByDescending(item => item.CreatedAt)
            .ToListAsync();
        return Ok(items);
    }

    [HttpPost]
    public async Task<ApiResponse> ToggleFavorite([FromBody] FavoriteRequest request)
    {
        if (!tenant.HasWildcard && !tenant.IsCustomer) return Error(BaseApiResponseCode.Forbidden, "无权管理收藏");
        var scopedUserId = tenant.HasWildcard ? request.UserId : tenant.UserId;
        var favorite = await freeSql.Select<Favorite>()
            .Where(item => item.UserId == scopedUserId && item.ProductId == request.ProductId && !item.IsDeleted)
            .FirstAsync();
        if (favorite is null)
        {
            await freeSql.Insert(new Favorite { UserId = scopedUserId, ProductId = request.ProductId }).ExecuteAffrowsAsync();
            return Ok(new { favorited = true });
        }

        await freeSql.Update<Favorite>().Where(item => item.Id == favorite.Id)
            .Set(item => item.IsDeleted, true).ExecuteAffrowsAsync();
        return Ok(new { favorited = false });
    }

    [HttpPost]
    public async Task<ApiResponse> Register([FromBody] RegisterRequest request)
    {
        var user = await CreateUserAsync(request.UserName, request.Password, request.Email, request.Phone,
            request.Role == "admin" ? "customer" : request.Role);
        return user is null
            ? Error(BaseApiResponseCode.BadRequest, "用户名或手机号已存在")
            : Ok(CreateLoginResult(user, new AuthorizationResponse { Success = true, TenantType = "customer" }));
    }

    [HttpPost]
    public async Task<ApiResponse> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrWhiteSpace(request.Password))
            return Error(BaseApiResponseCode.BadRequest, "请输入用户名和密码");

        var user = await freeSql.Select<User>()
            .Where(item => item.UserName == request.UserName && !item.IsDeleted)
            .FirstAsync();
        if (user is null || !user.IsEnabled || Hash(request.Password, user.Salt) != user.pwd)
            return Error(BaseApiResponseCode.BadRequest, "用户名或密码错误");

        return Ok(CreateLoginResult(user, await ResolveAuthorizationAsync(user)));
    }

    [HttpGet]
    public async Task<ApiResponse> Profile([FromQuery] long id)
    {
        if (!tenant.HasWildcard && (tenant.UserId <= 0 || (!tenant.IsCustomer && !tenant.IsPlatform && !tenant.IsMerchant)))
            return Error(BaseApiResponseCode.Unauthorized, "请先登录");
        var scopedUserId = tenant.HasWildcard ? id : tenant.UserId;
        var user = await freeSql.Select<User>().Where(item => item.Id == scopedUserId && !item.IsDeleted).FirstAsync();
        return user is null ? Error(BaseApiResponseCode.NotFound, "用户不存在") : Ok(Shape(user));
    }

    [HttpGet]
    public async Task<ApiResponse> Users([FromQuery] PagedUserQuery query)
    {
        var selection = freeSql.Select<User>()
            .Where(item => !item.IsDeleted)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Keyword), item =>
                item.UserName.Contains(query.Keyword) || item.Phone.Contains(query.Keyword) || item.Email.Contains(query.Keyword))
            .WhereIf(!string.IsNullOrWhiteSpace(query.Role), item => item.Role == query.Role);

        var total = await selection.CountAsync();
        var items = await selection
            .OrderByDescending(item => item.CreatedAt)
            .Page((Math.Max(query.Page, 1) - 1) * query.PageSize, query.PageSize)
            .ToListAsync();
        return Ok(new { items = items.Select(user => Shape(user)), total, page = query.Page, pageSize = query.PageSize });
    }

    private static ApiResponse? ValidateUser(string userName, string password, string phone, string email, bool requirePassword)
    {
        if (string.IsNullOrWhiteSpace(userName) || userName.Length < 3 || userName.Length > 64)
            return new ApiResponse { Code = 400, Message = "用户名必须为3-64个字符", Data = new { errors = new { userName = new[] { "用户名必须为3-64个字符" } } } };
        if (requirePassword && (string.IsNullOrWhiteSpace(password) || password.Length < 8 || !password.Any(char.IsDigit) || !password.Any(char.IsLetter)))
            return new ApiResponse { Code = 400, Message = "密码至少8位且包含字母和数字", Data = new { errors = new { password = new[] { "密码至少8位且包含字母和数字" } } } };
        if (!string.IsNullOrWhiteSpace(password) && (password.Length < 8 || !password.Any(char.IsDigit) || !password.Any(char.IsLetter)))
            return new ApiResponse { Code = 400, Message = "密码至少8位且包含字母和数字", Data = new { errors = new { password = new[] { "密码至少8位且包含字母和数字" } } } };
        if (!string.IsNullOrWhiteSpace(phone) && !PhoneRegex.IsMatch(phone))
            return new ApiResponse { Code = 400, Message = "手机号格式不正确", Data = new { errors = new { phone = new[] { "手机号格式不正确" } } } };
        if (!string.IsNullOrWhiteSpace(email) && !EmailRegex.IsMatch(email))
            return new ApiResponse { Code = 400, Message = "邮箱格式不正确", Data = new { errors = new { email = new[] { "邮箱格式不正确" } } } };
        return null;
    }

    [HttpPost]
    public async Task<ApiResponse> Create([FromBody] CreateUserRequest request)
    {
        if (ValidateUser(request.UserName, request.Password, request.Phone, request.Email, true) is { } invalid)
            return invalid;
        var user = await CreateUserAsync(request.UserName, request.Password, request.Email, request.Phone,
            string.IsNullOrWhiteSpace(request.Role) ? "customer" : request.Role);
        if (user is null) return Error(BaseApiResponseCode.BadRequest, "用户名或手机号已存在");
        await AssignPermissionRoleAsync(user.Id, request.Role, request.PlatformId, request.MerchantId);
        return Ok(Shape(user));
    }

    [HttpPost]
    public async Task<ApiResponse> UpdateStatus([FromBody] UpdateUserStatusRequest request)
    {
        var updated = await freeSql.Update<User>()
            .Where(item => item.Id == request.Id && !item.IsDeleted)
            .Set(item => item.IsEnabled, request.IsEnabled)
            .Set(item => item.UpdatedAt, DateTime.Now)
            .ExecuteAffrowsAsync() > 0;
        return updated ? Ok(new { success = true }) : Error(BaseApiResponseCode.NotFound, "用户不存在");
    }

    [HttpPost]
    public async Task<ApiResponse> Update([FromBody] UpdateUserRequest request)
    {
        if (ValidateUser(request.UserName, request.Password ?? "", request.Phone, request.Email, false) is { } invalid)
            return invalid;
        var user = await freeSql.Select<User>().Where(item => item.Id == request.Id && !item.IsDeleted).FirstAsync();
        if (user is null) return Error(BaseApiResponseCode.NotFound, "用户不存在");

        var duplicate = await freeSql.Select<User>()
            .AnyAsync(item => item.Id != request.Id && !item.IsDeleted &&
                (item.UserName == request.UserName || (!string.IsNullOrWhiteSpace(request.Phone) && item.Phone == request.Phone)));
        if (duplicate) return Error(BaseApiResponseCode.BadRequest, "用户名或手机号已存在");

        user.UserName = request.UserName;
        user.Email = request.Email;
        user.Phone = request.Phone;
        user.Role = request.Role == "customer" ? "customer" : "admin";
        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            var salt = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
            user.Salt = salt;
            user.pwd = Hash(request.Password, salt);
        }

        await freeSql.Update<User>().SetSource(user).ExecuteAffrowsAsync();
        await AssignPermissionRoleAsync(user.Id, request.Role, request.PlatformId, request.MerchantId);
        return Ok(new { success = true });
    }

    private async Task AssignPermissionRoleAsync(long userId, string roleCode, long platformId, long merchantId)
    {
        if (string.IsNullOrWhiteSpace(roleCode) || roleCode is "admin" or "customer") return;
        var address = await serviceDiscovery.GetPollingAddressAsync("PermissionService", PollingAddressType.Grpc)
            ?? throw new InvalidOperationException("PermissionService 不可用");
        using var channel = GrpcChannel.ForAddress($"http://{address}");
        var client = MagicOnionClient.Create<IPermissionService>(channel);
        if (!await client.AssignRoleAsync(new AssignRoleRequest { UserId = userId, RoleCode = roleCode, PlatformId = platformId, MerchantId = merchantId }))
            throw new InvalidOperationException("角色绑定失败：角色不存在或租户范围不完整");
    }

    private async Task<User?> CreateUserAsync(string userName, string password, string email, string phone, string role)
    {
        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
            return null;

        var exists = await freeSql.Select<User>()
            .AnyAsync(item => !item.IsDeleted && (item.UserName == userName || (!string.IsNullOrWhiteSpace(phone) && item.Phone == phone)));
        if (exists)
            return null;

        var salt = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        var user = new User
        {
            UserName = userName.Trim(),
            Email = email ?? string.Empty,
            Phone = phone ?? string.Empty,
            pwd = Hash(password, salt),
            Salt = salt,
            Role = role == "admin" ? "admin" : "customer",
            IsAllAgreeAgreement = true,
            IsEnabled = true
        };
        return await freeSql.Insert(user).ExecuteAffrowsAsync() > 0 ? user : null;
    }

    private long ResolveUserId(long requestedUserId) => tenant.HasWildcard ? requestedUserId : tenant.UserId;

    private object CreateLoginResult(User user, AuthorizationResponse authorization)
    {
        return new { token = CreateToken(user, authorization), user = Shape(user, authorization) };
    }

    private static object Shape(User user, AuthorizationResponse? authorization = null)
    {
        return new
        {
            user.Id, user.UserName, user.Email, user.Phone, user.Role, user.Avatar, user.IsEnabled, user.CreatedAt,
            tenantType = authorization?.TenantType ?? "customer",
            platformId = authorization?.PlatformId.ToString() ?? "0",
            merchantId = authorization?.MerchantId.ToString() ?? "0",
            permissions = authorization?.Permissions ?? new List<string>(),
            roles = authorization?.Roles ?? new List<string>()
        };
    }

    private static string Hash(string password, string salt)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(password + salt)));
    }

    private string CreateToken(User user, AuthorizationResponse authorization)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("tenant_type", authorization.TenantType),
            new Claim("platform_id", authorization.PlatformId.ToString()),
            new Claim("merchant_id", authorization.MerchantId.ToString())
        };
        claims.AddRange(authorization.Permissions.Select(permission => new Claim("permission", permission)));
        claims.AddRange(authorization.Roles.Select(role => new Claim("role", role)));
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Auth:TokenSecret"] ?? DefaultTokenSecret));
        var token = new JwtSecurityToken(
            issuer: "SimpleShop.UserService",
            audience: "SimpleShop",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(12),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<AuthorizationResponse> ResolveAuthorizationAsync(User user)
    {
        var address = await serviceDiscovery.GetPollingAddressAsync("PermissionService", PollingAddressType.Grpc)
            ?? throw new InvalidOperationException("PermissionService 不可用");
        using var channel = GrpcChannel.ForAddress($"http://{address}");
        var client = MagicOnionClient.Create<IPermissionService>(channel);
        var authorization = await client.ResolveAsync(new AuthorizationRequest
        {
            UserId = user.Id, LegacyRole = user.Role, UserName = user.UserName
        });
        return authorization;
    }
}
