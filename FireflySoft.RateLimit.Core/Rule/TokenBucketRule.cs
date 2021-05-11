using System;

namespace FireflySoft.RateLimit.Core.Rule
{
    /// <summary>
    /// The rule of token bucket algorithm
    /// </summary>
    public class TokenBucketRule : RateLimitRule
    {
        /// <summary>
        /// Rhe capacity of token bucket
        /// </summary>
        public int Capacity { get; set; }

        /// <summary>
        /// The inflow quantity per unit time
        /// </summary>
        public int InflowQuantityPerUnit { get; set; }

        /// <summary>
        /// The time unit of inflow to the bucket bucket
        /// </summary>
        public TimeSpan InflowUnit { get; set; }

        /// <summary>
        /// true: throw exception, false:not throw exception
        /// Exception->RateLimitException
        /// </summary>
        public bool RateLimitExceptionThrow { get; set; }

        /// <summary>
        /// create a new instance
        /// </summary>
        /// <param name="capacity"></param>
        /// <param name="inflowQuantityPerUnit"></param>
        /// <param name="inflowUnit"></param>
        public TokenBucketRule(int capacity, int inflowQuantityPerUnit, TimeSpan inflowUnit)
        {
            if (capacity < 1)
            {
                throw new ArgumentException("the capacity can not less than 1.");
            }

            if (inflowQuantityPerUnit < 1)
            {
                throw new ArgumentException("the inflow quantity per unit can not less than 1.");
            }

            if (inflowUnit.TotalMilliseconds < 1)
            {
                throw new ArgumentException("the inflow unit can not less than 1ms.");
            }

            Capacity = capacity;
            InflowQuantityPerUnit = inflowQuantityPerUnit;
            InflowUnit = inflowUnit;
        }
    }
}