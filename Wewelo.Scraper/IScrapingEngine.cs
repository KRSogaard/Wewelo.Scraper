using System;
using System.Threading.Tasks;

namespace Wewelo.Scraper
{
    public interface IScrapingEngine
    {
        Task AddFailedTask(string taskName, string payload, Exception exp = null);
        Task AddTask(TaskPayload newTask);
        Task Start();
    }
}