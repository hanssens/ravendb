using System;
using System.Net;
using System.Net.Http;
using Raven.Client.Documents.Commands.MultiGet;
using Raven.Client.Documents.Conventions;
using Raven.Client.Documents.Queries;
using Raven.Client.Json.Serialization;
using Sparrow.Json;

namespace Raven.Client.Documents.Session.Operations.Lazy
{
    internal class LazyQueryOperation<T> : ILazyOperation
    {
        private readonly DocumentConventions _conventions;
        private readonly QueryOperation _queryOperation;
        private readonly Action<QueryResult> _afterQueryExecuted;

        public LazyQueryOperation(DocumentConventions conventions, QueryOperation queryOperation, Action<QueryResult> afterQueryExecuted)
        {
            _conventions = conventions;
            _queryOperation = queryOperation;
            _afterQueryExecuted = afterQueryExecuted;
        }

        public GetRequest CreateRequest(JsonOperationContext ctx)
        {
            return new GetRequest
            {
                CanCacheAggressively = _queryOperation.IndexQuery.DisableCaching == false &&  _queryOperation.IndexQuery.WaitForNonStaleResults == false,
                Url = "/queries",
                Method = HttpMethod.Post,
                Query = $"?queryHash={_queryOperation.IndexQuery.GetQueryHash(ctx)}",
                Content = new IndexQueryContent(_conventions, _queryOperation.IndexQuery)
            };
        }

        public object Result { get; set; }
        public QueryResult QueryResult { get; set; }
        public bool RequiresRetry { get; set; }

        public void HandleResponse(GetResponse response)
        {
            if (response.ForceRetry)
            {
                Result = null;
                RequiresRetry = true;
                return;
            }

            QueryResult queryResult = null;

            if (response.Result != null)
            {
                queryResult = JsonDeserializationClient.QueryResult((BlittableJsonReaderObject)response.Result);

                if (response.StatusCode == HttpStatusCode.NotModified)
                    queryResult.DurationInMs = -1; // taken from cache
            }

            HandleResponse(queryResult, response.Elapsed);
        }

        private void HandleResponse(QueryResult queryResult, TimeSpan duration)
        {
            _queryOperation.EnsureIsAcceptableAndSaveResult(queryResult, duration);

            _afterQueryExecuted?.Invoke(queryResult);
            Result = _queryOperation.Complete<T>();
            QueryResult = queryResult;
        }
    }
}
