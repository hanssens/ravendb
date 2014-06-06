﻿// -----------------------------------------------------------------------
//  <copyright file="DebugUser.cs" company="Hibernating Rhinos LTD">
//      Copyright (c) Hibernating Rhinos LTD. All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------
using System.Collections.Generic;
using System.Security.Principal;
using Raven.Abstractions.Data;
using Raven.Database.Server.Abstractions;
using Raven.Database.Extensions;
using Raven.Database.Server.Security.OAuth;
using System.Linq;
using Raven.Json.Linq;

namespace Raven.Database.Server.Responders.Debugging
{
	public class DebugUser : AbstractRequestResponder
	{
		public override string UrlPattern
		{
			get { return @"^/debug/user-info$"; }
		}

		public override string[] SupportedVerbs
		{
			get { return new[] {"GET", "POST"}; }
		}

		public override void Respond(IHttpContext context)
		{
			var principal = context.User;
			if (principal == null || principal.Identity == null || principal.Identity.IsAuthenticated == false)
			{
				var anonymous = new UserInfo
				{
					Remark = "Using anonymous user",
					IsAdminGlobal = server.SystemConfiguration.AnonymousUserAccessMode == AnonymousUserAccessMode.Admin
				};
				context.WriteJson(RavenJObject.FromObject(anonymous));
				return;
			}
			var windowsPrincipal = principal as WindowsPrincipal;
			if (windowsPrincipal != null)
			{
				var windowsUser = new UserInfo
				{
					Remark = "Using windows auth",
					User = windowsPrincipal.Identity.Name,
					IsAdminGlobal = windowsPrincipal.IsAdministrator(server.SystemConfiguration.AnonymousUserAccessMode),
                    IsAdminCurrentDb = windowsPrincipal.IsAdministrator(Database),
				};
				context.WriteJson(RavenJObject.FromObject(windowsUser));
				return;
			}

			var principalWithDatabaseAccess = principal as PrincipalWithDatabaseAccess;
			if (principalWithDatabaseAccess != null)
			{
				var databases = principalWithDatabaseAccess.AdminDatabases
					.Concat(principalWithDatabaseAccess.ReadOnlyDatabases)
					.Concat(principalWithDatabaseAccess.ReadWriteDatabases)
					.Select(db => new DatabaseInfo
					{
						Database = db,
						IsAdmin = principal.IsAdministrator(db)
					}).ToList();

				var windowsUserWithDatabase = new UserInfo
				{
					Remark = "Using windows auth with database access",
					User = principalWithDatabaseAccess.Identity.Name,
					IsAdminGlobal = principalWithDatabaseAccess.IsAdministrator(server.SystemConfiguration.AnonymousUserAccessMode) ||
					                IsLocalGlobalAdmin(databases),
					IsAdminCurrentDb = principalWithDatabaseAccess.IsAdministrator(Database),
					Databases = databases,
					AdminDatabases = principalWithDatabaseAccess.AdminDatabases,
					ReadOnlyDatabases = principalWithDatabaseAccess.ReadOnlyDatabases,
					ReadWriteDatabases = principalWithDatabaseAccess.ReadWriteDatabases
				};

				context.WriteJson(RavenJObject.FromObject(windowsUserWithDatabase));
				return;
			}

			var oAuthPrincipal = principal as OAuthPrincipal;
			if (oAuthPrincipal != null)
			{
				var databases = oAuthPrincipal.TokenBody.AuthorizedDatabases
					                                      .Select(db => new DatabaseInfo
					                                                    {
						                                                    Database = db.TenantId,
						                                                    IsAdmin = principal.IsAdministrator(db.TenantId)
					                                                    }).ToList();
				var oAuth = new UserInfo
				            {
					            Remark = "Using OAuth",
					            User = oAuthPrincipal.Name,
					            IsAdminGlobal = oAuthPrincipal.IsAdministrator(server.SystemConfiguration.AnonymousUserAccessMode) || IsLocalGlobalAdmin(databases),
					            IsAdminCurrentDb = oAuthPrincipal.IsAdministrator(Database),
					            Databases = databases,
					            AccessTokenBody = oAuthPrincipal.TokenBody,
				            };
				context.WriteJson(RavenJObject.FromObject(oAuth));
				return;
			}

		    var unknown = new UserInfo
		    {
		        Remark = "Unknown auth",
		        Principal = principal
		    };
			context.WriteJson(RavenJObject.FromObject(unknown));
		}

		private bool IsLocalGlobalAdmin(List<DatabaseInfo> databases)
		{
			if (databases == null)
				return false;
			var star = databases.FirstOrDefault(info => info.Database == "*");
			var system = databases.FirstOrDefault(info => info.Database == Constants.SystemDatabase);
			if (star != null && star.IsAdmin && system != null && system.IsAdmin)
				return true;

			return false;
		}
	}
}