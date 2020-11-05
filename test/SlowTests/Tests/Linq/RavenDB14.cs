﻿using System.Collections.Generic;
using System.Linq;
using FastTests;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;
using Xunit;
using Xunit.Abstractions;

namespace SlowTests.Tests.Linq
{
    public class RavenDB14 : RavenTestBase
    {
        public RavenDB14(ITestOutputHelper output) : base(output)
        {
        }

        private class User
        {
            public string Name { get; set; }

            public bool Active { get; set; }
        }

        [Fact]
        public void WhereThenFirstHasAND()
        {
            var queries = new List<string>();

            using (IDocumentStore store = GetDocumentStore())
            {
                void RecordQueries(object sender, BeforeQueryEventArgs args)
                {
                    queries.Add(args.QueryCustomization.ToString());
                    store.OnBeforeQuery -= RecordQueries;
                }

                store.OnBeforeQuery += RecordQueries;
                var documentSession = store.OpenSession();

                var _ = documentSession.Query<User>().Where(x => x.Name == "ayende").FirstOrDefault(x => x.Active);

                Assert.Equal(1, queries.Count);
                Assert.Equal("from 'Users' where Name = $p0 and Active = $p1 limit $p2, $p3", queries[0]);
            }
        }

        [Fact]
        public void WhereThenSingleHasAND()
        {
            var queries = new List<string>();

            using (IDocumentStore store = GetDocumentStore())
            {
                void RecordQueries(object sender, BeforeQueryEventArgs args)
                {
                    queries.Add(args.QueryCustomization.ToString());
                    store.OnBeforeQuery -= RecordQueries;
                }

                store.OnBeforeQuery += RecordQueries;
                var documentSession = store.OpenSession();

                var _ = documentSession.Query<User>().Where(x => x.Name == "ayende").SingleOrDefault(x => x.Active);

                Assert.Equal(1, queries.Count);
                Assert.Equal("from 'Users' where Name = $p0 and Active = $p1 limit $p2, $p3", queries[0]);
            }
        }
    }
}
