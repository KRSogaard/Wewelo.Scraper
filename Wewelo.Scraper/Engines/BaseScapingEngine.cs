using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NLog;
using System.Diagnostics;

namespace Wewelo.Scraper.Engines
{
    public abstract class BaseScapingEngine : IScrapingEngine
    {
        private static Logger log = LogManager.GetCurrentClassLogger();
        private Dictionary<string, List<IScrapingTaskFactory>> taskFactories;

        protected void PopulateFactories(List<IScrapingTaskFactory> scrapingTaskFactories)
        {
            this.taskFactories = new Dictionary<string, List<IScrapingTaskFactory>>();
            foreach (var f in scrapingTaskFactories)
            {
                var name = CleanName(f.GetTaskName());
                if (String.IsNullOrWhiteSpace(name))
                {
                    throw new Exception($"Task name for factory {f.GetType().Name} was empty.");
                }

                if (!taskFactories.ContainsKey(name))
                {
                    taskFactories.Add(name, new List<IScrapingTaskFactory>());
                }
                taskFactories[name].Add(f);
            }
        }

        protected List<IScrapingTaskFactory> GetFactory(string taskName)
        {
            var cleanTaskName = CleanName(taskName);
            return !taskFactories.ContainsKey(cleanTaskName) ? null : taskFactories[cleanTaskName];
        }

        protected async Task HandlePayload(TaskPayload payload)
        {
            var factories = GetFactory(payload.Task);
            if (factories == null || factories.Count == 0)
            {
                log.Error($"Unknown task type \"{payload.Task}\"");
                return;
            }

            foreach (var factory in factories)
            {
                IScrapingTask scrapingTask;

                try
                {
                    scrapingTask = factory.GetTaskInstance();
                    Debug.WriteLine($"Working with task {scrapingTask.GetType().FullName}");
                    Console.WriteLine($"Working with task {scrapingTask.GetType().FullName}");
                    log.Info($"Working with task {scrapingTask.GetType().FullName}");
                } catch (Exception exp)
                {
                    log.Error(exp, $"Where unable to get task instance from {factory.GetType().Name} for task {payload.Task}.");
                    await AddFailedTask(payload, exp);
                    continue;
                }

                try
                {
                    await scrapingTask.Execute(this, payload.Payload);
                } catch (Exception exp)
                {
                    log.Error(exp, $"Error while execute {scrapingTask.GetType().Name}.");
                    await AddFailedTask(payload, exp);
                }
            }
        }

        private string CleanName(string name)
        {
            return name?.Trim().ToUpper();
        }

        public abstract Task AddFailedTask(TaskPayload payload, Exception exp = null);

        public abstract Task AddTask(TaskPayload newTask);

        public abstract Task Start();

        public abstract Task Stop();
    }
}
