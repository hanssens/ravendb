﻿using System;
using System.Collections.Generic;
using System.Text;
using Sparrow;

namespace Raven.Server.Documents.Queries.AST
{
    public class GraphQuery
    {
        public HashSet<StringSegment> RecursiveMatches = new HashSet<StringSegment>(StringSegmentComparer.Ordinal);

        public Dictionary<StringSegment, (bool implicitAlias, Query withQuery)> WithDocumentQueries = new Dictionary<StringSegment, (bool implicitAlias, Query)>();

        public Dictionary<StringSegment, WithEdgesExpression> WithEdgePredicates = new Dictionary<StringSegment, WithEdgesExpression>();

        public QueryExpression MatchClause;

        public QueryExpression Where;

        public List<QueryExpression> Include;

        public List<(QueryExpression Expression, OrderByFieldType FieldType, bool Ascending)> OrderBy;

        public Dictionary<string, DeclaredFunction> DeclaredFunctions;

        public string QueryText;

        public (string FunctionText, Esprima.Ast.Program Program) SelectFunctionBody;

        public bool TryAddFunction(DeclaredFunction func)
        {
            if (DeclaredFunctions == null)
                DeclaredFunctions = new Dictionary<string, DeclaredFunction>(StringComparer.Ordinal);

            return DeclaredFunctions.TryAdd(func.Name, func);
        }

        public bool HasAlias(StringSegment alias)
        {
            if (WithDocumentQueries.ContainsKey(alias))
                return true;
            if (WithEdgePredicates.ContainsKey(alias))
                return true;
            if (RecursiveMatches.Contains(alias))
                return true;
            return false;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            new StringQueryVisitor(sb).VisitGraph(this);
            return sb.ToString();
        }
    }
}
