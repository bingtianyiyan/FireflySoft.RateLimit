/**
* 命名空间: FireflySoft.RateLimit.Core.Attribute
*
* 功 能： N/A
* 类 名： EnableRateLimitAttribute
*
* Ver 变更日期 负责人 变更内容
* ───────────────────────────────────
* V0.01 2021/5/12 13:38:59 ydy 初版
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
    /// should throttle RateLimit
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class EnableRateLimitAttribute : System.Attribute
    {
    }
}