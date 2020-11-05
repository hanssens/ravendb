//-----------------------------------------------------------------------
// <copyright file="ServerError.cs" company="Hibernating Rhinos LTD">
//     Copyright (c) Hibernating Rhinos LTD. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;

namespace Raven.Client.Documents.Indexes
{
    public class IndexingError
    {
        public string Error { get; set; }
        public DateTime Timestamp { get; set; }
        public string Document { get; set; }
        public string Action { get; set; }

        public override string ToString()
        {
            return $"Error: {Error}, Document: {Document}, Action: {Action}";
        }
    }
}
