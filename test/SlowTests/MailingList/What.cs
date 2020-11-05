﻿// -----------------------------------------------------------------------
//  <copyright file="What.cs" company="Hibernating Rhinos LTD">
//      Copyright (c) Hibernating Rhinos LTD. All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------

using System.Linq;
using FastTests;
using Xunit;
using Xunit.Abstractions;

namespace SlowTests.MailingList
{
    public class What : RavenTestBase
    {
        public What(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void Y_U_No_Work()
        {
            using (var store = GetDocumentStore())
            {
                using (var raven = store.OpenSession())
                {
                    raven.Store(new User { Name = new PersonName("Slappy", "McFuzznutz"), });
                    raven.SaveChanges();
                }

                using (var raven = store.OpenSession())
                {
                    var results = raven.Query<User>()
                                       .Customize(x => x.WaitForNonStaleResults())
                                       .Select(u => new UserSearchModel
                                       {
                                           FirstName = u.Name.First,
                                           LastName = u.Name.Last,
                                           UserId = u.Id,
                                       })
                                       .ToList();

                    Assert.Equal(results[0].LastName, "McFuzznutz"); // "This succeeds"
                }

                using (var raven = store.OpenSession())
                {
                    var results = raven.Query<User>()
                                       .Customize(x => x.WaitForNonStaleResults())
                                       .OrderBy(u => u.Name.Last)
                                       .AsEnumerable()
                                       .Select(u => new UserSearchModel
                                       {
                                           FirstName = u.Name.First,
                                           LastName = u.Name.Last,
                                           UserId = u.Id,
                                       })
                                       .ToList();

                    Assert.Equal(results[0].LastName, "McFuzznutz"); // "This succeeds"
                }

                using (var raven = store.OpenSession())
                {
                    var results = raven.Query<User>()
                                       .Customize(x => x.WaitForNonStaleResults())
                                       .OrderBy(u => u.Name.Last)
                                       .Select(u => new UserSearchModel
                                       {
                                           FirstName = u.Name.First,
                                           LastName = u.Name.Last,
                                           UserId = u.Id,
                                       })
                                       .ToList();

                    Assert.Equal(results[0].LastName, "McFuzznutz"); // "This fails. Expected: McFuzznutz But was: null"
                }
            }
        }

        private class UserSearchModel
        {
            public string UserId { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
        }

        private class User
        {
            public string Id { get; set; }
            public PersonName Name { get; set; }
        }

        private class PersonName
        {
            public PersonName(string first, string last)
            {
                First = first;
                Last = last;
            }

            public string First { get; private set; }
            public string Last { get; private set; }
        }
    }
}
