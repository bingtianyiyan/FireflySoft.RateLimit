/**
* 命名空间: FireflySoft.RateLimit.Core.Attribute
*
* 功 能： N/A
* 类 名： LeakyBucketLimitAttribute
*
* Ver 变更日期 负责人 变更内容
* ───────────────────────────────────
* V0.01 2021/5/20 11:05:03ydy 初版
*
* Copyright (c) 2019 Lir Corporation. All rights reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：　　　　　　　　　　　　　　│
*└──────────────────────────────────┘
*/

using System;

namespace FireflySoft.RateLimit.Core.Attribute
{
    /// <summary>
    /// LeakyBucketLimitAttribute is priority global LeakyBucketRule with below params
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class LeakyBucketLimitAttribute : System.Attribute
    {
        /// <summary>
        /// The capacity of current leaky bucket
        /// </summary>
        public long Capacity { get; private set; }

        /// <summary>
        /// The outflow quantity per unit time
        /// </summary>
        public long OutflowQuantityPerUnit { get; private set; }

        /// <summary>
        /// The time unit of outflow from the leaky bucket
        /// </summary>
        // public TimeSpan OutflowUnit { get; private set; }

        /// <summary>
        ///  The time unit of inflow to the bucket bucket
        ///  like: 1m->TimeSpan.FromMilliseconds   1s->TimeSpan.FromSeconds  1m->TimeSpan.FromMinute 1h 1d
        /// </summary>
        public string Period { get; set; }

        /// <summary>
        /// true: throw exception, false:not throw exception
        /// Exception->RateLimitException
        /// </summary>
        public bool RateLimitExceptionThrow { get; set; }
    }
}