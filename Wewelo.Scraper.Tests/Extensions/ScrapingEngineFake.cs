using System.Collections.Generic;
using Amazon.S3;
using Amazon.SQS;

namespace Wewelo.Scraper.Tests.Extensions
{
    public class ScrapingEngineFake : ScrapingEngine
    {
        public ScrapingEngineFake(IAmazonS3 s3Client, IAmazonSQS sqsClient, ScrapingEngineConfig config, List<IScrapingTaskFactory> taskFactories) 
            : base(s3Client, sqsClient, config, taskFactories)
        {
        }

        public void CallOnSQSMessage(string payload)
        {
            this.OnSQSMessage(payload).Wait();
        }
    }
}