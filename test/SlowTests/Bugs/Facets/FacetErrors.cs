﻿using System;
using System.Collections.Generic;
using Xunit;
using System.Linq;
using Raven.Client.Documents;
using Raven.Client.Documents.Queries.Facets;
using Raven.Client.Exceptions;
using Xunit.Abstractions;

namespace SlowTests.Bugs.Facets
{
    public class FacetErrors : FacetTestBase
    {
        public FacetErrors(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void PrestonThinksFacetsShouldNotHideOtherErrors()
        {
            var cameras = GetCameras(30);
            var now = DateTime.Now;

            //putting some in the past and some in the future
            for (int x = 0; x < cameras.Count; x++)
            {
                cameras[x].DateOfListing = now.AddDays(x - 15);
            }


            var dates = new List<DateTime>{
                now.AddDays(-10),
                now.AddDays(-7),
                now.AddDays(0),
                now.AddDays(7)
            };

            using (var store = GetDocumentStore())
            {
                CreateCameraCostIndex(store);

                InsertCameraData(store , cameras);

                var facets = new List<RangeFacet>{
                    new RangeFacet
                    {
                        Ranges = new List<string>{
                            string.Format("[NULL TO {0:yyyy-MM-ddTHH-mm-ss.fffffff}]", dates[0]),
                            string.Format("[{0:yyyy-MM-ddTHH-mm-ss.fffffff} TO {1:yyyy-MM-ddTHH-mm-ss.fffffff}]", dates[0], dates[1]),
                            string.Format("[{0:yyyy-MM-ddTHH-mm-ss.fffffff} TO {1:yyyy-MM-ddTHH-mm-ss.fffffff}]", dates[1], dates[2]),
                            string.Format("[{0:yyyy-MM-ddTHH-mm-ss.fffffff} TO {1:yyyy-MM-ddTHH-mm-ss.fffffff}]", dates[2], dates[3]),
                            string.Format("[{0:yyyy-MM-ddTHH-mm-ss.fffffff} TO NULL]", dates[3])
                        }
                    }
                };

                var session = store.OpenSession();
                //CameraCostIndex does not include zoom, bad index specified.
                var query = session.Query<Camera, CameraCostIndex>().Where(x => x.Zoom > 3);
                Assert.Throws<RavenException>(() => query.ToList());
                Assert.Throws<RavenException>(() => query.AggregateBy(facets).Execute());
            }
        }
    }
}
