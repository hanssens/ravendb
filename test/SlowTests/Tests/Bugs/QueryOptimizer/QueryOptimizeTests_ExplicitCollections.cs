﻿using System.Linq;
using FastTests;
using Raven.Client.Documents.Indexes;
using Raven.Client.Documents.Operations.Indexes;
using Raven.Client.Documents.Queries;
using Xunit;
using Xunit.Abstractions;

namespace SlowTests.Tests.Bugs.QueryOptimizer
{
    public class QueryOptimizeTests_ExplicitCollections : RavenTestBase
    {
        public QueryOptimizeTests_ExplicitCollections(ITestOutputHelper output) : base(output)
        {
        }

        private class User
        {
            public string Id { get; set; }
            public string Email { get; set; }
            public string Name { get; set; }
            public int Age { get; set; }
        }

        [Fact]
        public void WillNotError()
        {
            using (var store = GetDocumentStore())
            {
                using (var session = store.OpenSession())
                {
                    var blogPosts = from post in session.Query<BlogPost>()
                                    where post.Tags.Any(tag => tag == "RavenDB")
                                    select post;

                    blogPosts.ToList();
                    session.Query<User>()
                        .Where(x => x.Email == "ayende@ayende.com")
                        .ToList();

                    session.Query<User>()
                        .OrderBy(x => x.Name)
                        .ToList();
                }
            }
        }

        [Fact]
        public void CanUseExistingDynamicIndex()
        {
            using (var store = GetDocumentStore())
            {
                using (var commands = store.Commands())
                {
                    var queryResult = commands.Query(
                        new IndexQuery()
                        {
                            Query = "FROM Users WHERE Name = 'Ayende' AND Age = 3"
                        });

                    Assert.Equal("Auto/Users/ByAgeAndName", queryResult.IndexName);

                    queryResult = commands.Query(
                        new IndexQuery()
                        {
                            Query = "FROM Users WHERE Name = 'Ayende'"
                        });

                    Assert.Equal("Auto/Users/ByAgeAndName", queryResult.IndexName);
                }
            }
        }

        [Fact]
        public void WillCreateWiderIndex()
        {
            using (var store = GetDocumentStore())
            {
                using (var commands = store.Commands())
                {
                    var queryResult = commands.Query(
                        new IndexQuery()
                        {
                            Query = "FROM Users WHERE Name = '3'"
                        });

                    Assert.Equal("Auto/Users/ByName", queryResult.IndexName);

                    queryResult = commands.Query(
                        new IndexQuery()
                        {
                            Query = "FROM Users WHERE Age = 3"
                        });

                    Assert.Equal("Auto/Users/ByAgeAndName", queryResult.IndexName);

                    queryResult = commands.Query(
                        new IndexQuery()
                        {
                            Query = "FROM Users WHERE Name = 'Ayende'"
                        });

                    Assert.Equal("Auto/Users/ByAgeAndName", queryResult.IndexName);
                }
            }
        }

        [Fact]
        public void WillCreateWiderIndex_UsingEnityName()
        {
            using (var store = GetDocumentStore())
            {
                using (var commands = store.Commands())
                {
                    var queryResult = commands.Query(
                        new IndexQuery()
                        {
                            Query = "FROM Users WHERE Name = '3'"
                        });

                    Assert.Equal("Auto/Users/ByName", queryResult.IndexName);

                    queryResult = commands.Query(
                        new IndexQuery()
                        {
                            Query = "FROM Users WHERE Age = 3"
                        });

                    Assert.Equal("Auto/Users/ByAgeAndName", queryResult.IndexName);

                    queryResult = commands.Query(
                        new IndexQuery()
                        {
                            Query = "FROM Users WHERE Name = 'Ayende'"
                        });

                    Assert.Equal("Auto/Users/ByAgeAndName", queryResult.IndexName);
                }
            }
        }
        [Fact]
        public void WillCreateWiderIndex_UsingDifferentEntityNames()
        {
            using (var store = GetDocumentStore())
            {
                using (var commands = store.Commands())
                {
                    var queryResult = commands.Query(
                        new IndexQuery()
                        {
                            Query = "FROM Users WHERE Name = '3'"
                        });

                    Assert.Equal("Auto/Users/ByName", queryResult.IndexName);

                    queryResult = commands.Query(
                        new IndexQuery()
                        {
                            Query = "FROM Cars WHERE Age = 3"
                        });

                    Assert.Equal("Auto/Cars/ByAge", queryResult.IndexName);

                    queryResult = commands.Query(
                        new IndexQuery()
                        {
                            Query = "FROM Users WHERE Name = 'Ayende'"
                        });

                    Assert.Equal("Auto/Users/ByName", queryResult.IndexName);
                }
            }
        }

        [Fact]
        public void WillAlwaysUseSpecifiedIndex()
        {
            using (var store = GetDocumentStore())
            {
                store.Maintenance.Send(new PutIndexesOperation(new[] {
                                                new IndexDefinition
                                                {
                                                    Name = "test",
                                                    Maps = { "from doc in docs.Users select new { doc.Name, doc.Age }" }
                                                }}));


                store.Maintenance.Send(new PutIndexesOperation(new[] {
                                                new IndexDefinition
                                                {
                                                    Name = "test2",
                                                    Maps = { "from doc in docs.Users select new { doc.Name }" }
                                                }}));

                using (var commands = store.Commands())
                {
                    var queryResult = commands.Query(
                        new IndexQuery()
                        {
                            Query = "FROM INDEX 'test2' WHERE Name = 'Ayende'"
                        });

                    Assert.Equal("test2", queryResult.IndexName);
                }
            }
        }

        private class SomeObject
        {
            public string StringField { get; set; }
            public int IntField { get; set; }
        }

        private class BlogPost
        {
            public string[] Tags { get; set; }
            public string Title { get; set; }
            public string BodyText { get; set; }
        }
    }
}
