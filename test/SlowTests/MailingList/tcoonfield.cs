﻿using System.Linq;
using FastTests;
using Raven.Client.Documents.Indexes;
using Xunit;
using Xunit.Abstractions;

namespace SlowTests.MailingList
{
    public class tcoonfield : RavenTestBase
    {
        public tcoonfield(ITestOutputHelper output) : base(output)
        {
        }


        [Fact]
        public void ShouldUpdateIndexWhenProductNoLongerInIt()
        {
            //Arrange
            var product = new Product("MyName", ActiveStatus.Live);
            using (var store = GetDocumentStore())
            {
                new Product_AvailableForSale().Execute(store);

                using (var docSession = store.OpenSession())
                {
                    docSession.Store(product);
                    docSession.SaveChanges();

                    product.Status = ActiveStatus.NotLive.ToString();
                    docSession.Store(product);
                    docSession.SaveChanges();
                }

                // Act / Assert
                using (var docSession = store.OpenSession())
                {
                    var products = docSession.Advanced.DocumentQuery<Product, Product_AvailableForSale>()
                        .WaitForNonStaleResults()
                        .WhereLucene("Name", "MyName")
                        .ToList();

                    Assert.Empty(products);
                    //Worth noting that I also tried the regular query syntax and it failed as well.
                    //docSession.Query<Product>("Product/AvailableForSale").Count(p => p.Name == "MyName").Should().Be(0);
                }
            }
        }

        private enum ActiveStatus
        {
            Live, NotLive, Discontinued
        }

        private class Product
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Status { get; set; }

            public Product(string name, ActiveStatus status)
            {
                Name = name;
                Status = status.ToString();
            }
        }

        private class Product_AvailableForSale : AbstractIndexCreationTask<Product>
        {
            public Product_AvailableForSale()
            {
                Map = products => from p in products
                                  where p.Status != ActiveStatus.NotLive.ToString()
                                  select new
                                  {
                                      p.Status,
                                      p.Name
                                  };
            }
        }
    }
}
