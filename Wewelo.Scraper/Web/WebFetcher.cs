using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using Wewelo.Common;
using Wewelo.Common.Tasks;
using Wewelo.Scraper.Web.Proxy;

namespace Wewelo.Scraper.Web
{
    public class WebFetcherResult
    {
        public HttpResponseMessage ResponseMessage { get; set; }
        public string HTML { get; set; }
        public WebFetcherResultStatus Status { get; set; }
        public Exception Exception { get; set; }

        public override string ToString()
        {
            return "{" + $"\"status-code\": \"{ResponseMessage.StatusCode}\", \"status\": \"{Status}\", \"html\": \"{HTML.Length}\"" + "}";
        }
    }

    public enum WebFetcherResultStatus
    {
        Ok,
        VerificationFailed,
        Exception
    }

    public class WebFetcher
    {
        private static Logger log = LogManager.GetCurrentClassLogger();

        private Uri url;
        private HttpMethod method;
        private ProxyManager proxyManager;
        private TimeSpan timeOut;
        private List<KeyValuePair<string, string>> formData;
        private List<KeyValuePair<string, string>> headers;
        private string formString;
        private Func<string, bool> validateResult;

        private CookieContainer _cookieContainer;
        public CookieContainer cookieContainer
        {
            get
            {
                if (_cookieContainer == null)
                {
                    _cookieContainer = new CookieContainer();
                }
                return _cookieContainer;
            }
            set { _cookieContainer = value; }
        }
        public void setRequestTimeOut(TimeSpan span)
        {
            timeOut = span;
        }
        public void setValidator(Func<string, bool> validator)
        {
            validateResult = validator;
        }
        public void AddFormData(string key, string value)
        {
            formData.Add(new KeyValuePair<string, string>(key, value));
        }
        public void Addheader(string key, string value)
        {
            headers.Add(new KeyValuePair<string, string>(key, value));
        }
        public void SetForm(string value)
        {
            formString = value;
        }

        public WebFetcher(ProxyManager proxyManager, String url, HttpMethod method = null)
        {
            Validator.NonNull("proxyManager", proxyManager);
            Validator.NonEmpty("url", url);

            this.proxyManager = proxyManager;
            this.url = new Uri(url);
            this.method = method ?? HttpMethod.Get;
            headers = new List<KeyValuePair<string, string>>();
            formData = new List<KeyValuePair<string, string>>();
            timeOut = TimeSpan.FromSeconds(30);

            // Default no validation
            validateResult = (html) => true;
        }

        public async Task<WebFetcherResult> Download()
        {
            var proxy = getProxy();
            try
            {
                log.Debug($"Fetching URL: {url}");
                using (var client = new HttpClient(GetHttpClientHandler(proxy)))
                {
                    AddHeadersToClient(client);

                    client.BaseAddress = url.PathAndQuery.Contains("https:")
                        ? new Uri("https://" + url.Host)
                        : new Uri("http://" + url.Host);

                    CancellationTokenSource cancelToken = new CancellationTokenSource();
                    log.Trace("Got web requset timeout of {0}", timeOut);
                    cancelToken.CancelAfter(timeOut);
                    log.Trace("Download started.");
                    DateTime start = DateTime.Now;


                    var message = new HttpRequestMessage(method, url.PathAndQuery);
                    AttachHeaders(message);
                    AttachContent(message);

                    HttpResponseMessage result = null;
                    result = await client.SendAsync(message, cancelToken.Token).ConfigureAwait(false);

                    var returnResult = new WebFetcherResult()
                    {
                        ResponseMessage = result,
                        Status = WebFetcherResultStatus.Ok
                    };

                    if (result != null && result.IsSuccessStatusCode)
                    {
                        returnResult.HTML = await result.Content.ReadAsStringAsync()
                            .WithCancellation(cancelToken.Token).ConfigureAwait(false);

                        if (!validateResult(returnResult.HTML))
                        {
                            returnResult.Status = WebFetcherResultStatus.VerificationFailed;
                            proxyManager.AddProxy(proxy, true);
                        }
                        else
                        {
                            proxyManager.AddProxy(proxy, false);
                        }
                        return returnResult;
                    }

                    // Got bad response code
                    proxyManager.AddProxy(proxy, true);
                    returnResult.Status = WebFetcherResultStatus.VerificationFailed;
                    return returnResult;
                }
            }
            catch (Exception exp)
            {
                log.Warn(exp, "Exception while downloading.");
                proxyManager.AddProxy(proxy, false);
                return new WebFetcherResult()
                {
                    Status = WebFetcherResultStatus.Exception,
                    Exception = exp
                };
            }
        }

        private void AttachContent(HttpRequestMessage message)
        {
            if (this.formString != null)
            {
                message.Content = new StringContent(this.formString, Encoding.UTF8, "application/json");
            }
            else if (this.formData.Any())
            {
                StringBuilder stringBuilder = new StringBuilder();
                foreach (KeyValuePair<string, string> current in formData)
                {
                    if (stringBuilder.Length > 0)
                    {
                        stringBuilder.Append('&');
                    }

                    stringBuilder.Append(Encode(current.Key));
                    stringBuilder.Append('=');
                    stringBuilder.Append(Encode(current.Value));
                }

                message.Content = new StringContent(stringBuilder.ToString(), Encoding.UTF8, "application/x-www-form-urlencoded");
            }
        }

        private void AttachHeaders(HttpRequestMessage message)
        {
            foreach (var header in this.headers)
            {
                message.Headers.Add(header.Key, header.Value);
            }
        }

        private HttpClientHandler GetHttpClientHandler(IWebProxy proxy)
        {
            return new HttpClientHandler()
            {
                CookieContainer = cookieContainer,
                UseDefaultCredentials = false,
                UseProxy = true,
                Proxy = proxy,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
        }

        private IWebProxy getProxy()
        {
            var proxy = proxyManager.GetProxy();
            if (proxy == null)
            {
                throw new ProxyException("No proxy is avalible.");
            }
            log.Debug($"Got proxy \"{proxy.GetProxy(url)}\"");
            return proxy;
        }

        protected virtual void AddHeadersToClient(HttpClient client)
        {
            try
            {
                client.DefaultRequestHeaders.Add("Accept",
                    "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
                client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
                client.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.5");
                client.DefaultRequestHeaders.Add("User-Agent",
                    "Mozilla/5.0 (Windows NT 6.1; rv:38.0) Gecko/20100101 Firefox/38.0");
                client.DefaultRequestHeaders.Add("AcceptCharset", "utf-8");

                if (client.DefaultRequestHeaders.Contains("Host"))
                {
                    client.DefaultRequestHeaders.Remove("Host");
                }
                client.DefaultRequestHeaders.Add("Host", url.Host);
            }
            catch (Exception e)
            {
                log.Error(e, "Failed to add default headders to the request.");
                throw e;
            }
        }

        private static string Encode(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                return string.Empty;
            }
            return System.Net.WebUtility.UrlEncode(data).Replace("%20", "+");
        }
    }
}
