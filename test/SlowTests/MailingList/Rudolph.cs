﻿using System.Linq;
using FastTests;
using Raven.Client.Documents.Linq;
using Xunit;
using Xunit.Abstractions;

namespace SlowTests.MailingList
{
    public class Rudolph : RavenTestBase
    {
        public Rudolph(ITestOutputHelper output) : base(output)
        {
        }

        private class User
        {
            public string Name { get; set; }
            public string Email { get; set; }

            public Login[] Logins { get; set; }
        }

        private class Login
        {
            public string OpenIdIdentifier { get; set; }
        }

        private class Identifier
        {
            protected bool Equals(Identifier other)
            {
                return string.Equals(str, other.str);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((Identifier)obj);
            }

            public override int GetHashCode()
            {
                return (str != null ? str.GetHashCode() : 0);
            }

            private string str;

            public static implicit operator string(Identifier identifier)
            {
                return identifier.str;
            }

            public static implicit operator Identifier(string identifier)
            {
                return new Identifier { str = identifier };
            }

            public static bool operator ==(Identifier id1, Identifier id2)
            {
                return ReferenceEquals(id1, id2);
            }

            public static bool operator !=(Identifier id1, Identifier id2)
            {
                return !ReferenceEquals(id2, id1);
            }
        }

        [Fact]
        public void CanUseImplicitIdentifier()
        {
            Identifier identifier = "http://openid";
            using (var store = GetDocumentStore())
            {
                using (var s = store.OpenSession())
                {
                    var user = new User()
                    {
                        Name = "Test",
                        Logins = new Login[] { new Login() { OpenIdIdentifier = identifier } }
                    };

                    s.Store(user);
                    s.SaveChanges();

                }
                using (var s = store.OpenSession())
                {
                    var query = from u in s.Query<User>().Customize(x => x.WaitForNonStaleResults())
                                where u.Logins.Any(x => x.OpenIdIdentifier == identifier)
                                select u;

                    Assert.NotNull(query.SingleOrDefault()); // will throw
                }
            }
        }
    }
}
