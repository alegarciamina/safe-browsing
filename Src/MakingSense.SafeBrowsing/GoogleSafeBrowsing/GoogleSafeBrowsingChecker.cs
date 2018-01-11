﻿#if !(NETSTANDARD1_0)
using MakingSense.SafeBrowsing.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MakingSense.SafeBrowsing.GoogleSafeBrowsing
{
    /// <summary>
    /// Google Safe Browsing implementation
    /// </summary>
    public class GoogleSafeBrowsingChecker : IUrlChecker
    {
        private readonly GoogleSafeBrowsingDatabase _database;

        /// <summary>
        /// Create a new instance
        /// </summary>
        /// <param name="database"></param>
        public GoogleSafeBrowsingChecker(GoogleSafeBrowsingDatabase database)
        {
            _database = database;
        }

        /// <inheritdoc />
        public SafeBrowsingStatus Check(string url)
        {
            var canonicalUrl = CanonicalUrl.Create(url);
            var patterns = canonicalUrl.GeneratePrefixSuffixPatterns();

            var prefixHashes = patterns.Select(x=> CryptographyHelper.GenerateSHA256(x,4));

            if(_database.FindHashes(ThreatType.SOCIAL_ENGINEERING, prefixHashes).Count() > 0)
            {
                return new SafeBrowsingStatus(url, SafeBrowsing.ThreatType.Phishing);
            }
            else if (_database.FindHashes(ThreatType.MALWARE, prefixHashes).Count() > 0)
            {
                return new SafeBrowsingStatus(url, SafeBrowsing.ThreatType.Malware);
            }
            else if (_database.FindHashes(ThreatType.UNWANTED_SOFTWARE, prefixHashes).Count() > 0)
            {
                return new SafeBrowsingStatus(url, SafeBrowsing.ThreatType.Unwanted);
            }

            return new SafeBrowsingStatus(url, SafeBrowsing.ThreatType.NoThreat);
        }

        /// <inheritdoc />
        public Task<SafeBrowsingStatus> CheckAsync(string url) =>
            TaskUtilities.FromResult(Check(url));
    }
}
#endif
