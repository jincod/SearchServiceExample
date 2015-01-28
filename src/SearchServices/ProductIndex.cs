using System.Collections.Generic;

namespace SearchServices
{
    public class ProductIndex
    {
        public string Id { get; set; }
        public string CategoryId { get; set; }
        public Dictionary<string, object> Params { get; set; }
    }
}