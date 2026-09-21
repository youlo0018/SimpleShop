using Microsoft.Extensions.Options;

namespace SimpleShop.Gateway;

public sealed class GrayReleaseContextMiddleware(
    RequestDelegate next,
    IOptionsMonitor<GrayReleaseOptions> optionsAccessor,
    ILogger<GrayReleaseContextMiddleware> logger)
{
    private const string UserIdHeader = "X-User-Id";
    private const string RequestIdHeader = "X-Request-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        var options = optionsAccessor.CurrentValue;
        var requestedGroup = context.Request.Headers[options.HeaderName].ToString();
        var userId = context.Request.Headers[UserIdHeader].ToString();
        var grayGroup = ResolveGroup(options, requestedGroup, userId);
        var requestId = context.Request.Headers[RequestIdHeader].ToString();

        if (string.IsNullOrWhiteSpace(requestId))
        {
            requestId = Guid.NewGuid().ToString("N");
            context.Request.Headers[RequestIdHeader] = requestId;
        }

        context.Request.Headers[options.HeaderName] = grayGroup;
        context.Items["SimpleShop.GrayGroup"] = grayGroup;
        context.Response.Headers[options.HeaderName] = grayGroup;
        context.Response.Headers[RequestIdHeader] = requestId;

        logger.LogInformation(
            "Gray release routed user {UserId} to {GrayGroup} by ruleset {Ruleset}.",
            userId,
            grayGroup,
            options.DefaultGroup);

        await next(context);
    }

    private static string ResolveGroup(
        GrayReleaseOptions options,
        string requestedGroup,
        string userId)
    {
        if (options.StableUserIds.Contains(userId, StringComparer.OrdinalIgnoreCase))
        {
            return "stable";
        }

        if (options.CanaryUserIds.Contains(userId, StringComparer.OrdinalIgnoreCase))
        {
            return "canary";
        }

        return requestedGroup is "stable" or "canary"
            ? requestedGroup
            : options.DefaultGroup;
    }
}
