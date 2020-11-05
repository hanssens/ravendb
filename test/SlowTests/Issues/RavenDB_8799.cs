﻿using FastTests;
using Raven.Client.Documents.Subscriptions;
using Raven.Tests.Core.Utils.Entities;
using Xunit;
using Xunit.Abstractions;

namespace SlowTests.Issues
{
    public class RavenDB_8799 : RavenTestBase
    {
        public RavenDB_8799(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void CanDisableAndEnableSubscription()
        {
            using (var store = GetDocumentStore())
            {
                var name = store.Subscriptions.Create(new SubscriptionCreationOptions<Company>());
                var state = store.Subscriptions.GetSubscriptionState(name);

                Assert.False(state.Disabled);

                store.Subscriptions.Disable(name);

                state = store.Subscriptions.GetSubscriptionState(name);

                Assert.True(state.Disabled);

                store.Subscriptions.Enable(name);

                state = store.Subscriptions.GetSubscriptionState(name);

                Assert.False(state.Disabled);
            }
        }
    }
}
