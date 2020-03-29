using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using Wewelo.Scraper.Exceptions;

namespace Wewelo.Scraper.Engines
{
    public class LocalScrapingEngine : BaseScapingEngine, IScrapingEngine
    {
        private static Logger log = LogManager.GetCurrentClassLogger();
        private static int sleepTime = 250;
        private object lockingObject = new Object();
        private ConcurrentQueue<TaskPayload> payloads;
        private Thread[] threads;
        private bool[] threadStatus;
        private bool[] threadWorkingStatus;
        private int workers;
        private bool run;
        private int shutdownWaitMS;
        private Action<TaskPayload, Exception> failureHandler;
        private bool shutDownWhenNoTasks;

        public LocalScrapingEngine(int workers, 
            Action<TaskPayload, Exception> failureHandler, 
            List<IScrapingTaskFactory> taskFactories,
            bool shutDownWhenNoTasks = false, 
            int shutdownWaitMS = 1000)
        {
            this.workers = workers;
            this.shutdownWaitMS = shutdownWaitMS;
            this.shutDownWhenNoTasks = shutDownWhenNoTasks;
            this.failureHandler = failureHandler;
            payloads = new ConcurrentQueue<TaskPayload>();

            PopulateFactories(taskFactories);

            log.Info($"Creating local scraping engine with {workers} threads");

        }

        private void createThreads()
        {
            threads = new Thread[workers];
            threadStatus = new bool[workers];
            threadWorkingStatus = new bool[workers];

            for (int i = 0; i < workers; i++)
            {
                int threadIndex = i;
                log.Debug($"Creating thread {threadIndex}");
                Thread t = new Thread(() => ThreadAction(threadIndex));
                t.Start();

                threads[i] = t;
            }
        }

        private void ThreadAction(int index)
        {
            try
            {
                threadStatus[index] = true;

                TaskPayload payload = null;
                while (run) {
                    try
                    {
                        payload = null;
                        if (payloads.IsEmpty)
                        {
                            lock (lockingObject)
                            {
                                if (!run)
                                {
                                    break;
                                }

                                if (shutDownWhenNoTasks && !HasWorkingThread())
                                {
                                    log.Info("Queue is empty and no thread is working, shutting down.");
                                    Stop(); // Don't wait so the thread is shutting down
                                    break;
                                }
                            }

                            log.Debug("No messages to be processed");
                            Thread.Sleep(sleepTime);
                            continue;
                        }

                        lock (lockingObject)
                        {
                            if (!payloads.TryDequeue(out payload))
                            {
                                log.Debug("Was unable to get message from the queue");
                                Thread.Sleep(sleepTime);
                            }
                        }

                        threadWorkingStatus[index] = true;
                        log.Debug($"Thead {index} has started working");
                        if (payload == null)
                        {
                            Console.WriteLine("Wait");
                        }
                        HandlePayload(payload).Wait();
                        log.Debug($"Thead {index} is done working");
                        threadWorkingStatus[index] = false;

                    } catch (Exception exp)
                    {
                        log.Error(exp, $"Exception while executing job: {exp.Message}");
                        if (payload != null)
                        {
                            AddFailedTask(payload, exp).Wait();
                        }
                        continue;
                    }
                }
                log.Info($"Task thread {index} is stopping");
            } finally
            {
                threadWorkingStatus[index] = false;
                threadStatus[index] = false;
            }
            log.Info($"Task thread {index} is stopped");
        }

        public override Task AddFailedTask(TaskPayload payload, Exception exp = null)
        {
            // May add this later
            log.Error(exp, $"Task {payload.Task} failed for payload: {payload.Payload}");
            return Task.Run(() => failureHandler.Invoke(payload, exp));
        }

        public override Task AddTask(TaskPayload newTask)
        {
            return Task.Run(() => {
                lock (lockingObject)
                {
                    log.Debug("Adding new task to queue size before: " + payloads.Count);
                    payloads.Enqueue(newTask);
                    log.Debug("Adding new task to queue size before: " + payloads.Count);
                }
            });
        }

        public override Task Start()
        {
            return Task.Run(() =>
            {
                lock(lockingObject)
                {
                    if (run || HasRunningThread())
                    {
                        throw new ScraperException("Scraper engine is already running");
                    }

                    log.Info("Starting scraper engine");
                    run = true;
                    createThreads();
                }

                while (run || HasRunningThread())
                {
                    Thread.Sleep(100);
                }
            });
        }

        public override Task Stop()
        {
            return Task.Run(() =>
            {
                lock (lockingObject)
                {
                    run = false;
                    DateTime start = DateTime.UtcNow;
                    while(HasRunningThread())
                    {
                        if ((DateTime.UtcNow - start).Milliseconds > shutdownWaitMS)
                        {
                            log.Error($"Failed to shutdown after {(DateTime.UtcNow - start).Milliseconds} ms");
                            throw new ScraperException($"Failed to shutdown after {(DateTime.UtcNow - start).Milliseconds} ms");
                        }
                        log.Debug($"Waiting for threads to shutdown, sleeping a bit");
                        Thread.Sleep(100);
                    }
                }
            });
        }

        private bool HasRunningThread()
        {
            if (threads == null)
            {
                return false;
            }

            for (int i = 0; i < threads.Length; i++)
            {
                if (threadStatus[i])
                {
                    return true;
                }
            }
            return false;
        }

        private bool HasWorkingThread()
        {
            if (threads == null)
            {
                return false;
            }

            for (int i = 0; i < threads.Length; i++)
            {
                if (threadWorkingStatus[i])
                {
                    return true;
                }
            }
            return false;
        }
    }
}
