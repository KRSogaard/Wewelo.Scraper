using System;

namespace Wewelo.Scraper.Tests.Extensions
{
    public class ScrapingTaskTestClass : IScrapingTask
    {
        private Action<ScrapingEngine, string> action;

        public ScrapingTaskTestClass(Action<ScrapingEngine, string> action)
        {
            this.action = action;
        }

        public void Execute(ScrapingEngine scrapingEngine, string payload)
        {
            action(scrapingEngine, payload);
        }
    }
}