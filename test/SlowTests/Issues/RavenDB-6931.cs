﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FastTests;
using Raven.Client.Documents.Operations;
using Raven.Client.Documents.Operations.Attachments;
using Raven.Tests.Core.Utils.Entities;
using Xunit;
using Xunit.Abstractions;

namespace SlowTests.Issues
{
    public class RavenDB_6931 : RavenTestBase
    {
        public RavenDB_6931(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void PatchAttachmentMetadataShouldThrow()
        {
            using (var store = GetDocumentStore())
            {
                using (var session = store.OpenSession())
                using (var profileStream = new MemoryStream(new byte[] { 1, 2, 3 }))
                {
                    session.Store(new User { Name = "Bob" }, "users/1");
                    session.Advanced.Attachments.Store("users/1", "profile.png", profileStream, "image/png");
                    session.SaveChanges();
                    var command = new PatchOperation.PatchCommand(
                        store.Conventions,
                        session.Advanced.Context,
                        "users/1",
                        null,
                        new PatchRequest
                        {
                            Script = @"this[""@metadata""][""@attachments""] = []"
                        },
                        patchIfMissing: null,
                        skipPatchIfChangeVectorMismatch: false,
                        returnDebugInformation: false,
                        test: false);

                    Assert.Throws<Raven.Client.Exceptions.RavenException>(() => session.Advanced.RequestExecutor.Execute(command, session.Advanced.Context));
                }
            }
        }
    }
}
