/**
* 命名空间: FireflySoft.RateLimit.Core.Enum
*
* 功 能： N/A
* 类 名： RateLimitTypeEnum
*
* Ver 变更日期 负责人 变更内容
* ───────────────────────────────────
* V0.01 2021/5/20 13:51:47ydy 初版
*
* Copyright (c) 2019 Lir Corporation. All rights reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：　　　　　　　　　　　　　　│
*└──────────────────────────────────┘
*/

namespace FireflySoft.RateLimit.Core.Enum
{
    /// <summary>
    /// rateLimit
    /// </summary>
    public enum RateLimitTypeEnum
    {
        /// <summary>
        ///
        /// </summary>
        TokenBucket = 1,

        /// <summary>
        ///
        /// </summary>
        LeakyBucket = 2,

        /// <summary>
        ///
        /// </summary>
        FixedWindow = 3,

        /// <summary>
        ///
        /// </summary>
        SlidingWindow = 4
    }
}