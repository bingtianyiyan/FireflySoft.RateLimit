using FireflySoft.RateLimit.Core.Attribute;
using FireflySoft.RateLimit.Core.Rule;
using FireflySoft.RateLimit.Core.Time;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FireflySoft.RateLimit.Core.InProcessAlgorithm
{
    /// <summary>
    /// Define an in-process token bucket algorithm
    /// </summary>
    public class InProcessTokenBucketAlgorithm : BaseInProcessAlgorithm
    {
        /// <summary>
        /// store rateLimitRule
        /// </summary>
        private static readonly ConcurrentDictionary<string, TokenBucketRule> _requestRateLimitRule = new ConcurrentDictionary<string, TokenBucketRule>();

        /// <summary>
        /// create a new instance
        /// </summary>
        /// <param name="rules">The rate limit rules</param>
        /// <param name="timeProvider">The time provider</param>
        /// <param name="updatable">If rules can be updated</param>
        public InProcessTokenBucketAlgorithm(IEnumerable<TokenBucketRule> rules, ITimeProvider timeProvider = null, bool updatable = false)
        : base(rules, timeProvider, updatable)
        {
        }

        /// <summary>
        /// check single rule for target
        /// </summary>
        /// <param name="target"></param>
        /// <param name="rule"></param>
        /// <returns></returns>
        protected override RuleCheckResult CheckSingleRule(string target, RateLimitRule rule, HttpContext context = null)
        {
            var currentRule = rule as TokenBucketRule;
            var amount = 1;
            //check controller of method mark TokenbucketAttribute is priority compare with global
            if (context != null)
            {
                bool exists = _requestRateLimitRule.TryGetValue(target, out TokenBucketRule storeRule);
                if (exists)
                {
                    currentRule = storeRule;
                }
                else
                {
                    //check Attribute
                    var endpoint = GetEndpoint(context);
                    if (endpoint != null)
                    {
                        var actionAttribute = endpoint.Metadata.GetMetadata<TokenBucketLimitAttribute>();
                        if (actionAttribute != null)
                        {
                            currentRule.Capacity = actionAttribute.Capacity;
                            currentRule.InflowQuantityPerUnit = actionAttribute.InflowQuantityPerUnit;
                            currentRule.InflowUnit = CommonUtils.Parse(actionAttribute.Period);
                            currentRule.RateLimitExceptionThrow = actionAttribute.RateLimitExceptionThrow;
                            _requestRateLimitRule.TryAdd(target, currentRule);
                        }
                    }
                }
            }
            var result = InnerCheckSingleRule(target, amount, currentRule);
            if (result.Item1 && currentRule.RateLimitExceptionThrow)
            {
                throw new RateLimitException(context.Request.Path.Value);
            }
            return new RuleCheckResult()
            {
                IsLimit = result.Item1,
                Target = target,
                Count = result.Item2,
                Rule = rule
            };
        }

        /// <summary>
        /// get endpoint
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static Endpoint GetEndpoint(HttpContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return context.Features.Get<IEndpointFeature>()?.Endpoint;
        }

        /// <summary>
        /// check single rule for target
        /// </summary>
        /// <param name="target"></param>
        /// <param name="rule"></param>
        /// <returns></returns>
        protected override async Task<RuleCheckResult> CheckSingleRuleAsync(string target, RateLimitRule rule, HttpContext context = null)
        {
            return await Task.FromResult(CheckSingleRule(target, rule, context));
        }

        /// <summary>
        /// Decrease the count value of the rate limit target for token bucket algorithm.
        /// </summary>
        /// <param name="target">The target</param>
        /// <param name="amount">The amount of decrease</param>
        /// <param name="currentRule">The rate limit rule</param>
        /// <returns>Amount of token in the bucket</returns>
        public Tuple<bool, long> InnerCheckSingleRule(string target, long amount, TokenBucketRule currentRule)
        {
            bool locked = CheckLocked(target);
            if (locked)
            {
                return new Tuple<bool, long>(true, -1);
            }

            var inflowUnit = currentRule.InflowUnit.TotalMilliseconds;

            lock (target)
            {
                var currentTime = _timeProvider.GetCurrentLocalTime();
                long bucketAmount = 0;
                var result = _cache.GetCacheItem(target);
                if (result == null)
                {
                    var startTime = AlgorithmStartTime.ToSpecifiedTypeTime(currentTime, TimeSpan.FromMilliseconds(inflowUnit), currentRule.StartTimeType);
                    bucketAmount = currentRule.Capacity - amount;
                    _cache.Add(target, new CountValue(bucketAmount) { LastFlowTime = startTime }, DateTimeOffset.MaxValue);
                    return new Tuple<bool, long>(false, bucketAmount);
                }

                var countValue = (CountValue)result.Value;
                var lastTime = countValue.LastFlowTime;
                var lastTimeChanged = false;
                var pastTime = currentTime - lastTime;
                var pastTimeMilliseconds = pastTime.TotalMilliseconds;
                // Debug.WriteLine(currentTime.ToString("mm:ss.fff") + "," + lastTime.ToString("mm:ss.fff") + "," + pastTimeMilliseconds);
                if (pastTimeMilliseconds < inflowUnit)
                {
                    bucketAmount = countValue.Value - amount;
                }
                else
                {
                    var pastInflowUnitQuantity = (int)(pastTimeMilliseconds / inflowUnit);
                    lastTime = lastTime.AddMilliseconds(pastInflowUnitQuantity * inflowUnit);
                    lastTimeChanged = true;
                    var pastInflowQuantity = currentRule.InflowQuantityPerUnit * pastInflowUnitQuantity;
                    bucketAmount = (countValue.Value < 0 ? 0 : countValue.Value) + pastInflowQuantity - amount;
                }

                if (bucketAmount < 0)
                {
                    if (currentRule.LockSeconds > 0)
                    {
                        TryLock(target, currentTime, TimeSpan.FromSeconds(currentRule.LockSeconds));
                    }

                    return new Tuple<bool, long>(true, bucketAmount);
                }

                if (bucketAmount >= currentRule.Capacity)
                {
                    bucketAmount = currentRule.Capacity - amount;
                }

                countValue.Value = bucketAmount;
                if (lastTimeChanged)
                {
                    countValue.LastFlowTime = lastTime;
                }
                return new Tuple<bool, long>(false, countValue.Value);
            }
        }
    }
}