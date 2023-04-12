namespace ExtravaWallSetup;

[System.Diagnostics.DebuggerDisplay("{DebuggerDisplay,nq}")]
public class SystemInfoModel
{
    public string ID { get; set; } = null!;
    public string PrettyName { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string VersionId { get; set; } = null!;
    public string VersionCodeName { get; set; } = null!;
    public Uri HomeUrl { get; set; } = null!;
    public Uri SupportUrl { get; set; } = null!;
    public Uri BugReportUrl { get; set; } = null!;

    [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
    private string DebuggerDisplay => ToString() ?? string.Empty;
}
