using System;

namespace Wewelo.Scraper.Web.Proxy
{
    public class ProxyException : Exception
    {
        public ProxyException()
        { }

        public ProxyException(String exp) : base(exp)
        { }
    }
}