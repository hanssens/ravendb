﻿using System;
using System.Collections.Generic;
using Raven.Client.Documents.Conventions;
using Raven.Client.Documents.Operations.TimeSeries;
using Raven.Client.Documents.Session;
using Sparrow.Json;
using Sparrow.Json.Parsing;

namespace Raven.Client.Documents.Commands.Batches
{
    public class TimeSeriesBatchCommandData : ICommandData
    {
        public TimeSeriesBatchCommandData(string documentId, string name, List<TimeSeriesOperation.AppendOperation> appends, List<TimeSeriesOperation.DeleteOperation> deletes)
        {
            if (string.IsNullOrWhiteSpace(documentId))
                throw new ArgumentNullException(nameof(documentId));
            
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(nameof(name));

            Id = documentId;
            Name = name;

            TimeSeries = new TimeSeriesOperation
            {
                Name = name,
            };

            if (appends != null)
            {
                foreach (var appendOperation in appends)
                {
                    TimeSeries.Append(appendOperation);
                }
            }

            if (deletes != null)
            {
                foreach (var deleteOperation in deletes)
                {
                    TimeSeries.Delete(deleteOperation);
                }
            }
        }

        public string Id { get; set; }

        public string Name { get; set; }

        public string ChangeVector => null;

        public CommandType Type => CommandType.TimeSeries;

        public TimeSeriesOperation TimeSeries { get; }

        public DynamicJsonValue ToJson()
        {
            var result = new DynamicJsonValue
            {
                [nameof(Id)] = Id,
                [nameof(TimeSeries)] = TimeSeries.ToJson(),
                [nameof(Type)] = Type
            };

            return result;
        }

        public DynamicJsonValue ToJson(DocumentConventions conventions, JsonOperationContext context)
        {
            return ToJson();
        }

        public void OnBeforeSaveChanges(InMemoryDocumentSessionOperations session)
        {
        }
    }
}
