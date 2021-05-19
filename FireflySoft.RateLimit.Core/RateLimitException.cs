/**
* 命名空间: FireflySoft.RateLimit.Core
*
* 功 能： N/A
* 类 名： RateLimitException
*
* Ver 变更日期 负责人 变更内容
* ───────────────────────────────────
* V0.01 2021/5/11 17:46:17 ydy 初版
*
* Copyright (c) 2019 Lir Corporation. All rights reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：　　　　　　　　　　│
*└──────────────────────────────────┘
*/

using System;
using System.Net;
using System.Net.Http;

namespace FireflySoft.RateLimit.Core
{
    /// <summary>
    /// throw RateLimit exception
    /// </summary>
    public class RateLimitException : Exception
    {
        public string Name { get; set; }

        public HttpStatusCode HttpStatusCode { get; set; }

        public RateLimitException(string name, HttpStatusCode httpStatusCode = HttpStatusCode.Forbidden)
        {
            this.Name = name;
            this.HttpStatusCode = httpStatusCode;
        }
    }
}