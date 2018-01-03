﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MakingSense.SafeBrowsing.GoogleSafeBrowsing
{
    /// <summary>
    /// In-memory database to store Google Safe Browsing lists
    /// </summary>
    public class GoogleSafeBrowsingDatabase
    {
        private const string SOCIAL_ENGINEERING = "SOCIAL_ENGINEERING";
        private const string UNWANTED_SOFTWARE = "UNWANTED_SOFTWARE";
        private const string MALWARE = "MALWARE";

        /// <summary>
        /// The minimum duration the client must wait before issuing any update request. 
        /// If this field is not set clients may update as soon as they want.
        /// </summary>
        public TimeSpan? MinimumWaitDuration { get; set; } = null;

        /// <summary>
        /// Last time Google Safe Browsing lists were updated
        /// </summary>
        public DateTimeOffset? Updated { get; set; } = null;

        /// <summary>
        /// Google Safe Browsing lists
        /// </summary>
        public Dictionary<string, SafeBrowsingList> SuspiciousLists { get; set; }

        /// <summary>
        /// Indicate if client is in back-off mode.
        /// <para>Clients that receive an unsuccessful HTTP response 
        /// (that is, any HTTP status code other than 200 OK) must enter back-off mode.
        /// Once in back-off mode, clients must wait the computed time duration before 
        /// they can issue another request to the server.</para>
        /// </summary>
        public bool BackOffMode { get; private set; } = false;

        /// <summary>
        /// Number of consecutive, unsuccessful requests that the client experiences 
        /// (starting with N=1 after the first unsuccessful request)
        /// </summary>
        public int BackOffRetryNumber { get; private set; }

        /// <summary>
        /// Random number between 0 and 1 that needs to be picked after every unsuccessful update.
        /// </summary>
        public double BackOffSeed { get; private set; }

        /// <summary>
        /// Back-off computed time duration before client can issue another request to the server. 
        /// It uses the following formula: MIN((2^(N-1) * 15 minutes) * (RAND + 1), 24 hours)
        /// </summary>
        public TimeSpan? BackOffDuration
        {
            get
            {
                return TimeSpan.FromMinutes(Math.Min(( Math.Pow(2, (BackOffRetryNumber - 1)) * 15) * (BackOffSeed + 1), 24 * 60));
            }
        }

        /// <summary>
        /// Returns true if MinimumWaitDuration has passed since last update or if it is the initial download
        /// </summary>
        public bool AllowRequest
        {
            get
            {
                if (BackOffMode)
                {
                    return !(Updated.HasValue && BackOffDuration.HasValue && Updated.Value.Add(BackOffDuration.Value) >= DateTimeOffset.Now);
                }

                return !(Updated.HasValue && MinimumWaitDuration.HasValue && Updated.Value.Add(MinimumWaitDuration.Value) >= DateTimeOffset.Now);
            }
        }

        /// <summary>
        /// Initialize an instance with default SuspiciousLists
        /// </summary>
        public GoogleSafeBrowsingDatabase()
        {
            SuspiciousLists = new Dictionary<string, SafeBrowsingList> {
                [SOCIAL_ENGINEERING] = new SafeBrowsingList(),
                [UNWANTED_SOFTWARE] = new SafeBrowsingList(),
                [MALWARE] = new SafeBrowsingList(),
            };
        }

        /// <summary>
        /// Enter back-off mode after receive an unsuccessful HTTP response.
        /// </summary>
        public void EnterBackOffMode()
        {
            Random random = new Random();
            BackOffSeed = random.NextDouble();
            Updated = DateTimeOffset.Now;

            if (!BackOffMode)
            {
                BackOffMode = true;
                BackOffRetryNumber = 1;
            }
            else
            {
                BackOffRetryNumber++;
            }
        }

        /// <summary>
        /// Exit back-off mode after receive a successful HTTP response.
        /// </summary>
        public void ExitBackOffMode()
        {
            BackOffMode = false;
        }
    }

    /// <summary>
    /// Google Safe Browsing list
    /// </summary>
    public class SafeBrowsingList
    {
        /// <summary>
        /// Url hash prefix list. Hashes can be anywhere from 4 to 32 bytes in size.
        /// </summary>
        public List<byte[]> Hashes { get; set; } = new List<byte[]>();

        /// <summary>
        /// The current state of the client for the requested list 
        /// (the encrypted client state that was received from the last successful list update).
        /// </summary>
        public string State { get; set; } = string.Empty;
    }
}
