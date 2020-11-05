using System.Collections.Generic;
using System.IO;
using System.Linq;
using Raven.Client.Documents.Commands;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Session.Operations;
using Raven.Client.Documents.Session.Tokens;
using Raven.Client.Extensions;
using Raven.Client.Json;
using Sparrow.Json;

namespace Raven.Client.Documents.Session
{
    public partial class DocumentSession
    {
        public IEnumerator<StreamResult<T>> Stream<T>(IQueryable<T> query)
        {
            var queryProvider = (IRavenQueryProvider)query.Provider;
            var docQuery = queryProvider.ToDocumentQuery<T>(query.Expression);
            return Stream(docQuery);
        }

        public IEnumerator<StreamResult<T>> Stream<T>(IQueryable<T> query, out StreamQueryStatistics streamQueryStats)
        {
            var queryProvider = (IRavenQueryProvider)query.Provider;
            var docQuery = queryProvider.ToDocumentQuery<T>(query.Expression);
            return Stream(docQuery, out streamQueryStats);
        }

        public IEnumerator<StreamResult<T>> Stream<T>(IDocumentQuery<T> query)
        {
            var streamOperation = new StreamOperation(this);
            var command = streamOperation.CreateRequest(query.GetIndexQuery());

            RequestExecutor.Execute(command, Context, sessionInfo: _sessionInfo);
            var result = streamOperation.SetResult(command.Result);

            return YieldResults(query, result);
        }

        public IEnumerator<StreamResult<T>> Stream<T>(IRawDocumentQuery<T> query)
        {
            return Stream((IDocumentQuery<T>)query);
        }

        public IEnumerator<StreamResult<T>> Stream<T>(IRawDocumentQuery<T> query, out StreamQueryStatistics streamQueryStats)
        {
            return Stream((IDocumentQuery<T>)query, out streamQueryStats);
        }

        public IEnumerator<StreamResult<T>> Stream<T>(IDocumentQuery<T> query, out StreamQueryStatistics streamQueryStats)
        {
            var stats = new StreamQueryStatistics();
            var streamOperation = new StreamOperation(this, stats);
            var command = streamOperation.CreateRequest(query.GetIndexQuery());

            RequestExecutor.Execute(command, Context, sessionInfo: _sessionInfo);
            var result = streamOperation.SetResult(command.Result);
            streamQueryStats = stats;

            return YieldResults(query, result);
        }

        private IEnumerator<StreamResult<T>> YieldResults<T>(IDocumentQuery<T> query, IEnumerator<BlittableJsonReaderObject> enumerator)
        {
            using (enumerator)
            {
                var documentQuery = ((DocumentQuery<T>)query);
                var fieldsToFetch = documentQuery.FieldsToFetchToken;
                var isProjectInto = documentQuery.IsProjectInto;

                while (enumerator.MoveNext())
                {
                    using (var json = enumerator.Current)
                    {
                        query.InvokeAfterStreamExecuted(json);

                        yield return CreateStreamResult<T>(json, fieldsToFetch, isProjectInto);
                    }
                }
            }
        }

        public void StreamInto<T>(IRawDocumentQuery<T> query, Stream output)
        {
            StreamInto((IDocumentQuery<T>)query, output);
        }

        public void StreamInto<T>(IDocumentQuery<T> query, Stream output)
        {
            var streamOperation = new StreamOperation(this);
            var command = streamOperation.CreateRequest(query.GetIndexQuery());

            RequestExecutor.Execute(command, Context, sessionInfo: _sessionInfo);

            using (command.Result.Response)
            using (command.Result.Stream)
            {
                command.Result.Stream.CopyTo(output);
            }
        }

        private StreamResult<T> CreateStreamResult<T>(BlittableJsonReaderObject json, FieldsToFetchToken fieldsToFetch, bool isProjectInto)
        {
            var metadata = json.GetMetadata();
            var changeVector = metadata.GetChangeVector();
            //MapReduce indexes return reduce results that don't have @id property
            metadata.TryGetId(out string id);

            var entity = QueryOperation.Deserialize<T>(id, json, metadata, fieldsToFetch, true, this, isProjectInto);

            var streamResult = new StreamResult<T>
            {
                ChangeVector = changeVector,
                Id = id,
                Document = entity,
                Metadata = new MetadataAsDictionary(metadata)
            };
            return streamResult;
        }

        public IEnumerator<StreamResult<T>> Stream<T>(string startsWith, string matches = null, int start = 0, int pageSize = int.MaxValue,
             string startAfter = null)
        {
            var streamOperation = new StreamOperation(this);

            var command = streamOperation.CreateRequest(startsWith, matches, start, pageSize, null, startAfter);
            RequestExecutor.Execute(command, Context, sessionInfo: _sessionInfo);
            using (var result = streamOperation.SetResult(command.Result))
            {
                while (result.MoveNext())
                {
                    using (var json = result.Current)
                    {
                        yield return CreateStreamResult<T>(json, null, isProjectInto: false);
                    }
                }
            }
        }
    }

}
