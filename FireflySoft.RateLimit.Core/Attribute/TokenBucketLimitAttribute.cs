/**
* 命名空间: FireflySoft.RateLimit.Core.Attribute
*
* 功 能： N/A
* 类 名： TokenBucketLimitAttribute
*
* Ver 变更日期 负责人 变更内容
* ───────────────────────────────────
* V0.01 2021/5/11 14:02:29 ydy 初版
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
    /// TokenBucketAttribute is priority global TokenBucketRule with below params
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class TokenBucketLimitAttribute : System.Attribute
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
        //public TimeSpan InflowUnit { get;set; }

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