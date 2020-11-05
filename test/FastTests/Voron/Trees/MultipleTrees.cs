﻿using System;
using Xunit;
using Xunit.Abstractions;

namespace FastTests.Voron.Trees
{
    public class MultipleTrees : StorageTest
    {
        public MultipleTrees(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void CanCreateNewTree()
        {
            using (var tx = Env.WriteTransaction())
            {
                tx.CreateTree("test");

                tx.CreateTree("test").Add("test", StreamFor("abc"));

                tx.Commit();
            }

            using (var tx = Env.ReadTransaction())
            {
                var stream = tx.ReadTree("test").Read("test");
                Assert.NotNull(stream);

                tx.Commit();
            }
        }

        [Fact]
        public void CanUpdateValuesInSubTree()
        {
            using (var tx = Env.WriteTransaction())
            {
                tx.CreateTree("test");

                tx.CreateTree("test").Add("test", StreamFor("abc"));

                tx.Commit();
            }

            using (var tx = Env.WriteTransaction())
            {

                tx.CreateTree("test").Add("test2", StreamFor("abc"));

                tx.Commit();
            }

            using (var tx = Env.ReadTransaction())
            {
                var stream = tx.CreateTree("test").Read("test2");
                Assert.NotNull(stream);

                tx.Commit();
            }
        }

        [Fact]
        public void CreatingTreeWithoutCommitingTransactionShouldYieldNoResults()
        {
            using (var tx = Env.WriteTransaction())
            {
                tx.CreateTree("test");
            }

            var e = Assert.Throws<InvalidOperationException>(() =>
                {
                    using (var tx = Env.ReadTransaction())
                    {
                        tx.CreateTree("test");
                    }
                });
            Assert.Contains("No such tree: 'test'", e.Message);
        }
    }
}
