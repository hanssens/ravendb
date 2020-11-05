﻿using System;
using Raven.Client.Documents.Session;
using Raven.Client.Json;

namespace Raven.Client.Documents.Operations.CompareExchange
{
    public class CompareExchangeValue<T> : ICompareExchangeValue
    {
        public string Key { get; }
        public long Index { get; internal set; }
        public T Value { get; set; }

        public IMetadataDictionary Metadata => _metadataAsDictionary ??= new MetadataAsDictionary();

        private IMetadataDictionary _metadataAsDictionary;

        private bool HasMetadata => _metadataAsDictionary != null;

        string ICompareExchangeValue.Key => Key;

        long ICompareExchangeValue.Index { get => Index; set => Index = value; }

        object ICompareExchangeValue.Value => Value;

        IMetadataDictionary ICompareExchangeValue.Metadata => Metadata;
        bool ICompareExchangeValue.HasMetadata => HasMetadata;

        public CompareExchangeValue(string key, long index, T value, IMetadataDictionary metadata = null)
        {
            Key = key;
            Index = index;
            Value = value;
            _metadataAsDictionary = metadata;
        }
    }

    internal interface ICompareExchangeValue
    {
        public string Key { get; }
        public long Index { get; internal set; }
        public object Value { get; }
        public IMetadataDictionary Metadata { get; }
        public bool HasMetadata { get; }
    }
}
