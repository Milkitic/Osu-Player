using System.Collections.Generic;

namespace Milky.OsuPlayer.Data
{
    public class PaginationQueryResult<T> where T : class
    {
        public PaginationQueryResult(List<T> collection, int count)
        {
            Collection = collection;
            Count = count;
        }

        public List<T> Collection { get; set; }
        public int Count { get; set; }
    }
}