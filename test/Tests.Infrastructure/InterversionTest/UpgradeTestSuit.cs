﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Raven.Client.Documents;
using Raven.Client.Documents.Operations.CompareExchange;
using Raven.Client.Documents.Session;
using Raven.Tests.Core.Utils.Entities;
using Xunit;

namespace Tests.Infrastructure.InterversionTest
{
    public abstract class UpgradeTestSuit
    {
        public static int ClusterDocumentNumber;

        protected readonly InterversionTestBase Infrastructure;

        protected UpgradeTestSuit(InterversionTestBase infrastructure)
        {
            Infrastructure = infrastructure;
        }

        public abstract Task TestClustering(List<DocumentStore> stores, string key);
        public abstract Task TestReplication(List<DocumentStore> stores);
    }

    public class Version41X : UpgradeTestSuit
    {
        public Version41X(InterversionTestBase infrastructure) : base(infrastructure)
        {
        }

        public override async Task TestClustering(List<DocumentStore> stores, string key)
        {
            using (var session = stores[0].OpenAsyncSession(new SessionOptions
            {
                TransactionMode = TransactionMode.ClusterWide
            }))
            {
                var id = $"cluster/document/{Interlocked.Increment(ref ClusterDocumentNumber)}";
                session.Advanced.RequestExecutor.DefaultTimeout = TimeSpan.FromSeconds(60);
                session.Advanced.ClusterTransaction.CreateCompareExchangeValue(key, "okay");
                var user = new User
                {
                    Name = "Karmel"
                };
                await session.StoreAsync(user, id);
                await session.SaveChangesAsync();

                Assert.True(await Infrastructure.WaitForDocumentInClusterAsync<User>(
                    id,
                    u => u.Name.Equals("Karmel"),
                    TimeSpan.FromSeconds(10),
                    stores,
                    stores[0].Database));
            }
        }

        public override async Task TestReplication(List<DocumentStore> stores)
        {
            using (var session = stores[0].OpenAsyncSession())
            {
                var user = new User
                {
                    Name = "aviv"
                };
                session.Advanced.RequestExecutor.DefaultTimeout = TimeSpan.FromSeconds(60);

                await session.StoreAsync(user, "users/");
                await session.SaveChangesAsync();

                Assert.True(await Infrastructure.WaitForDocumentInClusterAsync<User>(
                    user.Id,
                    u => u.Name.Equals("aviv"),
                    TimeSpan.FromSeconds(10),
                    stores,
                    stores[0].Database));
            }
        }
    }

    public class Version411 : UpgradeTestSuit
    {
        public Version411(InterversionTestBase infrastructure) : base(infrastructure)
        {
        }

        public override async Task TestClustering(List<DocumentStore> stores, string key)
        {
            using (var session = stores[0].OpenAsyncSession())
            {
                // test cluster operation for creating an index
                await session.Advanced.AsyncRawQuery<User>("from Users where Name = 'aviv'").ToListAsync();
            }
        }

        public override async Task TestReplication(List<DocumentStore> stores)
        {
            using (var session = stores[0].OpenAsyncSession())
            {
                var user = new User
                {
                    Name = "aviv"
                };
                session.Advanced.RequestExecutor.DefaultTimeout = TimeSpan.FromSeconds(60);

                await session.StoreAsync(user, "users/");
                await session.SaveChangesAsync();

                Assert.True(await Infrastructure.WaitForDocumentInClusterAsync<User>(
                    user.Id,
                    u => u.Name.Equals("aviv"),
                    TimeSpan.FromSeconds(10),
                    stores,
                    stores[0].Database));
            }
        }
    }

    public class Version40X : UpgradeTestSuit
    {
        public Version40X(InterversionTestBase infrastructure) : base(infrastructure)
        {
        }

        public override async Task TestClustering(List<DocumentStore> stores, string key)
        {
            var user = new User
            {
                Name = "Karmel"
            };
            await stores[0].Operations.SendAsync(new PutCompareExchangeValueOperation<User>(key, user, 0));
        }

        public override async Task TestReplication(List<DocumentStore> stores)
        {
            using (var session = stores[0].OpenAsyncSession())
            {
                var user = new User
                {
                    Name = "aviv"
                };
                session.Advanced.RequestExecutor.DefaultTimeout = TimeSpan.FromSeconds(60);

                await session.StoreAsync(user, "users/");
                await session.SaveChangesAsync();

                Assert.True(await Infrastructure.WaitForDocumentInClusterAsync<User>(
                    user.Id,
                    u => u.Name.Equals("aviv"),
                    TimeSpan.FromSeconds(10),
                    stores,
                    stores[0].Database));
            }
        }
    }
}
