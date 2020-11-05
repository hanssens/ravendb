﻿using System;
using FastTests;
using Sparrow.Json;
using Xunit;
using Xunit.Abstractions;

namespace SlowTests.Issues
{
    public class RavenDB_10537:RavenTestBase
    {
        public RavenDB_10537(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void TestDecimalNumbers()
        {
            using (var context = JsonOperationContext.ShortTermSingleUse())
            {                    
                var blittable = context.ReadObject(new Sparrow.Json.Parsing.DynamicJsonValue
                {
                    ["Max"] = Decimal.MaxValue,
                    ["Min"] = Decimal.MinValue

                }, "someDoc");

                blittable.TryGet<decimal>("Max", out var max);
                Assert.Equal(max, Decimal.MaxValue);

                blittable.TryGet<decimal>("Min", out var min);
                Assert.Equal(min, Decimal.MinValue);

            }
            
        }
    }
}
