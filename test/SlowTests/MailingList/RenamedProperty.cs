﻿using System.Linq;
using FastTests;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace SlowTests.MailingList
{
    public class RenamedProperty : RavenTestBase
    {
        public RenamedProperty(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void OrderByWithAttributeShouldStillWork()
        {
            using (var store = GetDocumentStore())
            {
                const int count = 1000;

                using (var session = store.OpenSession())
                {
                    for (var i = 0; i < count; i++)
                    {
                        var model = new MyClass
                        {
                            ThisWontWork = i,
                            ThisWillWork = i
                        };
                        session.Store(model);
                    }
                    session.SaveChanges();
                }

                using (var session = store.OpenSession())
                {
                    var orderedWithoutAttribute = session.Query<MyClass>().OrderBy(x => x.ThisWillWork).Take(count).ToList();
                    var orderedWithAttribute = session.Query<MyClass>().OrderByDescending(x => x.ThisWontWork).Take(count).ToList();

                    Assert.Equal(count, orderedWithoutAttribute.Count);
                    Assert.Equal(count, orderedWithAttribute.Count);

                    for (var i = 1; i <= count; i++)
                    {
                        Assert.Equal(orderedWithoutAttribute[i - 1].ThisWontWork, orderedWithAttribute[count - i].ThisWontWork);
                    }
                }
            }
        }

        private class MyClass
        {
            [JsonProperty("whoops")]
            public long ThisWontWork { get; set; }

            public long ThisWillWork { get; set; }
        }
    }
}
