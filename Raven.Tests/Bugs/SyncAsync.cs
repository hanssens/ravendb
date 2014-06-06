﻿// -----------------------------------------------------------------------
//  <copyright file="SyncAsync.cs" company="Hibernating Rhinos LTD">
//      Copyright (c) Hibernating Rhinos LTD. All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------
using System;
using Xunit;

namespace Raven.Tests.Bugs
{
    public class SyncAsync : Raven.Tests.Helpers.RavenTestBase
    {
        [Fact]
        public void Async()
        {
            var now = DateTime.UtcNow;
            using (var store = NewDocumentStore())
            {
                using (var session = store.OpenSession())
                using (var asyncSession = store.OpenAsyncSession())
                {
                    var query = session.Advanced.LuceneQuery<Foo>().WhereLessThanOrEqual(x => x.Expiry, now);
                    var asyncQuery = asyncSession.Advanced.AsyncLuceneQuery<Foo>().WhereLessThanOrEqual(x => x.Expiry, now);

                    Assert.Equal(query.GetIndexQuery(false).ToString(), asyncQuery.GetIndexQuery(false).ToString());
                }
            }
        }

        public class Foo
        {
            public int Id { get; set; }
            public DateTime Expiry { get; set; }
        }
    }
}