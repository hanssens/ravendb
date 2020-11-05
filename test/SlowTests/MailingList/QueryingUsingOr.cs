﻿// -----------------------------------------------------------------------
//  <copyright file="QueryingUsingOr.cs" company="Hibernating Rhinos LTD">
//      Copyright (c) Hibernating Rhinos LTD. All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq;
using FastTests;
using Raven.Client.Documents.Linq;
using Xunit;
using Xunit.Abstractions;

namespace SlowTests.MailingList
{
    public class QueryingUsingOr : RavenTestBase
    {
        public QueryingUsingOr(ITestOutputHelper output) : base(output)
        {
        }

        private class Foo
        {
            public string Id { get; private set; }
            public DateTime? ExpirationTime { get; set; }

            public Foo()
            {
                Id = Guid.NewGuid().ToString();
                ExpirationTime = null;
            }
        }

        [Fact]
        public void ShouldWork()
        {
            using (var documentStore = GetDocumentStore())
            {
                documentStore.Initialize();

                using (var session = documentStore.OpenSession())
                {
                    session.Store(new Foo());
                    session.Store(new Foo());
                    session.SaveChanges();
                }


                using (var session = documentStore.OpenSession())
                {
                    var bar = session.Query<Foo>()
                        .Customize(x => x.WaitForNonStaleResults())
                        .Where(foo => foo.ExpirationTime == null || foo.ExpirationTime > DateTime.Now)
                        .ToList();

                    Assert.Equal(2, bar.Count);
                }
            }
        }
    }
}
