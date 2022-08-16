using System.Collections.Generic;

namespace Milki.OsuPlayer.Data;

public class PaginationQueryResult<T>
{
    public PaginationQueryResult(IReadOnlyList<T> results, int totalCount)
    {
        Results = results;
        TotalCount = totalCount;
    }

    public IReadOnlyList<T> Results { get; set; }
    public int TotalCount { get; set; }
}