﻿//-----------------------------------------------------------------------
// <copyright file="DocumentSessionAttachmentsAsync.cs" company="Hibernating Rhinos LTD">
//     Copyright (c) Hibernating Rhinos LTD. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Raven.Client.Documents.Attachments;
using Raven.Client.Documents.Commands;
using Raven.Client.Documents.Operations.Attachments;

namespace Raven.Client.Documents.Session
{
    /// <summary>
    /// Implements Unit of Work for accessing the RavenDB server
    /// </summary>
    public class DocumentSessionAttachmentsAsync : DocumentSessionAttachmentsBase, IAttachmentsSessionOperationsAsync
    {
        public DocumentSessionAttachmentsAsync(InMemoryDocumentSessionOperations session) : base(session)
        {
        }

        public async Task<bool> ExistsAsync(string documentId, string name, CancellationToken token = default)
        {
            using (Session.AsyncTaskHolder())
            {
                var command = new HeadAttachmentCommand(documentId, name, null);
                await RequestExecutor.ExecuteAsync(command, Context, sessionInfo: SessionInfo, token).ConfigureAwait(false);
                return command.Result != null;
            }
        }

        public async Task<AttachmentResult> GetAsync(string documentId, string name, CancellationToken token = default)
        {
            using (Session.AsyncTaskHolder())
            {
                var operation = new GetAttachmentOperation(documentId, name, AttachmentType.Document, null);
                return await Session.Operations.SendAsync(operation, sessionInfo: SessionInfo, token).ConfigureAwait(false);
            }
        }

        public async Task<AttachmentResult> GetAsync(object entity, string name, CancellationToken token = default)
        {
            using (Session.AsyncTaskHolder())
            {
                if (Session.DocumentsByEntity.TryGetValue(entity, out DocumentInfo document) == false)
                    ThrowEntityNotInSessionOrMissingId(entity);

                var operation = new GetAttachmentOperation(document.Id, name, AttachmentType.Document, null);
                return await Session.Operations.SendAsync(operation, sessionInfo: SessionInfo, token).ConfigureAwait(false);
            }
        }

        public Task<IEnumerator<AttachmentEnumeratorResult>> GetAsync(IEnumerable<AttachmentRequest> attachments, CancellationToken token = default)
        {
            var operation = new GetAttachmentsOperation(attachments, AttachmentType.Document);
            return Session.Operations.SendAsync(operation, SessionInfo, token);
        }

        public async Task<AttachmentResult> GetRevisionAsync(string documentId, string name, string changeVector, CancellationToken token = default)
        {
            using (Session.AsyncTaskHolder())
            {
                var operation = new GetAttachmentOperation(documentId, name, AttachmentType.Revision, changeVector);
                return await Session.Operations.SendAsync(operation, sessionInfo: SessionInfo, token).ConfigureAwait(false);
            }
        }
    }
}
