﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Raven.Server.Documents.Queries.AST;
using Raven.Server.ServerWide;
using Raven.Server.ServerWide.Context;
using static Raven.Server.Documents.Queries.GraphQueryRunner;

namespace Raven.Server.Documents.Queries.Graph
{
    public class QueryQueryStep : IGraphQueryStep
    {
        private Query _query;
        private Sparrow.StringSegment _alias;
        private HashSet<string> _aliases;
        private DocumentsOperationContext _context;
        private long? _resultEtag;
        private OperationCancelToken _token;
        private QueryRunner _queryRunner;
        private QueryMetadata _queryMetadata;

        private int _index;
        private List<Match> _results = new List<Match>();
        private Dictionary<string, Match> _resultsById = new Dictionary<string, Match>(StringComparer.OrdinalIgnoreCase);

        public QueryQueryStep(QueryRunner queryRunner, Sparrow.StringSegment alias,Query query, QueryMetadata queryMetadata, DocumentsOperationContext documentsContext, long? existingResultEtag,
            OperationCancelToken token)
        {
            _query = query;
            _alias = alias;
            _aliases = new HashSet<string> { _alias };
            _queryRunner = queryRunner;
            _queryMetadata = queryMetadata;
            _context = documentsContext;
            _resultEtag = existingResultEtag;
            _token = token;
        }

        public bool GetNext(out Match match)
        {
            if (_index >= _results.Count)
            {
                match = default;
                return false;
            }
            match = _results[_index++];
            return true;
        }

        public ValueTask Initialize()
        {
            var results = _queryRunner.ExecuteQuery(new IndexQueryServerSide(_queryMetadata),
                  _context, _resultEtag, _token);

            if (results.IsCompleted)
            {
                // most of the time, we complete in a sync fashion
                CompleteInitialization(results.Result);
                return default;
            }

            return CompleteInitializeAsync(results);
        }

        private async ValueTask CompleteInitializeAsync(Task<DocumentQueryResult> results)
        {
            CompleteInitialization(await results);
        }



        private void CompleteInitialization(DocumentQueryResult results)
        {
            foreach (var result in results.Results)
            {
                var match = new Match();
                match.Set(_alias, result);
                _results.Add(match);
                if (result.Id == null)
                    continue;
                _resultsById[result.Id] = match;
            }
        }

        public bool TryGetById(string id, out Match match)
        {
            return _resultsById.TryGetValue(id, out match);
        }

        public string GetOuputAlias()
        {
            return _alias;
        }

        public HashSet<string> GetAllAliases()
        {
            return _aliases;
        }
    }
}
