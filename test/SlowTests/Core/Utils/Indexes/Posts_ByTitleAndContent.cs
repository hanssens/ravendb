using System.Linq;
using Raven.Client.Documents.Indexes;
using Post = SlowTests.Core.Utils.Entities.Post;

namespace SlowTests.Core.Utils.Indexes
{
    public class Posts_ByTitleAndContent : AbstractIndexCreationTask<Post>
    {
        public Posts_ByTitleAndContent()
        {
            Map = posts => from post in posts
                           select new
                           {
                               post.Title,
                               post.Desc
                           };

            Stores.Add(x => x.Title, FieldStorage.Yes);
            Stores.Add(x => x.Desc, FieldStorage.Yes);

            Analyzers.Add(x => x.Title, typeof(Lucene.Net.Analysis.SimpleAnalyzer).FullName);
            Analyzers.Add(x => x.Desc, typeof(Lucene.Net.Analysis.SimpleAnalyzer).FullName);

        }
    }
}
