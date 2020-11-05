﻿//-----------------------------------------------------------------------
// <copyright file="SessionDocumentTimeSeries.cs" company="Hibernating Rhinos LTD">
//     Copyright (c) Hibernating Rhinos LTD. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Raven.Client.Documents.Session.TimeSeries;
using Raven.Client.Util;

namespace Raven.Client.Documents.Session
{
    public class SessionDocumentTimeSeries<TValues> : ISessionDocumentTimeSeries, ISessionDocumentRollupTypedTimeSeries<TValues>, ISessionDocumentTypedTimeSeries<TValues> where TValues : new()
    {
        private readonly AsyncSessionDocumentTimeSeries<TimeSeriesEntry> _asyncSessionTimeSeries;

        public SessionDocumentTimeSeries(InMemoryDocumentSessionOperations session, string documentId, string name)
        {
            _asyncSessionTimeSeries = new AsyncSessionDocumentTimeSeries<TimeSeriesEntry>(session, documentId, name);
        }

        public SessionDocumentTimeSeries(InMemoryDocumentSessionOperations session, object entity, string name)
        {
            _asyncSessionTimeSeries = new AsyncSessionDocumentTimeSeries<TimeSeriesEntry>(session, entity, name);
        }

        public void Append(DateTime timestamp, IEnumerable<double> values, string tag = null)
        {
            _asyncSessionTimeSeries.Append(timestamp, values, tag);
        }

        public void Append(DateTime timestamp, double value, string tag = null)
        {
            _asyncSessionTimeSeries.Append(timestamp, value, tag);
        }

        public void Append(DateTime timestamp, TValues value, string tag = null)
        {
            _asyncSessionTimeSeries.Append(timestamp, value, tag);
        }

        public void Append(TimeSeriesEntry<TValues> entry)
        {
            _asyncSessionTimeSeries.Append(entry.Timestamp, entry.Value, entry.Tag);
        }

        public TimeSeriesEntry[] Get(DateTime? from = null, DateTime? to = null, int start = 0, int pageSize = int.MaxValue)
        {
            return AsyncHelpers.RunSync(() => _asyncSessionTimeSeries.GetAsync(from, to, start, pageSize));
        }

        TimeSeriesEntry<TValues>[] ISessionDocumentTypedTimeSeries<TValues>.Get(DateTime? from, DateTime? to, int start, int pageSize)
        {
            return AsyncHelpers.RunSync(() => _asyncSessionTimeSeries.GetAsyncInternal<TimeSeriesEntry<TValues>>(from, to, start, pageSize));
        }

        TimeSeriesRollupEntry<TValues>[] ISessionDocumentRollupTypedTimeSeries<TValues>.Get(DateTime? from, DateTime? to, int start, int pageSize)
        {
            return AsyncHelpers.RunSync(() => _asyncSessionTimeSeries.GetAsyncInternal<TimeSeriesRollupEntry<TValues>>(from, to, start, pageSize));
        }

        public void Append(TimeSeriesRollupEntry<TValues> entry)
        {
            entry.SetValuesFromMembers();
            _asyncSessionTimeSeries.Append(entry.Timestamp, entry.Values, entry.Tag);
        }

        public void Delete(DateTime? from = null, DateTime? to = null)
        {
            _asyncSessionTimeSeries.Delete(from, to);
        }

        public void Delete(DateTime at)
        {
            _asyncSessionTimeSeries.Delete(at);
        }
    }
}
