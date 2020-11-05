﻿// -----------------------------------------------------------------------
//  <copyright file="NullableSorting.cs" company="Hibernating Rhinos LTD">
//      Copyright (c) Hibernating Rhinos LTD. All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------

using System.Linq;
using FastTests;
using Raven.Client.Documents.Indexes;
using Xunit;
using Xunit.Abstractions;

namespace SlowTests.MailingList
{
    public class NullableSorting : RavenTestBase
    {
        public NullableSorting(ITestOutputHelper output) : base(output)
        {
        }

        private class Blog
        {
            public string Id { get; set; }
            public decimal? Price { get; set; }
        }

        private class Blog_Search : AbstractIndexCreationTask<Blog, Blog>
        {
            public Blog_Search()
            {
                Map = blogs => from b in blogs
                               select new Blog
                               {
                                   Id = b.Id,
                                   Price = b.Price
                               };
                Index(b => b.Price, FieldIndexing.Default);
            }
        }

        [Fact]
        public void SortByNullableDecimal()
        {
            using (var store = GetDocumentStore())
            {
                using (var session = store.OpenSession())
                {
                    session.Store(new Blog { Price = 10.20m });
                    session.Store(new Blog { Price = 1.50m });
                    session.Store(new Blog { });  //with price set it works
                    session.Store(new Blog { Price = 4.20m });
                    session.SaveChanges();
                }

                new Blog_Search().Execute(store);

                using (var session = store.OpenSession())
                {
                    var result = session
                     .Advanced
                     .DocumentQuery<Blog, Blog_Search>()
                     .WaitForNonStaleResults()
                     .OrderBy(x => x.Price)
                     .ToArray();

                    var ids = result.Select(b => b.Id).ToArray();

                    Assert.Equal(new[] { "blogs/3-A", "blogs/2-A", "blogs/4-A", "blogs/1-A" }, ids);
                }
            }
        }
    }
}
