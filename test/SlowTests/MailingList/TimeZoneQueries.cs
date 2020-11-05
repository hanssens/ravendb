﻿using System;
using System.Linq;
using FastTests;
using Xunit;
using Xunit.Abstractions;

namespace SlowTests.MailingList
{
    public class TimeZoneQueries : RavenTestBase
    {
        public TimeZoneQueries(ITestOutputHelper output) : base(output)
        {
        }

        private class Item
        {
            public DateTimeOffset At { get; set; }
        }

        [Fact]
        public void CanQueryAtSpecificTimeZone()
        {
            using(var store = GetDocumentStore())
            {
                var notTheCurrentTimeZone = GetDifferentTimeZoneThanCurrentTimeZone();
                using (var session = store.OpenSession())
                {
                    session.Store(new Item
                    {
                        At = new DateTimeOffset(new DateTime(2011,11,11,11,0,0),notTheCurrentTimeZone)
                    });
                    session.SaveChanges();
                }

                using (var session = store.OpenSession())
                {
                    var dateTimeOffset = new DateTimeOffset(new DateTime(2011, 11, 11, 13, 0, 0), notTheCurrentTimeZone.Add(TimeSpan.FromHours(2)));
                    Assert.NotEmpty(session.Query<Item>()		// exact match, different timezone
                        .Where(x => x.At == dateTimeOffset));

                    Assert.NotEmpty(session.Query<Item>()		// greater than equal match, same timezone
                        .Where(x => x.At > new DateTimeOffset(new DateTime(2011, 11, 11, 10, 0, 0), notTheCurrentTimeZone)));

                    Assert.NotEmpty(session.Query<Item>()		// less than match, different timezone
                        .Where(x => x.At < new DateTimeOffset(new DateTime(2011, 11, 11, 10, 0, 0), notTheCurrentTimeZone.Add(TimeSpan.FromHours(-9))))); 
                }
            }
        }

        private static TimeSpan GetDifferentTimeZoneThanCurrentTimeZone()
        {
            var differentTimeZoneThanCurrentTimeZone = TimeZoneInfo.Local.GetUtcOffset(DateTime.Now);
            if(differentTimeZoneThanCurrentTimeZone.Hours == 3)
                return TimeSpan.FromHours(1);
            return TimeSpan.FromHours(3);
        }
    }
}
