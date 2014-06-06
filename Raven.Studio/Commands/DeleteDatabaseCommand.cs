using System.Linq;
using Raven.Abstractions.Data;
using Raven.Studio.Controls;
using Raven.Studio.Infrastructure;
using Raven.Studio.Models;
using Raven.Studio.Features.Input;

namespace Raven.Studio.Commands
{
	public class DeleteDatabaseCommand : Command
	{
		private readonly DatabasesListModel databasesModel;

		public DeleteDatabaseCommand(DatabasesListModel databasesModel)
		{
			this.databasesModel = databasesModel;
		}

		public override void Execute(object parameter)
		{
            new DeleteDatabase() { DatabaseName = databasesModel.SelectedDatabase.Name }.ShowAsync()
			                    .ContinueOnSuccessInTheUIThread(deleteDatabase =>
			                    {
									if(deleteDatabase == null)
										return;
				                    
				                    if (deleteDatabase.DialogResult != true)
					                    return;

				                    var asyncDatabaseCommands = ApplicationModel.Current.Server.Value.DocumentStore
				                                                                .AsyncDatabaseCommands
				                                                                .ForSystemDatabase();

			                        var relativeUrl = "/admin/databases/" + deleteDatabase.DatabaseName;

				                    if (deleteDatabase.hardDelete.IsChecked == true)
					                    relativeUrl += "?hard-delete=true";

				                    var httpJsonRequest = asyncDatabaseCommands.CreateRequest(relativeUrl, "DELETE");
				                    httpJsonRequest.ExecuteRequestAsync()
				                                   .ContinueOnSuccessInTheUIThread(() =>
				                                   {
					                                   var database = ApplicationModel.Current.Server
					                                                                  .Value.Databases
					                                                                  .FirstOrDefault(s =>
						                                                                  s != Constants.SystemDatabase &&
						                                                                  s != deleteDatabase.DatabaseName) ??
					                                                  Constants.SystemDatabase;
					                                   ExecuteCommand(new ChangeDatabaseCommand(), database);
				                                   });
			                    });
		}
	}
}