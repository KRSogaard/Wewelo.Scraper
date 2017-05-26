using System;
using System.Threading.Tasks;

namespace Wewelo.Scraper.Tests.Extensions
{
    public class ScrapingTaskTestClass : IScrapingTask
    {
        private Action<ScrapingEngine, string> action;

        public ScrapingTaskTestClass(Action<ScrapingEngine, string> action)
        {
            this.action = action;
        }

        public Task Execute(ScrapingEngine scrapingEngine, string payload)
        {
            return Task.Run(() =>
            {
                action(scrapingEngine, payload);
            });
        }
    }
}