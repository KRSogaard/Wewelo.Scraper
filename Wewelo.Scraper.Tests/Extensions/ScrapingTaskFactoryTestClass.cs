using System;

namespace Wewelo.Scraper.Tests.Extensions
{
    public class ScrapingTaskFactoryTestClass : IScrapingTaskFactory
    {
        private string taskName;
        private Action<ScrapingEngine, string> action;

        public ScrapingTaskFactoryTestClass(string taskName, Action<ScrapingEngine, string> action)
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