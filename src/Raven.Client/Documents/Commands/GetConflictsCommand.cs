﻿using System;
using System.Net.Http;
using Raven.Client.Http;
using Raven.Client.Json.Serialization;
using Sparrow.Json;

namespace Raven.Client.Documents.Commands
{
    public class GetConflictsCommand : RavenCommand<GetConflictsResult>
    {
        private readonly string _id;
        public override bool IsReadRequest => true;

        public GetConflictsCommand(string id)
        {
            _id = id;
        }

        public override HttpRequestMessage CreateRequest(JsonOperationContext ctx, ServerNode node, out string url)
        {
            url = $"{node.Url}/databases/{node.Database}/replication/conflicts?docId={Uri.EscapeDataString(_id)}";
            return new HttpRequestMessage
            {
                Method = HttpMethod.Get
            };
        }

        public override void SetResponse(JsonOperationContext context, BlittableJsonReaderObject response, bool fromCache)
        {
            if (response == null)
                ThrowInvalidResponse();
            Result = JsonDeserializationClient.GetConflictsResult(response);
        }
    }
}
