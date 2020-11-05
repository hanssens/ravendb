﻿namespace Raven.Client.Documents.Queries.MoreLikeThis
{
    public class MoreLikeThisOptions
    {
        public const int DefaultMaximumNumberOfTokensParsed = 5000;
        public const int DefaultMinimumTermFrequency = 2;
        public const int DefaultMinimumDocumentFrequency = 5;
        public const int DefaultMaximumDocumentFrequency = int.MaxValue;
        public const bool DefaultBoost = false;
        public const float DefaultBoostFactor = 1;
        public const int DefaultMinimumWordLength = 0;
        public const int DefaultMaximumWordLength = 0;
        public const int DefaultMaximumQueryTerms = 25;

        internal static MoreLikeThisOptions Default = new MoreLikeThisOptions();

        /// <summary>
        ///     Ignore terms with less than this frequency in the source doc. Default is 2.
        /// </summary>
        public int? MinimumTermFrequency { get; set; }

        /// <summary>
        ///     Return a Query with no more than this many terms. Default is 25.
        /// </summary>
        public int? MaximumQueryTerms { get; set; }

        /// <summary>
        ///     The maximum number of tokens to parse in each example doc field that is not stored with TermVector support. Default
        ///     is 5000.
        /// </summary>
        public int? MaximumNumberOfTokensParsed { get; set; }

        /// <summary>
        ///     Ignore words less than this length or if 0 then this has no effect. Default is 0.
        /// </summary>
        public int? MinimumWordLength { get; set; }

        /// <summary>
        ///     Ignore words greater than this length or if 0 then this has no effect. Default is 0.
        /// </summary>
        public int? MaximumWordLength { get; set; }

        /// <summary>
        ///     Ignore words which do not occur in at least this many documents. Default is 5.
        /// </summary>
        public int? MinimumDocumentFrequency { get; set; }

        /// <summary>
        ///     Ignore words which occur in more than this many documents. Default is Int32.MaxValue.
        /// </summary>
        public int? MaximumDocumentFrequency { get; set; }

        /// <summary>
        ///     Ignore words which occur in more than this percentage of documents.
        /// </summary>
        public int? MaximumDocumentFrequencyPercentage { get; set; }

        /// <summary>
        ///     Boost terms in query based on score. Default is false.
        /// </summary>
        public bool? Boost { get; set; }

        /// <summary>
        ///     Boost factor when boosting based on score. Default is 1.
        /// </summary>
        public float? BoostFactor { get; set; }

        /// <summary>
        ///     The document id containing the custom stop words
        /// </summary>
        public string StopWordsDocumentId { get; set; }

        /// <summary>
        ///     The fields to compare
        /// </summary>
        public string[] Fields { get; set; }
    }
}
