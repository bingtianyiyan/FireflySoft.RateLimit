/**
* 命名空间: FireflySoft.RateLimit.Core
*
* 功 能： N/A
* 类 名： RateLimitTypeAttributeJson
*
* Ver 变更日期 负责人 变更内容
* ───────────────────────────────────
* V0.01 2021/5/20 13:58:22ydy 初版
*
* Copyright (c) 2019 Lir Corporation. All rights reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：　　　　　　　　　　　　　　│
*└──────────────────────────────────┘
*/

using FireflySoft.RateLimit.Core.Attribute;

namespace FireflySoft.RateLimit.Core
{
    /// <summary>
    /// rateLimit Attribute data
    /// </summary>
    public class RateLimitTypeAttributeJson
    {
        /// <summary>
        ///
        /// </summary>
        public TokenBucketLimitAttribute TokenBucketLimitAttribute { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public LeakyBucketLimitAttribute LeakyBucketLimitAttribute { get; set; }

        /// <summary>
        ///
        /// </summary>
        public SlidingWindowLimitAttribute SlidingWindowLimitAttribute { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public FixedWindowLimitAttribute FixedWindowLimitAttribute { get; set; }
    }
}