﻿using System;
using System.Threading.Tasks;
using FastTests;
using Raven.Client.Documents;
using Xunit;
using Xunit.Abstractions;

namespace SlowTests.Issues
{
    public class RavenDB10215 : RavenTestBase
    {
        public RavenDB10215(ITestOutputHelper output) : base(output)
        {
        }

        public class MyEntity
        {
            public string Name { get; set; }
            public string Description { get; set; }

            public string Id { get; set; }
        }

        [Fact]
        public void CanUseDocumentStoreBeforeStore()
        {
            using (var store = GetDocumentStore(options:
                new Options
                {
                    ModifyDocumentStore = s =>
                    {
                        var documentStore = (IDocumentStore)s;
                        documentStore.OnBeforeStore += (sender, eventArgs) =>
                        {
                            if (eventArgs.Entity is MyEntity entity)
                                entity.Description = DateTime.UtcNow.ToString();
                        };
                    }
                }))
            {
                using (var session = store.OpenSession())
                {
                    var entity = new MyEntity
                    {
                        Name = "Async session"
                    };

                    session.Store(entity);
                    session.SaveChanges();
                    Assert.NotNull(entity.Description);
                }
            }
        }
        [Fact]
        public async Task CanUseDocumentStoreBeforeStoreAsync()
        {
            using (var store = GetDocumentStore(options:
                new Options
                {
                    ModifyDocumentStore = s =>
                    {
                        var documentStore = (IDocumentStore)s;
                        documentStore.OnBeforeStore += (sender, eventArgs) =>
                        {
                            if (eventArgs.Entity is MyEntity entity)
                                entity.Description = DateTime.UtcNow.ToString();
                        };
                    }
                }))
            {
                using (var session = store.OpenAsyncSession())
                {
                    var entity = new MyEntity
                    {
                        Name = "Async session"
                    };

                    await session.StoreAsync(entity);
                    await session.SaveChangesAsync();
                    Assert.NotNull(entity.Description);
                }
            }
        }
    }
}
