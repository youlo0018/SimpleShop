namespace SimpleShop.Gateway;

public sealed class GrayReleaseOptions
{
    public const string SectionName = "GrayRelease";

    public string DefaultGroup { get; set; } = "stable";

    public string HeaderName { get; set; } = "X-Gray-Group";

    public List<string> StableUserIds { get; set; } = [];

    public List<string> CanaryUserIds { get; set; } = [];
}
