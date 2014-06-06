﻿//-----------------------------------------------------------------------
// <copyright file="RavenTest.cs" company="Hibernating Rhinos LTD">
//     Copyright (c) Hibernating Rhinos LTD. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition.Primitives;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Raven.Abstractions.Data;
using Raven.Abstractions.Extensions;
using Raven.Abstractions.MEF;
using Raven.Client;
using Raven.Client.Connection;
using Raven.Client.Document;
using Raven.Client.Embedded;
using Raven.Client.Indexes;
using Raven.Database;
using Raven.Database.Config;
using Raven.Database.Extensions;
using Raven.Database.Impl;
using Raven.Database.Plugins;
using Raven.Database.Server;
using Raven.Database.Server.Security;
using Raven.Database.Storage;
using Raven.Json.Linq;
using Raven.Server;
using Xunit;
using Xunit.Sdk;

namespace Raven.Tests.Helpers
{
	public class RavenTestBase : IDisposable
	{
		protected readonly List<RavenDbServer> servers = new List<RavenDbServer>();
		protected readonly List<IDocumentStore> stores = new List<IDocumentStore>();
		private readonly List<string> pathsToDelete = new List<string>();

		public RavenTestBase()
		{
            Environment.SetEnvironmentVariable(Constants.RavenDefaultQueryTimeout, "30");
			CommonInitializationUtil.Initialize();
		}

		protected string NewDataPath(string prefix = null)
		{
			var newDataDir = Path.GetFullPath(string.Format(@".\{0}-{1}-{2}\", DateTime.Now.ToString("yyyy-MM-dd,HH-mm-ss"), prefix ?? "TestDatabase", Guid.NewGuid().ToString("N")));
			Directory.CreateDirectory(newDataDir);
			pathsToDelete.Add(newDataDir);
			return newDataDir;
		}

		public EmbeddableDocumentStore NewDocumentStore(
			bool runInMemory = true,
			string requestedStorage = null,
			ComposablePartCatalog catalog = null,
			string dataDir = null,
			bool enableAuthentication = false)
		{
			var storageType = GetDefaultStorageType(requestedStorage);
			var documentStore = new EmbeddableDocumentStore
			{
				Configuration =
				{
					DefaultStorageTypeName = storageType,
					DataDirectory = dataDir ?? NewDataPath(),
					RunInUnreliableYetFastModeThatIsNotSuitableForProduction = true,
					RunInMemory = storageType.Equals("esent", StringComparison.OrdinalIgnoreCase) == false && runInMemory,
					Port = 8079
				}
			};

			if (catalog != null)
				documentStore.Configuration.Catalog.Catalogs.Add(catalog);

			try
			{
				ModifyStore(documentStore);
				ModifyConfiguration(documentStore.Configuration);

				documentStore.Initialize();

				if (enableAuthentication)
				{
					EnableAuthentication(documentStore.DocumentDatabase);
					ModifyConfiguration(documentStore.Configuration);
				}

				CreateDefaultIndexes(documentStore);

				return documentStore;
			}
			catch
			{
				// We must dispose of this object in exceptional cases, otherwise this test will break all the following tests.
				documentStore.Dispose();
				throw;
			}
			finally
			{
				stores.Add(documentStore);
			}
		}

		public static void EnableAuthentication(DocumentDatabase database)
		{
			var license = GetLicenseByReflection(database);
			license.Error = false;
			license.Status = "Commercial";

			// rerun this startup task
			database.StartupTasks.OfType<AuthenticationForCommercialUseOnly>().First().Execute(database);
		}

		public IDocumentStore NewRemoteDocumentStore(bool fiddler = false, RavenDbServer ravenDbServer = null, string databaseName = null,
			bool runInMemory = true,
			string dataDirectory = null,
			string requestedStorage = null,
			bool enableAuthentication = false)
		{
			ravenDbServer = ravenDbServer ?? GetNewServer(runInMemory: runInMemory, dataDirectory: dataDirectory, requestedStorage: requestedStorage, enableAuthentication: enableAuthentication);
			ModifyServer(ravenDbServer);
			var store = new DocumentStore
			{
				Url = GetServerUrl(fiddler),
				DefaultDatabase = databaseName,
			};
			stores.Add(store);
			store.AfterDispose += (sender, args) => ravenDbServer.Dispose();
			ModifyStore(store);
			return store.Initialize();
		}

		private static string GetServerUrl(bool fiddler)
		{
			if (fiddler)
			{
				if (Process.GetProcessesByName("fiddler").Any())
					return "http://localhost.fiddler:8079";
			}
			return "http://localhost:8079";
		}

		public static string GetDefaultStorageType(string requestedStorage = null)
		{
			string defaultStorageType;
			var envVar = Environment.GetEnvironmentVariable("raventest_storage_engine");
			if (string.IsNullOrEmpty(envVar) == false)
				defaultStorageType = envVar;
			else if (requestedStorage != null)
				defaultStorageType = requestedStorage;
			else
				defaultStorageType = "munin";
			return defaultStorageType;
		}

		protected RavenDbServer GetNewServer(int port = 8079,
			string dataDirectory = null,
			bool runInMemory = true,
			string requestedStorage = null,
			bool enableAuthentication = false)
		{
			if (dataDirectory != null)
				pathsToDelete.Add(dataDirectory);

			var storageType = GetDefaultStorageType(requestedStorage);
			var ravenConfiguration = new RavenConfiguration
			{
				Port = port,
				DataDirectory = dataDirectory ?? NewDataPath(),
				RunInMemory = storageType.Equals("esent", StringComparison.OrdinalIgnoreCase) == false && runInMemory,
				DefaultStorageTypeName = storageType,
				AnonymousUserAccessMode = enableAuthentication ? AnonymousUserAccessMode.None : AnonymousUserAccessMode.Admin
			};

			ModifyConfiguration(ravenConfiguration);

			ravenConfiguration.PostInit();

			NonAdminHttp.EnsureCanListenToWhenInNonAdminContext(ravenConfiguration.Port);
			var ravenDbServer = new RavenDbServer(ravenConfiguration);
			servers.Add(ravenDbServer);

			try
			{
				using (var documentStore = new DocumentStore
				{
					Url = "http://localhost:" + port,
					Conventions =
					{
						FailoverBehavior = FailoverBehavior.FailImmediately
					},
				}.Initialize())
				{
					CreateDefaultIndexes(documentStore);
				}
			}
			catch
			{
				ravenDbServer.Dispose();
				throw;
			}

			if (enableAuthentication)
			{
				EnableAuthentication(ravenDbServer.Database);
				ModifyConfiguration(ravenConfiguration);
			}

			return ravenDbServer;
		}

		public ITransactionalStorage NewTransactionalStorage(string requestedStorage = null, string dataDir = null)
		{
			ITransactionalStorage newTransactionalStorage;
			string storageType = GetDefaultStorageType(requestedStorage);

			if (storageType == "munin")
				newTransactionalStorage = new Storage.Managed.TransactionalStorage(new RavenConfiguration {DataDirectory = dataDir ?? NewDataPath(),}, () => { });
			else
				newTransactionalStorage = new Storage.Esent.TransactionalStorage(new RavenConfiguration {DataDirectory = dataDir ?? NewDataPath(),}, () => { });

			newTransactionalStorage.Initialize(new DummyUuidGenerator(), new OrderedPartCollection<AbstractDocumentCodec>());
			return newTransactionalStorage;
		}

		protected virtual void ModifyStore(DocumentStore documentStore)
		{
		}

		protected virtual void ModifyStore(EmbeddableDocumentStore documentStore)
		{
		}

		protected virtual void ModifyConfiguration(InMemoryRavenConfiguration configuration)
		{
		}

		protected virtual void ModifyServer(RavenDbServer ravenDbServer)
		{
		}

		protected virtual void CreateDefaultIndexes(IDocumentStore documentStore)
		{
			new RavenDocumentsByEntityName().Execute(documentStore);
		}

		public static void WaitForIndexing(IDocumentStore store, string db = null,TimeSpan? timeout = null)
		{
			var databaseCommands = store.DatabaseCommands;
			if (db != null)
				databaseCommands = databaseCommands.ForDatabase(db);
		    bool spinUntil = SpinWait.SpinUntil(() => databaseCommands.GetStatistics().StaleIndexes.Length == 0, timeout ?? TimeSpan.FromSeconds(20));
		    if (spinUntil == false)
		        WaitForUserToContinueTheTest((EmbeddableDocumentStore) store);
		    Assert.True(spinUntil);
		}

		public static void WaitForIndexing(DocumentDatabase db)
		{
			Assert.True(SpinWait.SpinUntil(() => db.Statistics.StaleIndexes.Length == 0, TimeSpan.FromMinutes(5)));
		}

		public static void WaitForAllRequestsToComplete(RavenDbServer server)
		{
			Assert.True(SpinWait.SpinUntil(() => server.Server.HasPendingRequests == false, TimeSpan.FromMinutes(15)));
		}

		protected void WaitForBackup(DocumentDatabase db, bool checkError)
		{
			WaitForBackup(key => db.Get(key, null), checkError);
		}

		protected void WaitForBackup(IDatabaseCommands commands, bool checkError)
		{
			WaitForBackup(commands.Get, checkError);
		}

		private void WaitForBackup(Func<string, JsonDocument> getDocument, bool checkError)
		{
			var done = SpinWait.SpinUntil(() =>
			{
				// We expect to get the doc from database that we tried to backup
				var jsonDocument = getDocument(BackupStatus.RavenBackupStatusDocumentKey);
				if (jsonDocument == null)
					return true;

				var backupStatus = jsonDocument.DataAsJson.JsonDeserialization<BackupStatus>();
				if (backupStatus.IsRunning == false)
				{
					if (checkError)
					{
						var firstOrDefault =
							backupStatus.Messages.FirstOrDefault(x => x.Severity == BackupStatus.BackupMessageSeverity.Error);
						if (firstOrDefault != null)
							Assert.False(true, firstOrDefault.Message);
					}

					return true;
				}
				return false;
			}, TimeSpan.FromMinutes(15));
			Assert.True(done);
		}

		protected void WaitForRestore(IDatabaseCommands databaseCommands)
		{
			var done = SpinWait.SpinUntil(() =>
			{
				// We expect to get the doc from the <system> database
				var doc = databaseCommands.Get(RestoreStatus.RavenRestoreStatusDocumentKey);

				if (doc == null)
					return false;

				var status = doc.DataAsJson["restoreStatus"].Values().Select(token => token.ToString()).ToList();

				var restoreFinishMessages = new[]
				{
					"The new database was created",
					"Esent Restore: Restore Complete", 
					"Restore ended but could not create the datebase document, in order to access the data create a database with the appropriate name",
				};
				return restoreFinishMessages.Any(status.Last().Contains);
			}, TimeSpan.FromMinutes(5));

			Assert.True(done);
		}

		protected void WaitForDocument(IDatabaseCommands databaseCommands, string id)
		{
			var done = SpinWait.SpinUntil(() =>
			{
				// We expect to get the doc from the <system> database
				var doc = databaseCommands.Get(id);
				return doc != null;
			}, TimeSpan.FromMinutes(5));

			Assert.True(done);
		}

		public static void WaitForUserToContinueTheTest(EmbeddableDocumentStore documentStore, bool debug = true)
		{
			if (debug && Debugger.IsAttached == false)
				return;

			documentStore.SetStudioConfigToAllowSingleDb();

			documentStore.DatabaseCommands.Put("Pls Delete Me", null,

											   RavenJObject.FromObject(new { StackTrace = new StackTrace(true) }),
											   new RavenJObject());

			documentStore.Configuration.AnonymousUserAccessMode = AnonymousUserAccessMode.Admin;
			using (var server = new HttpServer(documentStore.Configuration, documentStore.DocumentDatabase))
			{
				server.StartListening();

				try
				{
					Process.Start(documentStore.Configuration.ServerUrl); // start the server
				}
				catch (Win32Exception e)
				{
					Console.WriteLine("Failed to open the browser. Please open it manually at {0}. {1}", documentStore.Configuration.ServerUrl, e);
				}

				do
				{
					Thread.Sleep(100);
				} while (documentStore.DatabaseCommands.Get("Pls Delete Me") != null && (debug == false || Debugger.IsAttached));
			}
		}

		protected void WaitForUserToContinueTheTest(bool debug = true, string url = null)
		{
			if (debug && Debugger.IsAttached == false)
				return;

			using (var documentStore = new DocumentStore
			{
				Url = url ?? "http://localhost:8079"
			})
			{
				documentStore.Initialize();
				documentStore.DatabaseCommands.Put("Pls Delete Me", null,
												   RavenJObject.FromObject(new { StackTrace = new StackTrace(true) }), new RavenJObject());

				Process.Start(documentStore.Url); // start the server

				do
				{
					Thread.Sleep(100);
				} while (documentStore.DatabaseCommands.Get("Pls Delete Me") != null && (debug == false || Debugger.IsAttached));
			}
		}

		protected void ClearDatabaseDirectory(string dataDir)
		{
			bool isRetry = false;

			while (true)
			{
				try
				{
					IOExtensions.DeleteDirectory(dataDir);
					break;
				}
				catch (IOException)
				{
					if (isRetry)
						throw;

					GC.Collect();
					GC.WaitForPendingFinalizers();
					isRetry = true;

                    Thread.Sleep(2500);
				}
			}
		}

		public virtual void Dispose()
		{
			var errors = new List<Exception>();

			foreach (var store in stores)
			{
				try
				{
					store.Dispose();
				}
				catch (Exception e)
				{
					errors.Add(e);
				}
			}

			foreach (var server in servers)
			{
				try
				{
					server.Dispose();
				}
				catch (Exception e)
				{
					errors.Add(e);
				}
			}

			GC.Collect(2);
			GC.WaitForPendingFinalizers();

			foreach (var pathToDelete in pathsToDelete)
			{
				try
				{
					ClearDatabaseDirectory(pathToDelete);
				}
				catch (Exception e)
				{
					errors.Add(e);
				}
			}

			if (errors.Count > 0)
				throw new AggregateException(errors);
		}

		protected static void PrintServerErrors(ServerError[] serverErrors)
		{
			if (serverErrors.Any())
			{
				Console.WriteLine("Server errors count: " + serverErrors.Count());
				foreach (var serverError in serverErrors)
				{
					Console.WriteLine("Server error: " + serverError.ToString());
				}
			}
			else
				Console.WriteLine("No server errors");
		}

		protected void AssertNoIndexErrors(IDocumentStore documentStore)
		{
			var embeddableDocumentStore = documentStore as EmbeddableDocumentStore;
			var errors = embeddableDocumentStore != null
									   ? embeddableDocumentStore.DocumentDatabase.Statistics.Errors
									   : documentStore.DatabaseCommands.GetStatistics().Errors;

			try
			{
				Assert.Empty(errors);
			}
			catch (EmptyException)
			{
				Console.WriteLine(errors.First().Error);
				throw;
			}
		}

		public static LicensingStatus GetLicenseByReflection(DocumentDatabase database)
		{
			var field = database.GetType().GetField("validateLicense", BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.NotNull(field);
			var validateLicense = field.GetValue(database);

			var currentLicenseProp = validateLicense.GetType().GetProperty("CurrentLicense", BindingFlags.Static | BindingFlags.Public);
			Assert.NotNull(currentLicenseProp);

			return (LicensingStatus)currentLicenseProp.GetValue(validateLicense, null);
		}
	}
}