﻿using System;
using System.Collections.Generic;
using Raven.Client.Documents.Operations.TimeSeries;
using Raven.Client.Documents.Session.Loaders;
using Raven.Server.Documents.Handlers;


namespace Raven.Server.Documents.Queries.TimeSeries
{
    public class TimeSeriesIncludesField
    {
        public TimeSeriesIncludesField()
        {
            TimeSeries = new Dictionary<string, HashSet<TimeSeriesRange>>(StringComparer.OrdinalIgnoreCase);
        }

        public readonly Dictionary<string, HashSet<TimeSeriesRange>> TimeSeries;

        public void AddTimeSeries(string timeseries, string fromStr, string toStr, string sourcePath = null)
        {
            var key = sourcePath ?? string.Empty;
            if (TimeSeries.TryGetValue(key, out var hashSet) == false)
            {
                TimeSeries[key] = hashSet = new HashSet<TimeSeriesRange>(TimeSeriesRangeComparer.Instance);
            }

            hashSet.Add(new TimeSeriesRange
            {
                Name = timeseries,
                From = string.IsNullOrEmpty(fromStr) ? DateTime.MinValue : TimeSeriesHandler.ParseDate(fromStr, timeseries),
                To = string.IsNullOrEmpty(toStr) ? DateTime.MaxValue : TimeSeriesHandler.ParseDate(toStr, timeseries)
            });
        }
    }
}
