﻿// -----------------------------------------------------------------------
//  <copyright file="RavenDB_1762.cs" company="Hibernating Rhinos LTD">
//      Copyright (c) Hibernating Rhinos LTD. All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------
using System;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using Raven.Abstractions.Data;
using Raven.Bundles.Replication.Plugins;
using Raven.Client;
using Raven.Client.Exceptions;
using Raven.Database.Config;
using Raven.Json.Linq;
using Raven.Tests.Bundles.Replication;
using Raven.Tests.Linq;
using Xunit;

namespace Raven.Tests.Issues
{
    public class RavenDB_1762 : ReplicationBase
    {
        [InheritedExport(typeof(AbstractDocumentReplicationConflictResolver))]
        public class DeleteOnConflict : AbstractDocumentReplicationConflictResolver
        {
            public static bool Enabled { get; set; }

            public void Enable()
            {
                Enabled = true;
            }

            public void Disable()
            {
                Enabled = false;
            }

            public override bool TryResolve(string id, Raven.Json.Linq.RavenJObject metadata, Raven.Json.Linq.RavenJObject document, Raven.Abstractions.Data.JsonDocument existingDoc, Func<string, Raven.Abstractions.Data.JsonDocument> getDocument)
            {
                if (Enabled)
                {
                    metadata.Add("Raven-Remove-Document-Marker", true);
                }
                return Enabled;
            }
        }

        [InheritedExport(typeof(AbstractDocumentReplicationConflictResolver))]
        public class PutOnConflict : AbstractDocumentReplicationConflictResolver
        {
            public static bool Enabled { get; set; }

            public void Enable()
            {
                Enabled = true;
            }

            public void Disable()
            {
                Enabled = false;
            }

            private static void ReplaceValues(RavenJObject target, RavenJObject source)
            {
                string[] targetKeys = target.Keys.ToArray();

                foreach (string key in targetKeys)
                    target.Remove(key);

                foreach (string key in source.Keys)
                    target.Add(key, source[key]);
            }

            public override bool TryResolve(string id, Raven.Json.Linq.RavenJObject metadata, Raven.Json.Linq.RavenJObject document, Raven.Abstractions.Data.JsonDocument existingDoc, Func<string, Raven.Abstractions.Data.JsonDocument> getDocument)
            {
                if (Enabled)
                {
                    if (metadata.ContainsKey(Constants.RavenDeleteMarker))
                    {
                        ReplaceValues(document, existingDoc.DataAsJson);
                        ReplaceValues(document, existingDoc.Metadata);
                    }
                }

                return Enabled;
            }
        }

        protected override void ConfigureServer(RavenConfiguration serverConfiguration)
        {
            serverConfiguration.Catalog.Catalogs.Add(new TypeCatalog(typeof(DeleteOnConflict), typeof(PutOnConflict)));
        }

        const string docId = "users/1";

        private void CanResolveConflict<T>(T resolver, Action<T> alterResolver, Action<IDocumentStore, IDocumentStore> testCallback)
        {
            var store1 = CreateStore();
            var store2 = CreateStore();

            using (var session = store1.OpenSession())
            {
                session.Store(new User
                {
                    Name = "Oren"
                }, docId);
                session.SaveChanges();
            }

            // master - master
            SetupReplication(store1.DatabaseCommands, store2.Url);

            WaitForReplication(store2, docId);

            DeleteOnConflict.Enabled = false;
            PutOnConflict.Enabled = false;
            // get reference to custom conflict resolver
            Assert.NotNull(resolver);
            if (alterResolver != null)
            {
                alterResolver(resolver);
            }

            testCallback(store1, store2);
        }

        [Fact]
        public void CanResolveConflictBetweenPutAndDelete_IncomingDelete()
        {
            CanResolveConflict(new DeleteOnConflict(), resolver => resolver.Enable() , delegate(IDocumentStore store1, IDocumentStore store2)
            {
                using (var s = store2.OpenSession())
                {
                    var user = s.Load<User>(docId);
                    user.Active = true;
                    s.Store(user);
                    s.SaveChanges();
                }

                using (var s = store1.OpenSession())
                {
                    var user = s.Load<User>(docId);
                    s.Delete(user);
                    s.SaveChanges();
                }

                WaitForReplication(store2, (session) => session.Load<User>(docId) == null);
                Assert.Null(store2.DatabaseCommands.Get(docId));
            }
            );
        }

        [Fact]
        public void CanResolveConflictBetweenPutAndDelete_IncomingPut_DeleteWins()
        {
            CanResolveConflict(new DeleteOnConflict(), resolver => resolver.Enable(), delegate(IDocumentStore store1, IDocumentStore store2)
            {

                using (var s = store2.OpenSession())
                {
                    var user = s.Load<User>(docId);
                    s.Delete(user);
                    s.SaveChanges();
                }
                using (var s = store1.OpenSession())
                {
                    var user = s.Load<User>(docId);
                    user.Active = true;
                    s.Store(user);
                    s.SaveChanges();
                }

                WaitForReplication(store2, (session) => session.Load<User>(docId) == null);
                Assert.Null(store2.DatabaseCommands.Get(docId));
            }
            );
        }

        [Fact]
        public void CanResolveConflictBetweenPutAndDelete_IncomingPut_PutWins()
        {
            CanResolveConflict(new PutOnConflict(), resolver => resolver.Enable(), delegate(IDocumentStore store1, IDocumentStore store2)
            {

                using (var s = store2.OpenSession())
                {
                    var user = s.Load<User>(docId);
                    s.Delete(user);
                    s.SaveChanges();
                }
                using (var s = store1.OpenSession())
                {
                    var user = s.Load<User>(docId);
                    user.Active = true;
                    s.Store(user);
                    s.SaveChanges();
                }

                WaitForReplication(store2, (session) => session.Load<User>(docId) != null);

                using (var s = store2.OpenSession())
                {
                    var user = s.Load<User>(docId);
                    Assert.NotNull(user);
                    Assert.True(user.Active);
                }
            }
            );
        }

        [Fact]
        public void CanResolveConflictBetweenPutAndDelete_IncomingDelete_Resolver_disabled()
        {
            CanResolveConflict(new DeleteOnConflict(), resolver => resolver.Disable(), delegate(IDocumentStore store1, IDocumentStore store2)
            {
                using (var s = store2.OpenSession())
                {
                    var user = s.Load<User>(docId);
                    user.Active = true;
                    s.Store(user);
                    s.SaveChanges();
                }

                using (var s = store1.OpenSession())
                {
                    var user = s.Load<User>(docId);
                    s.Delete(user);
                    s.SaveChanges();
                }

                Assert.Throws<ConflictException>(
                   () => WaitForReplication(store2, (session) => session.Load<User>(docId) == null));
            }
            );
        }

        [Fact]
        public void CanResolveConflictBetweenPutAndDelete_IncomingPut_Resolver_disabled()
        {
            CanResolveConflict(new DeleteOnConflict(), resolver => resolver.Disable(), delegate(IDocumentStore store1, IDocumentStore store2)
            {

                using (var s = store2.OpenSession())
                {
                    var user = s.Load<User>(docId);
                    s.Delete(user);
                    s.SaveChanges();
                }
                using (var s = store1.OpenSession())
                {
                    var user = s.Load<User>(docId);
                    user.Active = true;
                    s.Store(user);
                    s.SaveChanges();
                }

                Assert.Throws<ConflictException>(
                   () => WaitForReplication(store2, (session) => session.Load<User>(docId) != null));
            }
            );
        }
    }
}