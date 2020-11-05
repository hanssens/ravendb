﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastTests;
using FastTests.Server.Replication;
using Raven.Client.Documents.Operations;
using Raven.Client.Documents.Operations.Replication;
using Raven.Server.Config;
using SlowTests.Server.Replication;
using Tests.Infrastructure;
using Xunit.Abstractions;

namespace StressTests.Server.Replication
{
    public class ExternalReplicationStressTests_NoDispose : NoDisposalNoOutputNeeded
    {
        public ExternalReplicationStressTests_NoDispose(ITestOutputHelper output) : base(output)
        {
        }

        [Fact64Bit]
        public void ExternalReplicationShouldWorkWithSmallTimeoutStress()
        {
            for (int i = 0; i < 10; i++)
            {
                Parallel.For(0, 3, RavenTestHelper.DefaultParallelOptions, async _ =>
                {
                    using (var test = new ExternalReplicationTests(Output))
                    {
                        await test.ExternalReplicationShouldWorkWithSmallTimeoutStress(20000);
                    }
                });
            }
        }
    }
}
