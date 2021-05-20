using FireflySoft.RateLimit.Core.Attribute;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;
using Polly;
using Polly.CircuitBreaker;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FireflySoft.RateLimit.Core
{
    /// <summary>
    /// Rate Limit Middleware
    /// </summary>
    public class RateLimitMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IAlgorithm _algorithm;
        private readonly HttpErrorResponse _error;
        private readonly HttpInvokeInterceptor _interceptor;
        private readonly RateLimitSpecialRule _specialRule;
        private static ConcurrentDictionary<string, string> _getMethodRoutePath = new ConcurrentDictionary<string, string>();
        private static ConcurrentDictionary<string, AsyncPolicy<AlgorithmCheckResult>> _getPollyObj = new ConcurrentDictionary<string, AsyncPolicy<AlgorithmCheckResult>>();

        /// <summary>
        /// store rateLimitRule
        /// </summary>
        private static readonly ConcurrentDictionary<string, RateLimitTypeAttributeJson> _requestRateLimitRule = new ConcurrentDictionary<string, RateLimitTypeAttributeJson>();

        /// <summary>
        /// Create a new instance
        /// </summary>
        /// <param name="next"></param>
        /// <param name="algorithm"></param>
        /// <param name="error"></param>
        /// <param name="interceptor"></param>
        /// <param name="specialRule"></param>
        public RateLimitMiddleware(RequestDelegate next, IAlgorithm algorithm, HttpErrorResponse error, HttpInvokeInterceptor interceptor, RateLimitSpecialRule specialRule)
        {
            _next = next;
            _algorithm = algorithm;
            _error = error;
            _interceptor = interceptor;
            _specialRule = specialRule;
        }

        /// <summary>
        /// Asynchronous processing of Middleware
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task Invoke(HttpContext context)
        {
            #region check whether need RateLimit  and local Attribute is prority then other rules

            string methodPath = context.Request.Path.Value;
            bool getMethodNameResult = _getMethodRoutePath.TryGetValue(methodPath, out string methodCacheName);
            if (!getMethodNameResult)
            {
                var routeData = context.GetRouteData();
                if (routeData != null
                    && routeData.Values.Any())
                {
                    var controllerName = routeData.Values["controller"];
                    var actionName = routeData.Values["action"];
                    methodCacheName = controllerName != null && actionName != null ? controllerName + "/" + actionName : methodPath;
                    _getMethodRoutePath.TryAdd(methodPath, methodCacheName);
                }
            }
            EnableRateLimitAttribute enableRateLimitAttribute = null;
            bool specialRuleResult = _specialRule != null
                                    && _specialRule.MethodList != null
                                    && _specialRule.MethodList.Any()
                                    && _specialRule.MethodList.Contains(methodCacheName);
            if (!specialRuleResult)
            {
                //wheather need RateLimit
                var endpoint = GetEndpoint(context);
                if (endpoint != null)
                {
                    enableRateLimitAttribute = endpoint.Metadata.GetMetadata<EnableRateLimitAttribute>();
                    if (enableRateLimitAttribute == null)
                    {
                        await _next(context);
                        return;
                    }
                    var disableRateLimitAttribute = endpoint.Metadata.GetMetadata<DisableRateLimitAttribute>();
                    if (disableRateLimitAttribute != null)
                    {
                        await _next(context);
                        return;
                    }
                }
                else
                {
                    await _next(context);
                    return;
                }
            }

            #endregion check whether need RateLimit  and local Attribute is prority then other rules

            //local method attribute
            var getMethodAttrRateLimitData = GetRateLimitTypeAttributeData(context);

            // context.Items.Add("PollyRequire", _specialRule != null ? _specialRule.EnablePolly : false);
            await DoOnBeforeCheck(context, _algorithm).ConfigureAwait(false);
            AlgorithmCheckResult checkResult;//= await _algorithm.CheckAsync(context);

            #region Polly

            bool enablePolly = enableRateLimitAttribute == null ? (_specialRule != null ? _specialRule.EnablePolly : false) : enableRateLimitAttribute.EnablePolly;
            if (enablePolly)
            {
                checkResult = await PollyRateLimitAdvancedCircuitBreakerAsync(context.Request.Path.Value, enableRateLimitAttribute)
                    .ExecuteAsync(async () =>
                {
                    return await _algorithm.CheckAsync(context, getMethodAttrRateLimitData);
                });
            }
            else
            {
                checkResult = await _algorithm.CheckAsync(context, getMethodAttrRateLimitData);
            }

            #endregion Polly

            await DoOnAfterCheck(context, checkResult).ConfigureAwait(false);

            if (checkResult.IsLimit)
            {
                await DoOnTriggered(context, checkResult).ConfigureAwait(false);

                context.Response.StatusCode = _error.HttpStatusCode;
                await SetHeaders(context, checkResult).ConfigureAwait(false);
                await ReponseWithTooManyRequests(context, checkResult).ConfigureAwait(false);
            }
            else
            {
                await DoOnBreforUntriggeredDoNext(context, checkResult).ConfigureAwait(false);

                await DoLeakyBucketWait(checkResult).ConfigureAwait(false);

                //Debug.WriteLine("R-Count" + checkResult.RuleCheckResults.First().Count + " " + DateTimeOffset.Now.ToString("mm:ss.fff"));
                await _next(context);

                await DoOnAfterUntriggeredDoNext(context, checkResult).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// setting response header
        /// </summary>
        /// <param name="context"></param>
        /// <param name="checkResult"></param>
        /// <returns></returns>
        private async Task SetHeaders(HttpContext context, AlgorithmCheckResult checkResult)
        {
            var headers = await BuildHttpHeaders(context, checkResult).ConfigureAwait(false);
            if (headers != null && headers.Count > 0)
            {
                foreach (var h in headers)
                {
                    context.Response.Headers.Append(h.Key, h.Value);
                }
            }
        }

        /// <summary>
        /// setting rateLimit response content
        /// </summary>
        /// <param name="context"></param>
        /// <param name="checkResult"></param>
        /// <returns></returns>
        private async Task ReponseWithTooManyRequests(HttpContext context, AlgorithmCheckResult checkResult)
        {
            string content = await BuildHttpContent(context, checkResult).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(content))
            {
                var bodyContent = Encoding.UTF8.GetBytes(content);
                context.Response.ContentType = "application/json;charset=utf-8";
                await context.Response.Body.WriteAsync(bodyContent, 0, bodyContent.Length).ConfigureAwait(false);
            }
            else
            {
                await context.Response.WriteAsync(string.Empty).ConfigureAwait(false);
            }
        }

        private static async Task DoLeakyBucketWait(AlgorithmCheckResult checkResult)
        {
            // Simulation leaky bucket algorithm queuing mechanism
            var wait = checkResult.RuleCheckResults.Max(d => d.Wait);
            if (wait > 0)
            {
                await Task.Delay((int)wait).ConfigureAwait(false);
            }
        }

        private async Task<string> BuildHttpContent(HttpContext context, AlgorithmCheckResult checkResult)
        {
            string content = null;
            if (_error.BuildHttpContentAsync != null)
            {
                content = await _error.BuildHttpContentAsync(context, checkResult).ConfigureAwait(false);
            }
            else if (_error.BuildHttpContent != null)
            {
                content = _error.BuildHttpContent(context, checkResult);
            }

            return content;
        }

        private async Task<Dictionary<string, StringValues>> BuildHttpHeaders(HttpContext context, AlgorithmCheckResult checkResult)
        {
            Dictionary<string, StringValues> headers = null;
            if (_error.BuildHttpHeadersAsync != null)
            {
                headers = await _error.BuildHttpHeadersAsync(context, checkResult).ConfigureAwait(false);
            }
            else if (_error.BuildHttpHeaders != null)
            {
                headers = _error.BuildHttpHeaders(context, checkResult);
            }

            return headers;
        }

        private async Task DoOnTriggered(HttpContext context, AlgorithmCheckResult checkResult)
        {
            if (_interceptor != null)
            {
                if (_interceptor.OnTriggeredAsync != null)
                {
                    await _interceptor.OnTriggeredAsync(context, checkResult).ConfigureAwait(false);
                }
                else if (_interceptor.OnTriggered != null)
                {
                    _interceptor.OnTriggered(context, checkResult);
                }
            }
        }

        private async Task DoOnBeforeCheck(HttpContext context, IAlgorithm algorithm)
        {
            if (_interceptor != null)
            {
                if (_interceptor.OnBeforeCheckAsync != null)
                {
                    await _interceptor.OnBeforeCheckAsync(context, algorithm).ConfigureAwait(false);
                }
                else if (_interceptor.OnBeforeCheck != null)
                {
                    _interceptor.OnBeforeCheck(context, algorithm);
                }
            }
        }

        private async Task DoOnAfterCheck(HttpContext context, AlgorithmCheckResult checkResult)
        {
            if (_interceptor != null)
            {
                if (_interceptor.OnAfterCheckAsync != null)
                {
                    await _interceptor.OnAfterCheckAsync(context, checkResult).ConfigureAwait(false);
                }
                else if (_interceptor.OnAfterCheck != null)
                {
                    _interceptor.OnAfterCheck(context, checkResult);
                }
            }
        }

        private async Task DoOnBreforUntriggeredDoNext(HttpContext context, AlgorithmCheckResult checkResult)
        {
            if (_interceptor != null)
            {
                if (_interceptor.OnBreforUntriggeredDoNextAsync != null)
                {
                    await _interceptor.OnBreforUntriggeredDoNextAsync(context, checkResult).ConfigureAwait(false);
                }
                else if (_interceptor.OnBreforUntriggeredDoNext != null)
                {
                    _interceptor.OnBreforUntriggeredDoNext(context, checkResult);
                }
            }
        }

        private async Task DoOnAfterUntriggeredDoNext(HttpContext context, AlgorithmCheckResult checkResult)
        {
            if (_interceptor != null)
            {
                if (_interceptor.OnAfterUntriggeredDoNextAsync != null)
                {
                    await _interceptor.OnAfterUntriggeredDoNextAsync(context, checkResult).ConfigureAwait(false);
                }
                else if (_interceptor.OnAfterUntriggeredDoNext != null)
                {
                    _interceptor.OnAfterUntriggeredDoNext(context, checkResult);
                }
            }
        }

        /// <summary>
        /// rateLimit exception
        /// </summary>
        /// <returns></returns>
        private static AsyncPolicy<AlgorithmCheckResult> PollyRateLimitAdvancedCircuitBreakerAsync(string policyKey, EnableRateLimitAttribute enableRateLimitAttribute)
        {
            var getResult = _getPollyObj.TryGetValue(policyKey, out AsyncPolicy<AlgorithmCheckResult> pollyObj);
            if (getResult)
            {
                return pollyObj;
            }
            int retryCount = enableRateLimitAttribute == null ? 1 : enableRateLimitAttribute.RetryCount;
            double failureThreshold = enableRateLimitAttribute == null ? 0.75 : enableRateLimitAttribute.FailureThreshold;
            TimeSpan samplingDuration = enableRateLimitAttribute == null ? TimeSpan.FromSeconds(10) : enableRateLimitAttribute.SamplingDuration;
            int minimumThroughput = enableRateLimitAttribute == null ? 100 : enableRateLimitAttribute.MinimumThroughput;
            TimeSpan durationOfBreak = enableRateLimitAttribute == null ? TimeSpan.FromSeconds(10) : enableRateLimitAttribute.DurationOfBreak;
            var breakPolicy = Policy<AlgorithmCheckResult>.Handle<RateLimitException>().AdvancedCircuitBreakerAsync(
                failureThreshold: failureThreshold,
                samplingDuration: samplingDuration,
                minimumThroughput: minimumThroughput,
                durationOfBreak: durationOfBreak,
                onBreak: (r, t) =>
                {
                    Console.WriteLine("onbreak");
                },
                onReset: () =>
                {
                    Console.WriteLine("onReset");
                },
                onHalfOpen: () =>
                {
                    Console.WriteLine("onHalfOpen");
                }
                );
            var retry = Policy<AlgorithmCheckResult>.Handle<RateLimitException>().WaitAndRetryAsync(retryCount, i => TimeSpan.FromMilliseconds(100 * i));
            var message = new AlgorithmCheckResult(new List<RuleCheckResult>() { new RuleCheckResult() { IsLimit = true } })
            {
            };
            var fallback = Policy<AlgorithmCheckResult>.Handle<BrokenCircuitException>().FallbackAsync(message);
            var fallbackBreak = Policy.WrapAsync(fallback, retry, breakPolicy).WithPolicyKey(policyKey);
            _getPollyObj.TryAdd(policyKey, fallbackBreak);
            return fallbackBreak;
        }

        /// <summary>
        /// get method rateLimit attribute
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private static RateLimitTypeAttributeJson GetRateLimitTypeAttributeData(HttpContext context)
        {
            #region check controller of method mark TokenbucketAttribute is priority compare with global service.AddRamitLimit

            RateLimitTypeAttributeJson storeRule = null;
            if (context != null)
            {
                string target = context.Request.Path.Value;
                bool exists = _requestRateLimitRule.TryGetValue(target, out storeRule);
                if (!exists)
                {
                    //check Attribute
                    var endpoint = GetEndpoint(context);
                    if (endpoint != null)
                    {
                        var actionTokenBucketAttribute = endpoint.Metadata.GetMetadata<TokenBucketLimitAttribute>();
                        var actionLeakyBucketAttribute = endpoint.Metadata.GetMetadata<LeakyBucketLimitAttribute>();
                        var actionFixedWindowAttribute = endpoint.Metadata.GetMetadata<FixedWindowLimitAttribute>();
                        var actionSlidingWindowAttribute = endpoint.Metadata.GetMetadata<SlidingWindowLimitAttribute>();
                        storeRule = actionTokenBucketAttribute != null || actionLeakyBucketAttribute != null
                            || actionFixedWindowAttribute != null || actionSlidingWindowAttribute != null ?
                            new RateLimitTypeAttributeJson() : null;
                        if (actionTokenBucketAttribute != null)
                        {
                            storeRule.TokenBucketLimitAttribute = new TokenBucketLimitAttribute();
                            storeRule.TokenBucketLimitAttribute.Capacity = actionTokenBucketAttribute.Capacity;
                            storeRule.TokenBucketLimitAttribute.InflowQuantityPerUnit = actionTokenBucketAttribute.InflowQuantityPerUnit;
                            storeRule.TokenBucketLimitAttribute.Period = actionTokenBucketAttribute.Period;
                            storeRule.TokenBucketLimitAttribute.RateLimitExceptionThrow = actionTokenBucketAttribute.RateLimitExceptionThrow;
                        }
                        if (actionLeakyBucketAttribute != null)
                        {
                            storeRule.LeakyBucketLimitAttribute = new LeakyBucketLimitAttribute();
                            storeRule.LeakyBucketLimitAttribute.Capacity = actionLeakyBucketAttribute.Capacity;
                            storeRule.LeakyBucketLimitAttribute.OutflowQuantityPerUnit = actionLeakyBucketAttribute.OutflowQuantityPerUnit;
                            storeRule.LeakyBucketLimitAttribute.Period = actionLeakyBucketAttribute.Period;
                            storeRule.LeakyBucketLimitAttribute.RateLimitExceptionThrow = actionLeakyBucketAttribute.RateLimitExceptionThrow;
                        }
                        if (actionFixedWindowAttribute != null)
                        {
                            storeRule.FixedWindowLimitAttribute = new FixedWindowLimitAttribute();
                            storeRule.FixedWindowLimitAttribute.LimitNumber = actionFixedWindowAttribute.LimitNumber;
                            storeRule.FixedWindowLimitAttribute.Period = actionFixedWindowAttribute.Period;
                            storeRule.FixedWindowLimitAttribute.RateLimitExceptionThrow = actionFixedWindowAttribute.RateLimitExceptionThrow;
                        }
                        if (actionSlidingWindowAttribute != null)
                        {
                            storeRule.SlidingWindowLimitAttribute = new SlidingWindowLimitAttribute();
                            storeRule.SlidingWindowLimitAttribute.LimitNumber = actionSlidingWindowAttribute.LimitNumber;
                            storeRule.SlidingWindowLimitAttribute.StatWindowPeriod = actionSlidingWindowAttribute.StatWindowPeriod;
                            storeRule.SlidingWindowLimitAttribute.StatSmallPeriod = actionSlidingWindowAttribute.StatSmallPeriod;
                            storeRule.SlidingWindowLimitAttribute.RateLimitExceptionThrow = actionSlidingWindowAttribute.RateLimitExceptionThrow;
                        }
                        if (storeRule != null)
                        {
                            _requestRateLimitRule.TryAdd(target, storeRule);
                        }
                    }
                }
            }
            return storeRule;

            #endregion check controller of method mark TokenbucketAttribute is priority compare with global service.AddRamitLimit
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
    }

    /// <summary>
    /// Rate Limit Middleware Extensions
    /// </summary>
    public static class RateLimitMiddlewareExtensions
    {
        /// <summary>
        /// Using rate limit processor
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseRateLimit(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RateLimitMiddleware>();
        }
    }
}