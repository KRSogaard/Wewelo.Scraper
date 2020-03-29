using System.Collections.Generic;
using Amazon.S3;
using Amazon.SQS;
using Wewelo.Scraper.Engines;

namespace Wewelo.Scraper.Tests.Extensions
{
    public class ScrapingEngineFake : SQSScrapingEngine
    {
        public ScrapingEngineFake(IAmazonS3 s3Client, IAmazonSQS sqsClient, SQSScrapingEngineConfig config, List<IScrapingTaskFactory> taskFactories) 
            : base(s3Client, sqsClient, config, taskFactories)
        {
        }

        public void CallOnSQSMessage(string payload)
        {
            this.OnSQSMessage(payload).Wait();
        }
    }
}