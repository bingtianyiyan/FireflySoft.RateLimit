using System;
using System.Collections.Generic;

namespace FireflySoft.RateLimit.Core.Rule
{
    /// <summary>
    /// The rule of sliding window algorithm
    /// </summary>
    public class SlidingWindowRule : RateLimitRule
    {
        private int _periodNumber;

        /// <summary>
        /// small period number
        /// </summary>
        /// <value></value>
        public int PeriodNumber
        {
            get
            {
                return _periodNumber;
            }
            set { this._periodNumber = value; }
        }

        /// <summary>
        /// Statistical time window, which counts the number of requests in this time.
        /// </summary>
        public TimeSpan StatWindow { get; set; }

        /// <summary>
        /// The threshold of triggering rate limit in the statistical time window。
        /// </summary>
        public int LimitNumber { get; set; }

        /// <summary>
        /// Small period length in statistical time window
        /// </summary>
        public TimeSpan StatPeriod { get; set; }

        /// <summary>
        /// true: throw exception, false:not throw exception
        /// Exception->RateLimitException
        /// </summary>
        public bool RateLimitExceptionThrow { get; set; }

        /// <summary>
        /// create a new instance
        /// </summary>
        /// <param name="statWindow"></param>
        /// <param name="statPeriod"></param>
        public SlidingWindowRule(TimeSpan statWindow, TimeSpan statPeriod)
        {
            if (statWindow.TotalMilliseconds < 1)
            {
                throw new ArgumentException("the stat window can not less than 1ms.");
            }

            if (statPeriod.TotalMilliseconds < 1)
            {
                throw new ArgumentException("the stat period can not less than 1ms.");
            }

            if (statWindow.TotalMilliseconds % statPeriod.TotalMilliseconds > 0)
            {
                throw new ArgumentException("The stat window must be an integral multiple of the stat period.");
            }

            StatWindow = statWindow;
            StatPeriod = statPeriod;

            _periodNumber = (int)(StatWindow.TotalMilliseconds / StatPeriod.TotalMilliseconds);
        }
    }
}
