﻿// -----------------------------------------------------------------------
//  <copyright file="RavenDB_1603.cs" company="Hibernating Rhinos LTD">
//      Copyright (c) Hibernating Rhinos LTD. All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------
using System;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Raven.Abstractions.Data;
using Raven.Abstractions.Smuggler;
using Raven.Client;
using Raven.Client.Document;
using Raven.Client.Embedded;
using Raven.Database;
using Raven.Database.Config;
using Raven.Database.Extensions;
using Raven.Database.Smuggler;
using Raven.Json.Linq;
using Raven.Smuggler;
using Raven.Tests.Triggers;
using Xunit;
using System.Linq;

namespace Raven.Tests.Issues
{
    public class RavenDB_1603 : RavenTest
    {
        public class User
        {
            public string Name { get; set; }
            public string Id { get; set; }
        }

        public class Developer
        {
            public string Name { get; set; }
            public string Id { get; set; }
        }

        protected override void ModifyConfiguration(InMemoryRavenConfiguration configuration)
        {
            configuration.Container = new CompositionContainer(new TypeCatalog(
                                                                   typeof(ReadTriggers.HiddenDocumentsTrigger)));
        }

        [Fact]
        public async Task CanPerformDump_Dumper()
        {
            var backupPath = NewDataPath("BackupFolder");
            using (var store = NewDocumentStore())
            {
                InsertUsers(store, 0, 2000);

                var options = new SmugglerOptions
                {
                    BackupPath = backupPath,
                };
                var dumper = new DataDumper(store.DocumentDatabase, options);
                var backupStatus = new PeriodicBackupStatus();
                await dumper.ExportData(null, null, true, backupStatus);
            }

            VerifyDump(backupPath, store =>
            {
                using (var session = store.OpenSession())
                {
                    Assert.Equal(2000, session.Query<User>().Customize(x => x.WaitForNonStaleResultsAsOfNow()).Count());
                }
            });
            IOExtensions.DeleteDirectory(backupPath);
        }

        [Fact]
        public async Task CanPerformDump_Smuggler()
        {
            var backupPath = NewDataPath("BackupFolder");
            using (var store = NewRemoteDocumentStore())
            {
                InsertUsers(store, 0, 2000);

                var options = new SmugglerOptions
                {
                    BackupPath = backupPath,
                };
                var dumper = new SmugglerApi(options, new RavenConnectionStringOptions
                {
                    Url = "http://localhost:8079",
                });
                var backupStatus = new PeriodicBackupStatus();
                await dumper.ExportData(null, null, true, backupStatus);
            }

            VerifyDump(backupPath, store =>
            {
                using (var session = store.OpenSession())
                {
                    Assert.Equal(2000, session.Query<User>().Customize(x => x.WaitForNonStaleResultsAsOfNow()).Count());
                }
            });
            IOExtensions.DeleteDirectory(backupPath);
        }

        [Fact]
        public async Task CanPerformDumpWithLimit_Dumper()
        {
            var backupPath = NewDataPath("BackupFolder");
            using (var store = NewDocumentStore())
            {
                InsertUsers(store, 0, 2000);

                var options = new SmugglerOptions
                {
                    Limit = 1500,
                    BackupPath = backupPath,
                    Filters =
                {
                    new FilterSetting
                    {
                        Path = "@metadata.Raven-Entity-Name",
                        Values = {"Users"},
                        ShouldMatch = true,
                    }
                }
                };
                var dumper = new DataDumper(store.DocumentDatabase, options);
                var backupStatus = new PeriodicBackupStatus();
                await dumper.ExportData(null, null, true, backupStatus);
            }


            VerifyDump(backupPath, store =>
            {
                using (var session = store.OpenSession())
                {
                    Assert.Equal(1500, session.Query<User>().Customize(x => x.WaitForNonStaleResultsAsOfNow()).Count());
                }
            });
            IOExtensions.DeleteDirectory(backupPath);
        }

        [Fact]
        public async Task CanPerformDumpWithLimit_Smuggler()
        {
            var backupPath = NewDataPath("BackupFolder");
            using (var store = NewRemoteDocumentStore())
            {
                InsertUsers(store, 0, 2000);

                var options = new SmugglerOptions
                {
                    Limit = 1500,
                    BackupPath = backupPath,
                    Filters =
                {
                    new FilterSetting
                    {
                        Path = "@metadata.Raven-Entity-Name",
                        Values = {"Users"},
                        ShouldMatch = true,
                    }
                }
                };
                var dumper = new SmugglerApi(options, new RavenConnectionStringOptions
                {
                    Url = "http://localhost:8079",
                });
                var backupStatus = new PeriodicBackupStatus();
                await dumper.ExportData(null, null, true, backupStatus);
            }


            VerifyDump(backupPath, store =>
            {
                using (var session = store.OpenSession())
                {
                    Assert.Equal(1500, session.Query<User>().Customize(x => x.WaitForNonStaleResultsAsOfNow()).Count());
                }
            });
            IOExtensions.DeleteDirectory(backupPath);
        }

        private void VerifyDump(string backupPath, Action<EmbeddableDocumentStore> action)
        {
            using (var store = NewDocumentStore())
            {
                var smugglerOptions = new SmugglerOptions
                {
                    BackupPath = backupPath
                };
                var dataDumper = new DataDumper(store.DocumentDatabase, smugglerOptions);
                dataDumper.ImportData(smugglerOptions, true).Wait();

                action(store);
            }
        }

        [Fact]
        public async Task CanPerformDumpWithLimitAndFilter_Dumper()
        {
            var backupPath = NewDataPath("BackupFolder");
            using (var store = NewDocumentStore())
            {
                var counter = 0;
                counter = InsertUsers(store, counter, 1000);
                counter = InsertDevelopers(store, counter, 2);
                counter = InsertUsers(store, counter, 1000);
                InsertDevelopers(store, counter, 2);

                WaitForIndexing(store);

                var options = new SmugglerOptions
                {
                    Limit = 5,
                    BackupPath = backupPath,
                    Filters =
                {
                    new FilterSetting
                    {
                        Path = "@metadata.Raven-Entity-Name",
                        Values = {"Developers"},
                        ShouldMatch = true,
                    }
                }
                };
                var dumper = new DataDumper(store.DocumentDatabase, options);
                var backupStatus = new PeriodicBackupStatus();
                await dumper.ExportData(null, null, true, backupStatus);

            }


            VerifyDump(backupPath, store =>
            {
                using (var session = store.OpenSession())
                {
                    Assert.Equal(4, session.Query<Developer>().Customize(x => x.WaitForNonStaleResultsAsOfNow()).Count());
                }
            });

            IOExtensions.DeleteDirectory(backupPath);
        }

        [Fact]
        public async Task CanPerformDumpWithLimitAndFilter_Smuggler()
        {
            var backupPath = NewDataPath("BackupFolder");
            using (var store = NewRemoteDocumentStore())
            {
                var counter = 0;
                counter = InsertUsers(store, counter, 1000);
                counter = InsertDevelopers(store, counter, 2);
                counter = InsertUsers(store, counter, 1000);
                InsertDevelopers(store, counter, 2);

                WaitForIndexing(store);

                var options = new SmugglerOptions
                {
                    Limit = 5,
                    BackupPath = backupPath,
                    Filters =
                {
                    new FilterSetting
                    {
                        Path = "@metadata.Raven-Entity-Name",
                        Values = {"Developers"},
                        ShouldMatch = true,
                    }
                }
                };
                var dumper = new SmugglerApi(options, new RavenConnectionStringOptions
                {
                    Url = "http://localhost:8079",
                });
                var backupStatus = new PeriodicBackupStatus();
                await dumper.ExportData(null, null, true, backupStatus);

            }


            VerifyDump(backupPath, store =>
            {
                using (var session = store.OpenSession())
                {
                    Assert.Equal(4, session.Query<Developer>().Customize(x => x.WaitForNonStaleResultsAsOfNow()).Count());
                }
            });

            IOExtensions.DeleteDirectory(backupPath);
        }

        private static int InsertDevelopers(IDocumentStore store, int counter, int amount)
        {
            using (var session = store.OpenSession())
            {
                for (var j = 0; j < amount; j++)
                {
                    counter++;
                    session.Store(new Developer { Name = "Developer #" + (counter) });
                }
                session.SaveChanges();
            }
            return counter;
        }

        private static int InsertUsers(IDocumentStore store, int counter, int amount)
        {
            for (var i = 0; i < amount / 25; i++)
            {
                using (var session = store.OpenSession())
                {
                    for (var j = 0; j < 25; j++)
                    {
                        counter++;
                        session.Store(new User { Name = "USer #" + counter });
                    }
                    session.SaveChanges();
                }
            }
            return counter;
        }

        [Fact]
        public async Task CanDumpWhenHiddenDocs_Dumper()
        {
            var backupPath = NewDataPath("BackupFolder");
            using (var server = GetNewServer())
            {
                using (var store = new DocumentStore { Url = "http://localhost:8079" }.Initialize())
                {
                    InsertHidenUsers(store, 2000);

                    var user1 = store.DatabaseCommands.Get("users/1");
                    Assert.Null(user1);

                    InsertUsers(store, 1, 25);

                    // now perform full backup
                    var options = new SmugglerOptions
                    {
                        BackupPath = backupPath,
                    };
                    var dumper = new DataDumper(server.Database, options);
                    var backupStatus = new PeriodicBackupStatus();
                    await dumper.ExportData(null, null, true, backupStatus);
                }
            }

            VerifyDump(backupPath, store =>
            {
                using (var session = store.OpenSession())
                {
                    Assert.Equal(25, session.Query<User>().Customize(x => x.WaitForNonStaleResultsAsOfNow()).Count());
                }
            });

            IOExtensions.DeleteDirectory(backupPath);
        }

        [Fact]
        public async Task CanDumpWhenHiddenDocs_Smuggler()
        {
            var backupPath = NewDataPath("BackupFolder");
            using (NewRemoteDocumentStore())
            {
                using (var store = new DocumentStore { Url = "http://localhost:8079" }.Initialize())
                {
                    InsertHidenUsers(store, 2000);

                    var user1 = store.DatabaseCommands.Get("users/1");
                    Assert.Null(user1);

                    InsertUsers(store, 1, 25);

                    // now perform full backup
                    var options = new SmugglerOptions
                    {
                        BackupPath = backupPath,
                    };
                    var dumper = new SmugglerApi(options, new RavenConnectionStringOptions
                    {
                        Url = "http://localhost:8079",
                    });
                    var backupStatus = new PeriodicBackupStatus();
                    await dumper.ExportData(null, null, true, backupStatus);
                }
            }

            VerifyDump(backupPath, store =>
            {
                using (var session = store.OpenSession())
                {
                    Assert.Equal(25, session.Query<User>().Customize(x => x.WaitForNonStaleResultsAsOfNow()).Count());
                }
            });

            IOExtensions.DeleteDirectory(backupPath);
        }

        [Fact]
        public async Task CanDumpEmptyDatabase_Dumper()
        {
            var backupPath = NewDataPath("BackupFolder");
            using (var server = GetNewServer())
            {
                using (new DocumentStore { Url = "http://localhost:8079" }.Initialize())
                {
                    // now perform full backup
                    var options = new SmugglerOptions
                    {
                        BackupPath = backupPath,
                    };
                    var dumper = new DataDumper(server.Database, options);
                    var backupStatus = new PeriodicBackupStatus();
                    await dumper.ExportData(null, null, true, backupStatus);
                }
            }

            VerifyDump(backupPath, store =>
            {
                Assert.Equal(0, store.DocumentDatabase.GetDocuments(0, int.MaxValue, null, CancellationToken.None).Count());
            });

            IOExtensions.DeleteDirectory(backupPath);
        }

        [Fact]
        public async Task CanDumpEmptyDatabase_Smuggler()
        {
            var backupPath = NewDataPath("BackupFolder");
            using (NewRemoteDocumentStore())
            {
                // now perform full backup
                var options = new SmugglerOptions
                {
                    BackupPath = backupPath,
                };
                var dumper = new SmugglerApi(options, new RavenConnectionStringOptions
                {
                    Url = "http://localhost:8079",
                });
                var backupStatus = new PeriodicBackupStatus();
                await dumper.ExportData(null, null, true, backupStatus);
            }

            VerifyDump(backupPath, store =>
            {
                Assert.Equal(0, store.DocumentDatabase.GetDocuments(0, int.MaxValue, null, CancellationToken.None).Count());
            });

            IOExtensions.DeleteDirectory(backupPath);
        }

        [Fact]
        public async Task CanDumpWhenHiddenDocsWithLimit_Dumper()
        {
            var backupPath = NewDataPath("BackupFolder");
            using (var server = GetNewServer())
            {
                using (var store = new DocumentStore { Url = "http://localhost:8079" }.Initialize())
                {
                    InsertHidenUsers(store, 2000);

                    var user1 = store.DatabaseCommands.Get("users/1");
                    Assert.Null(user1);

                    InsertUsers(store, 1, 25);

                    // now perform full backup
                    var options = new SmugglerOptions
                    {
                        BackupPath = backupPath,
                    };
                    var dumper = new DataDumper(server.Database, options);
                    var backupStatus = new PeriodicBackupStatus();
                    await dumper.ExportData(null, null, true, backupStatus);
                }
            }

            VerifyDump(backupPath, store =>
            {
                using (var session = store.OpenSession())
                {
                    Assert.Equal(25, session.Query<User>().Customize(x => x.WaitForNonStaleResultsAsOfNow()).Count());
                }
            });

            IOExtensions.DeleteDirectory(backupPath);
        }

        [Fact]
        public async Task CanDumpWhenHiddenDocsWithLimit_Smuggler()
        {
            var backupPath = NewDataPath("BackupFolder");
            using (GetNewServer())
            {
                using (var store = new DocumentStore { Url = "http://localhost:8079" }.Initialize())
                {
                    InsertHidenUsers(store, 2000);

                    var user1 = store.DatabaseCommands.Get("users/1");
                    Assert.Null(user1);

                    InsertUsers(store, 1, 25);

                    // now perform full backup
                    var options = new SmugglerOptions
                    {
                        BackupPath = backupPath,
                    };
                    var dumper = new SmugglerApi(options, new RavenConnectionStringOptions
                    {
                        Url = "http://localhost:8079",
                    });
                    var backupStatus = new PeriodicBackupStatus();
                    await dumper.ExportData(null, null, true, backupStatus);
                }
            }

            VerifyDump(backupPath, store =>
            {
                using (var session = store.OpenSession())
                {
                    Assert.Equal(25, session.Query<User>().Customize(x => x.WaitForNonStaleResultsAsOfNow()).Count());
                }
            });

            IOExtensions.DeleteDirectory(backupPath);
        }

        private static void InsertHidenUsers(IDocumentStore store, int amount)
        {
            for (var i = 0; i < amount; i++)
            {
                store.DatabaseCommands.Put("user/" + (i + 1), null, new RavenJObject(), RavenJObject.FromObject(new
                {
                    hidden = true
                }));
            }
        }

        [Fact]
        public async Task CanDumpAttachments_Dumper()
        {
            var backupPath = NewDataPath("BackupFolder");
            using (var store = NewDocumentStore())
            {
                InsertAttachments(store, 328);

                var options = new SmugglerOptions
                {
                    BackupPath = backupPath,
                    BatchSize = 100
                };
                var dumper = new DataDumper(store.DocumentDatabase, options);
                var backupStatus = new PeriodicBackupStatus();
                await dumper.ExportData(null, null, true, backupStatus);
            }

            VerifyDump(backupPath, store =>
            {
                Assert.Equal(328, store.DatabaseCommands.GetAttachmentHeadersStartingWith("user", 0, 500).Count());
            });
            IOExtensions.DeleteDirectory(backupPath);
        }

        [Fact]
        public async Task CanDumpAttachments_Smuggler()
        {
            var backupPath = NewDataPath("BackupFolder");
            using (var store = NewRemoteDocumentStore())
            {
                InsertAttachments(store, 328);

                var options = new SmugglerOptions
                {
                    BackupPath = backupPath,
                    BatchSize = 100
                };
                var dumper = new SmugglerApi(options, new RavenConnectionStringOptions
                {
                    Url = "http://localhost:8079",
                });
                var backupStatus = new PeriodicBackupStatus();
                await dumper.ExportData(null, null, true, backupStatus);
            }

            VerifyDump(backupPath, store =>
            {
                Assert.Equal(328, store.DatabaseCommands.GetAttachmentHeadersStartingWith("user", 0, 500).Count());
            });
            IOExtensions.DeleteDirectory(backupPath);
        }

        [Fact]
        public async Task CanDumpAttachmentsWithLimit_Dumper()
        {
            var backupPath = NewDataPath("BackupFolder");
            using (var store = NewDocumentStore())
            {
                InsertAttachments(store, 328);

                var options = new SmugglerOptions
                {
                    BackupPath = backupPath,
                    BatchSize = 100,
                    Limit = 206
                };
                var dumper = new DataDumper(store.DocumentDatabase, options);
                var backupStatus = new PeriodicBackupStatus();
                await dumper.ExportData(null, null, true, backupStatus);
            }

            VerifyDump(backupPath, store =>
            {
                Assert.Equal(206, store.DatabaseCommands.GetAttachmentHeadersStartingWith("user", 0, 500).Count());
            });
            IOExtensions.DeleteDirectory(backupPath);
        }

        [Fact]
        public async Task CanDumpAttachmentsWithLimit_Smuggler()
        {
            var backupPath = NewDataPath("BackupFolder");
            using (var store = NewRemoteDocumentStore())
            {
                InsertAttachments(store, 328);

                var options = new SmugglerOptions
                {
                    BackupPath = backupPath,
                    BatchSize = 100,
                    Limit = 206
                };
                var dumper = new SmugglerApi(options, new RavenConnectionStringOptions
                {
                    Url = "http://localhost:8079",
                });
                var backupStatus = new PeriodicBackupStatus();
                await dumper.ExportData(null, null, true, backupStatus);
            }

            VerifyDump(backupPath, store =>
            {
                Assert.Equal(206, store.DatabaseCommands.GetAttachmentHeadersStartingWith("user", 0, 500).Count());
            });
            IOExtensions.DeleteDirectory(backupPath);
        }

        [Fact]
        public async Task CanDumpAttachmentsEmpty_Dumper()
        {
            var backupPath = NewDataPath("BackupFolder");
            using (var store = NewDocumentStore())
            {
                var options = new SmugglerOptions
                {
                    BackupPath = backupPath,
                    BatchSize = 100,
                    Limit = 206
                };
                var dumper = new DataDumper(store.DocumentDatabase, options);
                var backupStatus = new PeriodicBackupStatus();
                await dumper.ExportData(null, null, true, backupStatus);
            }

            VerifyDump(backupPath, store =>
            {
                Assert.Equal(0, store.DatabaseCommands.GetAttachmentHeadersStartingWith("user", 0, 500).Count());
            });
            IOExtensions.DeleteDirectory(backupPath);
        }

        [Fact]
        public async Task CanDumpAttachmentsEmpty_Smuggler()
        {
            var backupPath = NewDataPath("BackupFolder");
            using (NewRemoteDocumentStore())
            {
                var options = new SmugglerOptions
                {
                    BackupPath = backupPath,
                    BatchSize = 100,
                    Limit = 206
                };
                var dumper = new SmugglerApi(options, new RavenConnectionStringOptions
                {
                    Url = "http://localhost:8079",
                });
                var backupStatus = new PeriodicBackupStatus();
                await dumper.ExportData(null, null, true, backupStatus);
            }

            VerifyDump(backupPath, store =>
            {
                Assert.Equal(0, store.DatabaseCommands.GetAttachmentHeadersStartingWith("user", 0, 500).Count());
            });
            IOExtensions.DeleteDirectory(backupPath);
        }

        private void InsertAttachments(IDocumentStore store, int amount)
        {
            var counter = 0;
            var data = new byte[] { 1, 2, 3, 4 };
            for (var i = 0; i < amount; i++)
            {
                var documentKey = "users/" + (++counter);
                store.DatabaseCommands.PutAttachment(documentKey, null, new MemoryStream(data), new RavenJObject());
            }
        }
    }
}