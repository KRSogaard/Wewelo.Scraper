using System;
using System.Collections.Generic;
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

namespace Wewelo.Scraper.Engines
{
    public class SQSScrapingEngineConfig
    {
        public string TaskFailureBucket;
        public string SQSTaskQueue;
        public int Threads;
    }

    public class SQSScrapingEngine : BaseScapingEngine, IScrapingEngine
    {
        private static Logger log = LogManager.GetCurrentClassLogger();
        private IAmazonS3 s3Client;
        private IAmazonSQS sqsClient;
        private SQSScrapingEngineConfig config;
        private SQSConsumer sqsConsumer;

        public SQSScrapingEngine(IAmazonS3 s3Client, IAmazonSQS sqsClient,
            SQSScrapingEngineConfig config, List<IScrapingTaskFactory> taskFactories)
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

        public override Task Start()
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
            // temp hax
            if (String.Equals(taskName, "ItemParserPayload", StringComparison.CurrentCultureIgnoreCase))
            {
                taskName = "ItemParser";
            }

            var payload = GetPayload(taskObject);

            await HandlePayload(new TaskPayload(taskName, payload));
        }

        private string GetPayload(JObject taskObject)
        {
            return taskObject["payload"]?.ToString();
        }

        private string GetTaskName(JObject taskObject)
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

        public override Task AddTask(TaskPayload newTask)
        {
            JObject json = new JObject();
            json.Add("task", newTask.Task);
            json.Add("payload", newTask.Payload);

            log.Info("Sending SQS message: " + json);
            return sqsClient.SendMessageAsync(new SendMessageRequest(config.SQSTaskQueue, json.ToString()));
        }

        public override Task AddFailedTask(TaskPayload task, Exception exp = null)
        {
            string exception = JsonConvert.SerializeObject(new 
            {
                TakeType = task.Task,
                Exception = exp,
                Payload = task.Payload
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
                Key = DateTime.UtcNow.ToString("yyyy-MM-dd-HH-mm-ss-ffff") + "." + task.Task + ".json",
                ContentBody = exception
            };
            return s3Client.PutObjectAsync(request, CancellationToken.None);
        }

        public override Task Stop()
        {
            return sqsConsumer.Stop();
        }
    }
}
