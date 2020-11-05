﻿using FastTests;
using Raven.Client.Documents;
using Xunit;
using Xunit.Abstractions;

namespace SlowTests.MailingList
{
    public class JoelAsync : RavenTestBase
    {
        public JoelAsync(ITestOutputHelper output) : base(output)
        {
        }

        private class Dummy
        {
            public string Id { get; set; }
            public string Name { get; set; }
        }

        [Fact]
        public void AsyncQuery()
        {
            using (var store = GetDocumentStore())
            {
                using (var session = store.OpenAsyncSession())
                {
                    var results = session.Query<Dummy>().ToListAsync();
                    results.Wait();

                    var results2 = session.Query<Dummy>().ToListAsync();
                    results2.Wait();
                
                    Assert.Equal(0, results2.Result.Count);
                }
            }
        }
    }
}
