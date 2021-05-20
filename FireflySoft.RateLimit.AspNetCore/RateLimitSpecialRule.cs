/**
* 命名空间: FireflySoft.RateLimit.Core.Middleware
*
* 功 能： N/A
* 类 名： RateLimitSpecialRule
*
* Ver 变更日期 负责人 变更内容
* ───────────────────────────────────
* V0.01 2021/5/12 16:42:20ydy 初版
*
* Copyright (c) 2019 Lir Corporation. All rights reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：　　　　　　　　　　　　　　│
*└──────────────────────────────────┘
*/

namespace FireflySoft.RateLimit.Core
{
    /// <summary>
    /// RateLimit other special condition rule 
    /// </summary>
    public class RateLimitSpecialRule
    {
        /// <summary>
        /// whether open polly
        /// </summary>
        public bool EnablePolly { get; set; }

        /// <summary>
        ///  global RateLimit method list  
        ///  sample:  routeName + "/" + methodName   if is empty then default is context.Request.Path.Value
        /// </summary>
        public string[] MethodList { get; set; }
    }
}