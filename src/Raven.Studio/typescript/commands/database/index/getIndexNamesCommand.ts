import commandBase = require("commands/commandBase");
import database = require("models/resources/database");
import endpoints = require("endpoints");

class getIndexNamesCommand extends commandBase {

    constructor(private db: database) {
        super();
    }

    execute(): JQueryPromise<string[]> {
        const args = {
            namesOnly: true,
            pageSize: 1024
        };
        const url = endpoints.databases.index.indexes;
        return this.query(url, args, this.db, (x: resultsDto<string>) => x.Results)
            .fail((response: JQueryXHR) => this.reportError("Failed to get the database indexes", response.responseText, response.statusText));
    }
} 

export = getIndexNamesCommand;
