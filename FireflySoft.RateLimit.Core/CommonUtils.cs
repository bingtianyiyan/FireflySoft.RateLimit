/**
* 命名空间: FireflySoft.RateLimit.Core
*
* 功 能： N/A
* 类 名： CommonUtils
*
* Ver 变更日期 负责人 变更内容
* ───────────────────────────────────
* V0.01 2021/5/11 17:21:18 ydy 初版
*
* Copyright (c) 2019 Lir Corporation. All rights reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有　　　　　　　　　　　　　│
*└──────────────────────────────────┘
*/

using System;
using System.Linq;

namespace FireflySoft.RateLimit.Core
{
    /// <summary>
    /// common tools
    /// </summary>
    public class CommonUtils
    {
        /// <summary>
        /// Convert data
        /// </summary>
        /// <param name="notation"></param>
        /// <returns></returns>
        public static TimeSpan Parse(string notation)
        {
            if (String.IsNullOrWhiteSpace(notation))
            {
                throw GetException(notation);
            }
            var type = notation.Last();
            if (int.TryParse(notation.Substring(0, notation.Length - 1), out int number))
            {
                switch (type)
                {
                    case 's': return TimeSpan.FromSeconds(number);
                    case 'm': return TimeSpan.FromMinutes(number);
                    case 'h': return TimeSpan.FromHours(number);
                    case 'd': return TimeSpan.FromDays(number);
                }
            }
            throw GetException(notation);
        }

        private static FormatException GetException(string notation)
        {
            return new FormatException("Could not parse notation " + notation);
        }
    }
}