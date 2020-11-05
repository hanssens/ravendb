﻿using Raven.Tests.Core.Utils.Entities;
using Xunit;
using Xunit.Abstractions;

namespace FastTests.Client
{
    public class Update : RavenTestBase
    {
        public Update(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void Update_Document()
        {
            using (var store = GetDocumentStore())
            {
                using (var newSession = store.OpenSession())
                {
                    newSession.Store(new User { Name = "User1", Age = 1 }, "users/1");
                    newSession.SaveChanges();
                    var user = newSession.Load<User>("users/1");
                    Assert.NotNull(user);
                    Assert.Equal(user.Age, 1);
                    user.Age = 2;
                    newSession.SaveChanges();
                    var newUser = newSession.Load<User>("users/1");
                    Assert.NotNull(newUser);
                    Assert.Equal(newUser.Age, 2);
                }
            }
        }
    }
}

