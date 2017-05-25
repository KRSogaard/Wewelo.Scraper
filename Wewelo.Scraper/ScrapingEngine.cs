using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AmasTaskRunner;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using NLog;
using Wewelo.Common;
using Wewelo.Scraper.Exceptions;

namespace Wewelo.Scraper
{
    public class ScrapingEngineConfig
    {
        public string TaskFailureBucket;
        public string SQSTaskQueue;
        public int Threads;
    }

    public class ScrapingEngine
    {
        private static Logger log = LogManager.GetCurrentClassLogger();
        private IAmazonS3 s3Client;
        private IAmazonSQS sqsClient;
        private ScrapingEngineConfig config;
        private Dictionary<string, List<IScrapingTaskFactory>> taskFactories;
        private SQSConsumer sqsConsumer;

        public ScrapingEngine(IAmazonS3 s3Client, IAmazonSQS sqsClient, 
            ScrapingEngineConfig config, List<IScrapingTaskFactory> taskFactories)
        {
            this.s3Client = s3Client;
            this.sqsClient = sqsClient;
            this.config = config;
            
            PopulateFactories(taskFactories);

            sqsConsumer = new SQSConsumer(sqsClient, new SQSConsumerConfig()
            {
                Threads = config.Threads,
                FetchSize = 1,
                QueueUrl = config.SQSTaskQueue,
                VisabilityTimeOut = TimeSpan.FromMinutes(5)
            });
        }

        private void PopulateFactories(List<IScrapingTaskFactory> scrapingTaskFactories)
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

        private string CleanName(string name)
        {
            return name?.Trim().ToUpper();
        }

        public Task Start()
        {
            return sqsConsumer.Start(OnSQSMessage);
        }

        protected async Task OnSQSMessage(string body)
        {
            Validator.NonEmpty("body", body);

            JObject taskObject;
            try
            {
                taskObject = JObject.Parse(body);
            }
            catch (Exception exp)
            {
                log.Error(exp, $"Where unable to parse the task json, \"{body}\".");
                return;
            }
            
            log.Info("Incomming Task: " + body);
            var taskName = GetTaskName(taskObject);

            var factories = GetFactory(taskName);
            if (factories == null || factories.Count == 0)
            {
                log.Error($"Unknown task type \"{taskName}\"");
                return;
            }

            var payload = GetPayload(taskObject);
            foreach (var factory in factories)
            {
                IScrapingTask scrapingTask;

                try
                {
                    scrapingTask = factory.GetTaskInstance();
                }
                catch (Exception exp)
                {
                    log.Error(exp, $"Where unable to get task instance from {factory.GetType().Name} for task {taskName}.");
                    continue;
                }

                try
                {
                    scrapingTask.Execute(this, payload);
                }
                catch (Exception exp)
                {
                    log.Error(exp, $"Error while execute {scrapingTask.GetType().Name}.");
                    await AddFailedTask(taskName, payload, exp);
                }
            }
        }

        private string GetPayload(JObject taskObject)
        {
            return taskObject["payload"]?.ToString();
        }

        private List<IScrapingTaskFactory> GetFactory(string taskName)
        {
            var cleanTaskName = CleanName(taskName);
            return !taskFactories.ContainsKey(cleanTaskName) ? null : taskFactories[cleanTaskName];
        }

        private String GetTaskName(JObject taskObject)
        {
            if (taskObject["task"] == null)
            {
                throw new ScraperException("Task object was missing, the task property.");
            }

            try
            {
                return taskObject["task"].Value<string>();
            }
            catch (Exception exp)
            {
                throw new ScraperException($"Task object was malformed, \"{taskObject["task"]}\".", exp);
            }
        }

        public Task AddTask(string taskName, string payload = null)
        {
            JObject json = new JObject();
            json.Add("task", taskName);
            json.Add("payload", payload);

            log.Info("Sending SQS message: " + json);
            return sqsClient.SendMessageAsync(new SendMessageRequest(config.SQSTaskQueue, json.ToString()));
        }

        public Task AddFailedTask(String taskName, string payload, Exception exp = null)
        {
            string exception = JsonConvert.SerializeObject(new 
            {
                TakeType = taskName,
                Exception = exp,
                Payload = payload
            }, Formatting.Indented, new JsonSerializerSettings
            {
                Converters = new List<JsonConverter>() { new IsoDateTimeConverter(), new ExceptionJsonConverter() },
                ContractResolver = new CamelCasePropertyNamesContractResolver()
                {
                    IgnoreSerializableInterface = true
                }
            });

            log.Error(exception);
            var request = new PutObjectRequest
            {
                BucketName = config.TaskFailureBucket,
                Key = DateTime.UtcNow.ToString("yyyy-MM-dd-HH-mm-ss-ffff") + "." + taskName + ".json",
                ContentBody = exception
            };
            return s3Client.PutObjectAsync(request, CancellationToken.None);
        }
    }
}
