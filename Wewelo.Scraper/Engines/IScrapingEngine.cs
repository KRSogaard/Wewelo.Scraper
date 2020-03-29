using System;
using System.Threading.Tasks;

namespace Wewelo.Scraper.Engines
{
    public interface IScrapingEngine
    {
        Task AddFailedTask(TaskPayload payload, Exception exp = null);
        Task AddTask(TaskPayload newTask);
        Task Start();
        Task Stop();
    }
}