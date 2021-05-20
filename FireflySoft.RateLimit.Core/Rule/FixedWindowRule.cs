using System;

namespace FireflySoft.RateLimit.Core.Rule
{
    /// <summary>
    /// Fixed Window Algorithm
    /// </summary>
    public class FixedWindowRule : RateLimitRule
    {
        /// <summary>
        /// Statistical time window, which counts the number of requests in this time.
        /// When using redis storage, it needs to be an integral multiple of one second.
        /// </summary>
        public TimeSpan StatWindow { get; set; }

        /// <summary>
        /// The threshold of triggering rate limit in the statistical time window。
        /// </summary>
        public int LimitNumber { get; set; }

        /// <summary>
        /// true: throw exception, false:not throw exception
        /// Exception->RateLimitException
        /// </summary>
        public bool RateLimitExceptionThrow { get; set; }
    }
}