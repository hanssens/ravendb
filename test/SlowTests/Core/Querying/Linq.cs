﻿// -----------------------------------------------------------------------
//  <copyright file="Includes.cs" company="Hibernating Rhinos LTD">
//      Copyright (c) Hibernating Rhinos LTD. All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------

using System.Linq;
using Xunit.Abstractions;

using FastTests;

using Xunit;

using Company = SlowTests.Core.Utils.Entities.Company;
using User = SlowTests.Core.Utils.Entities.User;

namespace SlowTests.Core.Querying
{
    public class Linq : RavenTestBase
    {
        public Linq(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void CanQueryUsingLinq()
        {
            using (var store = GetDocumentStore())
            {
                using (var session = store.OpenSession())
                {
                    session.Store(new User { Name = "User1 name" });
                    session.Store(new User { Name = "User2 name" });
                    session.Store(new Company { Phone = 123 });
                    session.Store(new Company { Phone = 12 });
                    session.SaveChanges();

                    var users =
                        (
                            from user in session.Query<User>()
                            select user
                        ).ToArray();
                    Assert.Equal(2, users.Length);
                    Assert.Equal("User1 name", users[0].Name);
                    Assert.Equal("User2 name", users[1].Name);

                    var companies =
                        (
                            from company in session.Query<Company>()
                            where company.Phone > 12
                            select company
                        ).ToArray();
                    Assert.Equal(1, companies.Length);
                    Assert.Equal(123, companies[0].Phone);
                }
            }
        }
    }
}
