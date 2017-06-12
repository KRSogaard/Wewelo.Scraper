using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Wewelo.Scraper
{
    public interface IScrapingTask
    {
        Task Execute(IScrapingEngine scrapingEngine, string payload);
    }
}
