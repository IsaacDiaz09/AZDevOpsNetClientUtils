using Microsoft.TeamFoundation.SourceControl.WebApi;

namespace AzureDevopsUtils.Domain;

public class Change
{
    public required string FileName { get; init; }
    public required string? FilePath { get; init; }
    public required VersionControlChangeType Type { get; set; }
}