using System;
using System.Collections.Generic;
using System.Text;

namespace Wewelo.Scraper
{
    public interface IScrapingTaskFactory
    {
        string GetTaskName();
        IScrapingTask GetTaskInstance();
    }
}
