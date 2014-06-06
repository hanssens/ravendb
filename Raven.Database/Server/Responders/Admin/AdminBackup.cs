//-----------------------------------------------------------------------
// <copyright file="AdminBackup.cs" company="Hibernating Rhinos LTD">
//     Copyright (c) Hibernating Rhinos LTD. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Security.Principal;
using Raven.Abstractions.Data;
using Raven.Abstractions.Extensions;
using Raven.Database.Data;
using Raven.Database.Extensions;
using Raven.Database.Server.Abstractions;

namespace Raven.Database.Server.Responders.Admin
{
	public class AdminBackup : AdminResponder
	{
		public override string[] SupportedVerbs
		{
			get { return new[] { "POST" }; }
		}

		protected override WindowsBuiltInRole[] AdditionalSupportedRoles
		{
			get
			{
				return new[] { WindowsBuiltInRole.BackupOperator };
			}
		}

		public override void RespondToAdmin(IHttpContext context)
		{
			var backupRequest = context.ReadJsonObject<BackupRequest>();
			var incrementalString = context.Request.QueryString["incremental"];
			bool incrementalBackup;
			if (bool.TryParse(incrementalString, out incrementalBackup) == false)
				incrementalBackup = false;
			if (backupRequest.DatabaseDocument == null && Database.Name != null)
			{
				if (Database.Name.Equals(Constants.SystemDatabase, StringComparison.OrdinalIgnoreCase))
				{
					backupRequest.DatabaseDocument = new DatabaseDocument {Id = Constants.SystemDatabase};
				}
				else
				{
					var jsonDocument = SystemDatabase.Get("Raven/Databases/" + Database.Name, null);
					if (jsonDocument != null)
					{
						backupRequest.DatabaseDocument = jsonDocument.DataAsJson.JsonDeserialization<DatabaseDocument>();
						server.Unprotect(backupRequest.DatabaseDocument);
						backupRequest.DatabaseDocument.Id = Database.Name;
					}
				}
			}
			Database.StartBackup(backupRequest.BackupLocation, incrementalBackup, backupRequest.DatabaseDocument);
			context.SetStatusToCreated(BackupStatus.RavenBackupStatusDocumentKey);
		}
	}
}
