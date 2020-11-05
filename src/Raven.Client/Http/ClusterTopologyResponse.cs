﻿using System.Collections.Generic;

namespace Raven.Client.Http
{
    public class ClusterTopologyResponse
    {
        public string Leader;
        public string NodeTag;
        public ClusterTopology Topology;
        public long Etag;
        public Dictionary<string, NodeStatus> Status;
    }
}
