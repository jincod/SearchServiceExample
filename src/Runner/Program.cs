using SearchServices;

namespace Runner
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var searchService = new SearchService();
            searchService.Reindex();
        }
    }
}