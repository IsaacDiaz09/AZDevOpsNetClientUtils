namespace AzureDevopsUtils.Domain;

public sealed class AssignmentRelatedWork
{
    public int WorkItemId { get; init; }
    public DateTime FetchDataFromAzureDevOpsDate { get; } = DateTime.Now;
    public required string WorkItemType { get; init; }
    public required string AreaPath { get; init; }
    public required string TeamProject { get; init; }
    public required string IterationPath { get; init; }
    public required string State { get; init; }
    public required string Title { get; init; }
    public required AssignedTo AssignedTo { get; init; }
    public required IEnumerable<GitRepositoryWork> GitReposWork { get; init; }
}