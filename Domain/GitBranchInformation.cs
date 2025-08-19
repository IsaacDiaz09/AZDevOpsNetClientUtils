using System.Web;

namespace AzureDevopsUtils.Domain;

public class GitBranchInformation
{
    public required string Name { get; init; }
    public required Guid ProjectId { get; init; }
    public required Guid RepositoryId { get; init; }

    public static Guid ExtractProjectId(string url)
    {
        CheckUrl(url);
        var projectId = GetVstfsUrlParts(url)[0];
        return Guid.Parse(projectId);
    }

    public static Guid ExtractRepositoryId(string url)
    {
        CheckUrl(url);
        var repositoryId = GetVstfsUrlParts(url)[1];
        return Guid.Parse(repositoryId);
    }

    public static string ExtractBranchName(string url)
    {
        CheckUrl(url);
        var urlParts = GetVstfsUrlParts(url);
        return string.Join("/", urlParts[2..]).Remove(0, 2);
    }

    private static string[] GetVstfsUrlParts(string url)
    {
        return HttpUtility.UrlDecode(url).Replace("vstfs:///Git/Ref/", "").Split("/");
    }

    private static void CheckUrl(string url)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url, nameof(url));
        if (!url.StartsWith("vstfs:///Git/Ref"))
        {
            throw new ArgumentException($"Invalid url {url}");
        }
    }
}