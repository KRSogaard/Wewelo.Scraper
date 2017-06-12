using System;
using System.Threading.Tasks;

namespace Wewelo.Scraper.Tests.Extensions
{
    public class ScrapingTaskTestClass : IScrapingTask
    {
        private Action<IScrapingEngine, string> action;

        public ScrapingTaskTestClass(Action<IScrapingEngine, string> action)
        {
            this.action = action;
        }

        public Task Execute(IScrapingEngine scrapingEngine, string payload)
        {
            return Task.Run(() =>
            {
                action(scrapingEngine, payload);
            });
        }
    }
}