﻿using System;
using Sparrow.Json;

namespace Raven.Client.Documents.Commands
{  
    public class GetConflictsResult
    {
        public string Id { get; set; }

        public Conflict[] Results { get; internal set; }

        public long LargestEtag { get; set; } //etag of the conflict itself

        public class Conflict
        {
            public DateTime LastModified { get; set; }

            public string ChangeVector { get; set; }

            public BlittableJsonReaderObject Doc { get; set; }
        }
    }
}
