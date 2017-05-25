using System;
using System.Net;

namespace Wewelo.Scraper.Web.Proxy
{
    public class ProxyBonanzaProxy : IWebProxy
    {
        private string proxyIp;
        private string proxyPort;
        private string proxylogin;
        private string proxypassword;

        public ProxyBonanzaProxy(string ip, string port, string login, string password)
        {
            proxyIp = ip;
            proxyPort = port;
            proxylogin = login;
            proxypassword = password;
        }

        public Uri GetProxy(Uri destination)
        {
            return new Uri($"http://{proxyIp}:{proxyPort}");
        }

        public bool IsBypassed(Uri host)
        {
            return false;
        }

        public ICredentials Credentials
        {
            get => new NetworkCredential(proxylogin, proxypassword);
            set => throw new NotImplementedException();
        }
    }
}