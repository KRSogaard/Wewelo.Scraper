using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using NLog;

namespace Wewelo.Scraper.Web.Proxy
{
    public class ProxyManager
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        private ConcurrentQueue<IWebProxy> proxies;
        private ConcurrentDictionary<IWebProxy, int> badProxyCounter;

        public ProxyManager()
        {
            ForceProxies = false;
            ProxyRetries = 3;
            proxies = new ConcurrentQueue<IWebProxy>();
            badProxyCounter = new ConcurrentDictionary<IWebProxy, int>();
        }

        public int ProxyRetries { get; set; }
        public bool ForceProxies { get; set; }

        public IWebProxy GetProxy()
        {
            IWebProxy proxy;
            if (proxies == null ||
                !proxies.TryDequeue(out proxy))
            {
                return null;
            }
            return proxy;
        }

        public void AddProxy(IWebProxy proxy, bool bad = false)
        {
            ForceProxies = true;
            if (bad)
            {
                if (!badProxyCounter.ContainsKey(proxy))
                {
                    badProxyCounter.TryAdd(proxy, 0);
                }

                badProxyCounter[proxy]++;
                if (badProxyCounter[proxy] >= ProxyRetries)
                {
                    logger.Warn("Proxy {0} have failed {1} times, removing. {2} proxies remaining.", proxy, badProxyCounter[proxy]);
                    return;
                }
                logger.Warn("Proxy {0} have failed {1} of {2} times, requeueing.", proxy, badProxyCounter[proxy], ProxyRetries);
            }

            proxies.Enqueue(proxy);
        }

        public void Load(String path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException(path);
            }

            List<IWebProxy> proxyList = new List<IWebProxy>();
            foreach (var line in File.ReadAllLines(path, Encoding.UTF8))
            {
                string[] split = line.Split(',');
                proxyList.Add(new ProxyBonanzaProxy(split[0], split[1], split[2], split[3]));
            }

            // we need to random the insert order so all instante will not use the same proxy at the same time.
            Random rnd = new Random();
            foreach (var proxy in proxyList.OrderBy(x => rnd.Next()))
            {
                AddProxy(proxy);
            }
        }
    }
}