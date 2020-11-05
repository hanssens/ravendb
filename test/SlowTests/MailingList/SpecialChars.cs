﻿using System.Linq;
using FastTests;
using Raven.Tests.Core.Utils.Entities;
using Xunit;
using Xunit.Abstractions;

namespace SlowTests.MailingList
{
    public class SpecialChars : RavenTestBase
    {
        public SpecialChars(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void ShouldWork()
        {
            using (var store = GetDocumentStore())
            {
                using (var session = store.OpenSession())
                {
                    session.Query<User>()
                        .Where(x => x.LastName == "abc&edf")
                        .ToList();
                }
            }
        }
    }
}
