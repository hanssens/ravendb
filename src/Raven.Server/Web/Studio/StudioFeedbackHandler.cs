﻿using System.Threading.Tasks;
using Raven.Server.Documents.Studio;
using Raven.Server.Json;
using Raven.Server.Routing;
using Sparrow.Json;

namespace Raven.Server.Web.Studio
{
    public class StudioFeedbackHandler : RequestHandler
    {
        [RavenAction("/studio/feedback", "POST", AuthorizationStatus.ValidUser)]
        public async Task Feedback()
        {
            FeedbackForm feedbackForm;

            using (ServerStore.ContextPool.AllocateOperationContext(out JsonOperationContext context))
            {
                var json = context.Read(RequestBodyStream(), "feedback form");
                feedbackForm = JsonDeserializationServer.FeedbackForm(json);
            }

            await ServerStore.FeedbackSender.SendFeedback(feedbackForm).ConfigureAwait(false);

            NoContentStatus();
        }

    }
}