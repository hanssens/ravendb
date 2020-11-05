﻿using System.Collections.Generic;
using Raven.Client.Util;
using Sparrow.Json.Parsing;

namespace Raven.Client.Documents.Operations
{
    public class CollectionStatistics
    {
        public CollectionStatistics()
        {
            Collections = new Dictionary<string, long>();
        }

        public int CountOfDocuments { get; set; }
        public int CountOfConflicts { get; set; }

        public Dictionary<string, long> Collections { get; set; }
    }

    public class DetailedCollectionStatistics
    {
        public DetailedCollectionStatistics()
        {
            Collections = new Dictionary<string, CollectionDetails>();
        }

        public long CountOfDocuments { get; set; }
        public long CountOfConflicts { get; set; }

        public Dictionary<string, CollectionDetails> Collections { get; set; }
    }

    public class CollectionDetails : IDynamicJson
    {
        public string Name { get; set; }
        public long CountOfDocuments { get; set; }
        public Size Size { get; set; }

        public DynamicJsonValue ToJson()
        {
            return new DynamicJsonValue
            {
                [nameof(Name)] = Name,
                [nameof(CountOfDocuments)] = CountOfDocuments,
                [nameof(Size)] = new DynamicJsonValue
                {
                    [nameof(Size.HumaneSize)] = Size.HumaneSize,
                    [nameof(Size.SizeInBytes)] = Size.SizeInBytes
                }
            };
        }
    }
}
