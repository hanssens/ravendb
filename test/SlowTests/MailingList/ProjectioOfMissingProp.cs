﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FastTests;
using Raven.Client.Documents.Commands.Batches;
using Raven.Client.Documents.Operations;
using Xunit;
using Xunit.Abstractions;

namespace SlowTests.MailingList
{
    public class ProjectioOfMissingProp : RavenTestBase
    {
        public ProjectioOfMissingProp(ITestOutputHelper output) : base(output)
        {
        }

        public class Item
        {
            public List<string> Tags;
        }

        [Fact]
        public void CanProjectArrayPropThatIsMissingInDoc()
        {
            using (var store = GetDocumentStore())
            {
                using (var s = store.OpenSession())
                {
                    s.Store(new Item
                    {
                    }, "items/1");
                    s.SaveChanges();
                }

                using (var s = store.OpenSession())
                {
                    s.Query<Item>()
                        .Select(x => x.Tags)
                        .ToList();
                }
            }
        }
    }
}
