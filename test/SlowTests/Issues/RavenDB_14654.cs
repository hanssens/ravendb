﻿using System;
using FastTests;
using Raven.Client.Documents.Operations;
using Raven.Client.Documents.Operations.Backups;
using Raven.Client.Documents.Operations.CompareExchange;
using Xunit;
using Xunit.Abstractions;

namespace SlowTests.Issues
{
    public class RavenDB_14654 : RavenTestBase
    {
        public RavenDB_14654(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void FullBackupShouldBackupDocumentTombstones()
        {
            using var store = GetDocumentStore();

            // create 3 docs
            using (var session = store.OpenSession())
            {
                for (int i = 1; i <= 3; i++)
                {
                    session.Store(new DummyDoc { DummyString = $"{i}" }, $"{nameof(DummyDoc)}/{i}");
                }

                session.SaveChanges();
            }

            // create 3 CompareExchanges
            var rhinoceros = store.Operations.Send(new PutCompareExchangeValueOperation<DummyDoc>("emojis/Rhinoceros", new DummyDoc { DummyString = "🦏" }, 0));
            Assert.True(rhinoceros.Successful);
            var shark = store.Operations.Send(new PutCompareExchangeValueOperation<DummyDoc>("emojis/Shark", new DummyDoc { DummyString = "🦈" }, 0));
            Assert.True(shark.Successful);
            var bird = store.Operations.Send(new PutCompareExchangeValueOperation<DummyDoc>("emojis/Bird", new DummyDoc { DummyString = "🐦" }, 0));
            Assert.True(bird.Successful);

            // create 1 tombstone
            using (var session = store.OpenSession())
            {
                session.Delete($"{nameof(DummyDoc)}/{new Random().Next(1, 3)}");
                session.SaveChanges();
            }

            // create 1 CompareExchange tombstone
            var res = store.Operations.Send(new DeleteCompareExchangeValueOperation<DummyDoc>($"emojis/Rhinoceros", rhinoceros.Index));
            Assert.True(res.Successful);

            // run full backup
            var config = new PeriodicBackupConfiguration
            {
                LocalSettings = new LocalSettings
                {
                    FolderPath = NewDataPath(forceCreateDir: true)
                },
                Name = "full backup with tombstones",
                FullBackupFrequency = "0 0 1 1 *",
                BackupType = BackupType.Backup
            };

            var documentDb = Server.ServerStore.DatabasesLandlord.TryGetOrCreateResourceStore(store.Database);
            (long etag, _) = documentDb.Result.ReadLastEtagAndChangeVector();

            var result = store.Maintenance.Send(new UpdatePeriodicBackupOperation(config));
            store.Maintenance.Send(new StartBackupOperation(true, result.TaskId));

            var operation = new GetPeriodicBackupStatusOperation(result.TaskId);
            PeriodicBackupStatus status = null;
            var value = WaitForValue(() =>
            {
                status = store.Maintenance.Send(operation).Status;
                return status?.LastEtag;
            }, expectedVal: etag);
            Assert.True(etag == value, $"gotStatus? {status != null}, Status Error: {status?.Error?.Exception}, LocalBackup Exception: {status?.LocalBackup?.Exception}");
            Assert.NotNull(status.LocalBackup);

            var backupPath = status.LocalBackup.BackupDirectory;
            var restoredDbName = GetDatabaseName();
            using (RestoreDatabase(store, new RestoreBackupConfiguration
            {
                BackupLocation = backupPath,
                DatabaseName = restoredDbName
            }))
            {
                var stats = store.Maintenance.ForDatabase(restoredDbName).Send(new GetDetailedStatisticsOperation());
                Assert.Equal(2, stats.CountOfDocuments);
                Assert.Equal(1, stats.CountOfTombstones);
                Assert.Equal(2, stats.CountOfCompareExchange);

                // we don't put the Compare Exchange tombstones into the schema, on backup restore.
                Assert.Equal(0, stats.CountOfCompareExchangeTombstones);
            }
        }

        private class DummyDoc
        {
            public string DummyString { get; set; }
        }
    }
}
