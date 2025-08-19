namespace AzureDevopsUtils.Domain;

public class GitRepositoryWork
{
    public required string RepositoryName { get; init; }
    public required IEnumerable<Commit> Commits { get; init; }
}