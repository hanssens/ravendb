﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Raven.Server.Documents.Indexes.Static.Extensions;

namespace Raven.Server.Documents.Indexes.Static.Roslyn.Rewriters
{
    internal class CaptureSelectNewFieldNamesVisitor : CSharpSyntaxRewriter
    {
        private bool? _isQueryExpression;

        public HashSet<CompiledIndexField> Fields;

        public static HashSet<string> KnownMethodsToInspect = new HashSet<string>
        {
            "Select",
            "SelectMany",
            "Boost",
            "GroupBy",
            "OrderBy",
            "Distinct",
            "Where"
        };

        public override SyntaxNode Visit(SyntaxNode node)
        {
            // if we have query expression then we can only look for fields in query body
            // if we have invocation expression then we can only look for fields in invocation
            // LINQ syntax would be invalid otherwise
            if (_isQueryExpression.HasValue == false)
                _isQueryExpression = node is QueryExpressionSyntax;

            return base.Visit(node);
        }

        public override SyntaxNode VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            if (Fields != null)
                return node;

            var mae = node.Expression as MemberAccessExpressionSyntax;
            if (mae == null)
                return Visit(node.Expression);

            var methodName = mae.Name.Identifier.Text;

            if (KnownMethodsToInspect.Contains(methodName) == false)
                return Visit(node.Expression);

            // even when using query expression we can wrap everything in a Boost method
            if (_isQueryExpression == null || _isQueryExpression == false || methodName == nameof(DynamicExtensionMethods.Boost))
                CaptureFieldNames(node, x => x.VisitInvocationExpression(node));

            return node;
        }

        public override SyntaxNode VisitQueryBody(QueryBodySyntax node)
        {
            if (Fields != null || (_isQueryExpression.HasValue && _isQueryExpression == false))
                return node;

            CaptureFieldNames(node, x => x.VisitQueryBody(node));

            return node;
        }

        private void CaptureFieldNames(SyntaxNode node, Action<CaptureDictionaryFieldsNamesVisitor> visitDictionaryNodeExpression)
        {
            var nodes = node.DescendantNodes(descendIntoChildren: syntaxNode =>
            {
                if (syntaxNode is AnonymousObjectCreationExpressionSyntax ||
                    syntaxNode is ObjectCreationExpressionSyntax oce && CaptureDictionaryFieldsNamesVisitor.IsDictionaryObjectCreationExpression(oce))
                {
                    return false;
                }
                return true;
            }).ToList();

            var lastObjectCreation = nodes.LastOrDefault(x => x.IsKind(SyntaxKind.AnonymousObjectCreationExpression) ||
                                                              x.IsKind(SyntaxKind.ObjectCreationExpression) &&
                                                              x is ObjectCreationExpressionSyntax oce &&
                                                              CaptureDictionaryFieldsNamesVisitor.IsDictionaryObjectCreationExpression(oce));

            if (lastObjectCreation is AnonymousObjectCreationExpressionSyntax lastAnonymousObjectCreation)
            {
                VisitAnonymousObjectCreationExpression(lastAnonymousObjectCreation);
            }
            else if (lastObjectCreation is ObjectCreationExpressionSyntax oce && CaptureDictionaryFieldsNamesVisitor.IsDictionaryObjectCreationExpression(oce))
            {
                var dictVisitor = new CaptureDictionaryFieldsNamesVisitor();

                visitDictionaryNodeExpression(dictVisitor);

                Fields = dictVisitor.Fields;
            }
            else
            {
                var selectClause = nodes.LastOrDefault(x => x.IsKind(SyntaxKind.SelectClause));

                ThrowIndexingFunctionMustReturnAnonymousObjectOrDictionary(maybeNew: selectClause != null);
            }
        }

        public override SyntaxNode VisitAnonymousObjectCreationExpression(AnonymousObjectCreationExpressionSyntax node)
        {
            if (Fields != null)
                return node;

            Fields = RewritersHelper.ExtractFields(node);

            return node;
        }

        public override SyntaxNode VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
        {
            if (Fields != null)
                return node;

            if (CaptureDictionaryFieldsNamesVisitor.IsDictionaryObjectCreationExpression(node))
            {
                var dictVisitor = new CaptureDictionaryFieldsNamesVisitor();
                dictVisitor.VisitObjectCreationExpression(node);
                Fields = dictVisitor.Fields;
            }

            return node;
        }

        private static void ThrowIndexingFunctionMustReturnAnonymousObjectOrDictionary(bool maybeNew)
        {
            var message = $"Indexing function must return an anonymous object or {CaptureDictionaryFieldsNamesVisitor.SupportedGenericDictionaryType}.";
            if (maybeNew)
                message += " Did you forget to add 'new' in last select clause?";

            throw new InvalidOperationException(message);
        }

        public void Reset()
        {
            Fields = null;
            _isQueryExpression = new bool?();
        }
    }
}
