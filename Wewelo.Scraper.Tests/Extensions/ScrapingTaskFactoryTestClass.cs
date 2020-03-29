using System;
using Wewelo.Scraper.Engines;

namespace Wewelo.Scraper.Tests.Extensions
{
    public class ScrapingTaskFactoryTestClass : IScrapingTaskFactory
    {
        private string taskName;
        private Action<IScrapingEngine, string> action;

        public ScrapingTaskFactoryTestClass(string taskName, Action<IScrapingEngine, string> action)
        {
            this.taskName = taskName;
            this.action = action;
        }

        public string GetTaskName()
        {
            return taskName;
        }

        public IScrapingTask GetTaskInstance()
        {
            return new ScrapingTaskTestClass(action);
        }
    }
}