using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Wewelo.Scraper.Web
{
    public interface IWebFetcher
    {
        CookieContainer CookieContainer { get; set; }

        void AddFormData(string key, string value);
        void Addheader(string key, string value);
        Task<WebFetcherResult> Download(string url);
        Task<WebFetcherResult> Download(string url, HttpMethod method);
        void SetForm(string value);
        void SetRequestTimeOut(TimeSpan span);
        void SetValidator(Func<string, bool> validator);
    }
}