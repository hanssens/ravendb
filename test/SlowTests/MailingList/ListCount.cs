﻿using System;
using System.Collections.Generic;
using System.Linq;
using FastTests;
using Xunit;
using Xunit.Abstractions;

namespace SlowTests.MailingList
{
    public class ListCount : RavenTestBase
    {
        public ListCount(ITestOutputHelper output) : base(output)
        {
        }

        private class Location
        {
            public string Id { get; set; }

            public string Name { get; set; }

            public List<Guid> Properties { get; set; }
        }

        [Fact]
        public void CanGetCount()
        {
            using (var store = GetDocumentStore())
            {
                using (var session = store.OpenSession())
                {
                    session.Store(new Location
                    {
                        Properties = new List<Guid>
                        {
                            Guid.NewGuid(),
                            Guid.NewGuid()
                        },
                        Name = "Ayende"
                    });
                    session.SaveChanges();

                    var result = session.Query<Location>()
                        .Where(x => x.Name.StartsWith("ay"))
                        .Select(x => new
                        {
                            x.Name,
                            x.Properties.Count
                        }).ToList();

                    Assert.Equal("Ayende", result[0].Name);
                    Assert.Equal(2, result[0].Count);
                }
            }
        }
    }
}
