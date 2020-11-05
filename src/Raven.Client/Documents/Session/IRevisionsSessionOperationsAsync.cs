//-----------------------------------------------------------------------
// <copyright file="IRevisionsSessionOperationsAsync.cs" company="Hibernating Rhinos LTD">
//     Copyright (c) Hibernating Rhinos LTD. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Raven.Client.Json;

namespace Raven.Client.Documents.Session
{
    /// <summary>
    ///     Revisions advanced async session operations
    /// </summary>
    public interface IRevisionsSessionOperationsAsync
    {
        /// <summary>
        /// Returns all previous document revisions for specified document (with paging).
        /// </summary>
        Task<List<T>> GetForAsync<T>(string id, int start = 0, int pageSize = 25, CancellationToken token = default);

        /// <summary>
        /// Returns all previous document revisions metadata for specified document (with paging).
        /// </summary>
        Task<List<MetadataAsDictionary>> GetMetadataForAsync(string id, int start = 0, int pageSize = 25, CancellationToken token = default);

        /// <summary>
        /// Returns a document revision by change vector.
        /// </summary>
        Task<T> GetAsync<T>(string changeVector, CancellationToken token = default);

        /// <summary>
        /// Returns document revisions by change vectors.
        /// </summary>
        Task<Dictionary<string, T>> GetAsync<T>(IEnumerable<string> changeVectors, CancellationToken token = default);

        /// <summary>
        /// Returns the first revision for this document that happens before or at
        /// the specified date
        /// </summary>
        Task<T> GetAsync<T>(string id, DateTime date, CancellationToken token = default);

        /// <summary>
        /// Make the session create a revision for the specified entity.
        /// Can be used with tracked entities only.
        /// Revision will be created Even If:
        ///    1. Revisions configuration is Not set for the collection
        ///    2. Document was Not modified
        /// </summary>
        void ForceRevisionCreationFor<T>(T entity, ForceRevisionStrategy strategy = ForceRevisionStrategy.Before);

        /// <summary>
        /// Make the session create a revision for the specified document id.
        /// Revision will be created Even If:
        ///    1. Revisions configuration is Not set for the collection
        ///    2. Document was Not modified
        /// </summary>
        /// <param name="id"></param>
        void ForceRevisionCreationFor(string id, ForceRevisionStrategy strategy = ForceRevisionStrategy.Before);
    }
}
