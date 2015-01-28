using System.Collections.Generic;

namespace Domain
{
    public class Product
    {
        public string Id { get; set; }
        public string CategoryId { get; set; }
        public Dictionary<string, object> Params { get; set; }
    }
}