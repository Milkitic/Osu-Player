using System.Collections.Generic;

namespace Milky.OsuPlayer.Shared.Models
{
    public sealed class PaginationQueryResult<T>
    {
        public PaginationQueryResult(IReadOnlyList<T> results, int totalCount)
        {
            Results = results ?? [];
            TotalCount = totalCount < 0 ? 0 : totalCount;
        }

        public IReadOnlyList<T> Results { get; }

        public int TotalCount { get; }
    }
}