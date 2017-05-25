using System;
using System.Collections.Generic;
using System.Text;

namespace Wewelo.Scraper
{
    public interface IScrapingTask
    {
        void Execute(ScrapingEngine scrapingEngine, string payload);
    }
}
