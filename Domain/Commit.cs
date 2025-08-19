using Microsoft.TeamFoundation.SourceControl.WebApi;

namespace AzureDevopsUtils.Domain;

public class Commit
{
    public required string CommitId { get; set; }
    public required string Comment { get; init; }
    public required IEnumerable<Change> Changes { get; init; }
    public required ChangeCountDictionary ChangeCounts { get; init; }
    public required DateTime Date { get; init; }

    public bool IsEmptyCommit
    {
        get
        {
            return ChangeCounts.Values.All(changeCount => changeCount == 0);
        }
    }
}