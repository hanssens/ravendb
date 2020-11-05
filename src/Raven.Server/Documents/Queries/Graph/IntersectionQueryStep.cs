﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Raven.Server.ServerWide;
using Sparrow;
using static Raven.Server.Documents.Queries.GraphQueryRunner;

namespace Raven.Server.Documents.Queries.Graph
{
    public class IntersectionQueryStep<TOp> : IGraphQueryStep
        where TOp : struct, ISetOp
    {
        private Dictionary<long, List<Match>> _tempIntersect = new Dictionary<long, List<Match>>();
        private HashSet<string> _unionedAliases;
        private List<string> _intersectedAliases;
        private IGraphQueryStep _left;
        private readonly IGraphQueryStep _right;
        private readonly List<Match> _results = new List<Match>();
        private int _index = -1;
        private bool _returnEmptyIfLeftEmpty;
        private bool _returnEmptyIfRightEmpty;
        private OperationCancelToken _token;

        public IGraphQueryStep Left => _left;
        public IGraphQueryStep Right => _right;

        public IntersectionQueryStep(IGraphQueryStep left, IGraphQueryStep right, OperationCancelToken token, bool returnEmptyIfRightEmpty = false, bool returnEmptyIfLeftEmpty = true)
        {
            _returnEmptyIfLeftEmpty = returnEmptyIfLeftEmpty;
            _returnEmptyIfRightEmpty = returnEmptyIfRightEmpty;
            _unionedAliases = new HashSet<string>();
            _unionedAliases.UnionWith(left.GetAllAliases());
            _unionedAliases.UnionWith(right.GetAllAliases());

            var tmpIntersection = new HashSet<string>(left.GetAllAliases());
            tmpIntersection.IntersectWith(right.GetAllAliases());

            _intersectedAliases = tmpIntersection.ToList();

            _left = left;
            _right = right;
            _token = token;
        }

        public bool IsEmpty()
        {
            return _results.Count == 0;
        }

        public bool CollectIntermediateResults { get; set; }

        public List<Match> IntermediateResults => CollectIntermediateResults ? _results : new List<Match>();

        public IGraphQueryStep Clone()
        {
            return new IntersectionQueryStep<TOp>(_left.Clone(), _right.Clone(), _token, _returnEmptyIfRightEmpty, _returnEmptyIfLeftEmpty)
            {
                CollectIntermediateResults = CollectIntermediateResults
            };

        }

        private void IntersectExpressions()
        {
            _token.ThrowIfCancellationRequested();
            _index = 0;

            if (_returnEmptyIfRightEmpty && _right.IsEmpty())
                return;

            _tempIntersect.Clear();

            var operation = new TOp();
            operation.Set(_left, _right);
            var operationState = new HashSet<Match>();

            if (_intersectedAliases.Count == 0 && !operation.ShouldContinueWhenNoIntersection)
                return; // no matching aliases, so we need to stop when the operation is intersection

            while (_left.GetNext(out var leftMatch))
            {
                _token.ThrowIfCancellationRequested();
                long key = GetMatchHashKey(_intersectedAliases, leftMatch);
                if (_tempIntersect.TryGetValue(key, out var matches) == false)
                    _tempIntersect[key] = matches = new List<Match>(); // TODO: pool these
                matches.Add(leftMatch);
            }


            while (_right.GetNext(out var rightMatch))
            {
                _token.ThrowIfCancellationRequested();
                long key = GetMatchHashKey(_intersectedAliases, rightMatch);

                if (_tempIntersect.TryGetValue(key, out var matchesFromLeft) == false)
                {
                    if (operation.ShouldContinueWhenNoIntersection)
                        operationState.Add(rightMatch);
                    continue; // nothing matched, can skip
                }

                for (int i = 0; i < matchesFromLeft.Count; i++)
                {
                    _token.ThrowIfCancellationRequested();
                    var leftMatch = matchesFromLeft[i];
                    var allIntersectionsMatch = true;
                    for (int j = 0; j < _intersectedAliases.Count; j++)
                    {
                        var intersect = _intersectedAliases[j];
                        if (!leftMatch.TryGetAliasId(intersect, out var x) ||
                            !rightMatch.TryGetAliasId(intersect, out var y) ||
                            x != y)
                        {
                            allIntersectionsMatch = false;
                            break;
                        }
                    }

                    operation.Op(_results, leftMatch, rightMatch, allIntersectionsMatch, operationState);
                }
            }

            operation.Complete(_results, _tempIntersect, operationState, _token);
        }



        private static long GetMatchHashKey(List<string> intersectedAliases, Match match)
        {
            long key = 0L;
            for (int i = 0; i < intersectedAliases.Count; i++)
            {
                var alias = intersectedAliases[i];

                if (match.TryGetAliasId(alias, out long aliasId) == false)
                    aliasId = -i;

                key = Hashing.Combine(key, aliasId);
            }

            return key;
        }

        public ValueTask Initialize()
        {
            _token.ThrowIfCancellationRequested();
            if (_index != -1)
                return default;

            var leftTask = _left.Initialize();
            if (leftTask.IsCompleted == false)
            {
                return new ValueTask(CompleteLeftInitializationAsync(leftTask));
            }

            return CompleteInitializationAfterLeft();
        }

        private ValueTask CompleteInitializationAfterLeft()
        {
            _token.ThrowIfCancellationRequested();
            //At this point we know we are not going to yield results we can skip running right hand side
            if (_returnEmptyIfLeftEmpty && _left.IsEmpty())
            {
                _index = 0;
                return default;
            }

            var rightTask = _right.Initialize();
            if (rightTask.IsCompleted == false)
            {
                return new ValueTask(CompleteRightInitializationAsync(rightTask));
            }
            IntersectExpressions();
            return default;
        }

        private async Task CompleteRightInitializationAsync(ValueTask rightTask)
        {
            _token.ThrowIfCancellationRequested();
            await rightTask;
            IntersectExpressions();
        }

        private async Task  CompleteLeftInitializationAsync(ValueTask leftTask)
        {
            _token.ThrowIfCancellationRequested();
            await leftTask;
            await CompleteInitializationAfterLeft();
        }

        public HashSet<string> GetAllAliases()
        {
            return _unionedAliases;
        }

        public string GetOutputAlias()
        {
            return _right.GetOutputAlias();
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

        public List<Match> GetById(string id)
        {
            throw new NotSupportedException("Cannot get a match by id from an edge");
        }

        public void Analyze(Match match, GraphQueryRunner.GraphDebugInfo graphDebugInfo)
        {
            _left.Analyze(match, graphDebugInfo);
            _right.Analyze(match, graphDebugInfo);
        }

        public ISingleGraphStep GetSingleGraphStepExecution()
        {
            throw new System.NotSupportedException("Cannot get single step results from an intersection operation");
        }
    }
}
