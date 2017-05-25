using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SQS;
using Moq;
using Wewelo.Scraper.Exceptions;
using Wewelo.Scraper.Tests.Extensions;
using Xunit;

namespace Wewelo.Scraper.Tests
{
    public class EngineUnitTests
    {
        [Fact]
        public void TestNoTaskName()
        {
            Mock<IAmazonS3> s3Mock = new Mock<IAmazonS3>();
            Mock<IAmazonSQS> sqsMock = new Mock<IAmazonSQS>();
            ScrapingEngineConfig config = new ScrapingEngineConfig();
            List<IScrapingTaskFactory> taskFactories = new List<IScrapingTaskFactory>();

            string messageBody = "{}";

            var engine = new ScrapingEngineFake(s3Mock.Object, sqsMock.Object, config, taskFactories);
            AggregateException exp = (AggregateException)Record.Exception(() => engine.CallOnSQSMessage(messageBody));
            Assert.IsType(typeof(ScraperException), exp.InnerException);
        }

        [Fact]
        public void TestInvalidTaskName()
        {
            Mock<IAmazonS3> s3Mock = new Mock<IAmazonS3>();
            Mock<IAmazonSQS> sqsMock = new Mock<IAmazonSQS>();
            ScrapingEngineConfig config = new ScrapingEngineConfig();
            List<IScrapingTaskFactory> taskFactories = new List<IScrapingTaskFactory>();

            string messageBody = "{ \"task\": \"UnknownTask\" }";

            var engine = new ScrapingEngineFake(s3Mock.Object, sqsMock.Object, config, taskFactories);
            var exp = Record.Exception(() => engine.CallOnSQSMessage(messageBody));
            Assert.Null(exp);
        }

        [Fact]
        public void TestTasksIsCalled()
        {
            string taskName = "TaskNameOne";
            int methodCalls = 0;
            Mock<IAmazonS3> s3Mock = new Mock<IAmazonS3>();
            Mock<IAmazonSQS> sqsMock = new Mock<IAmazonSQS>();
            ScrapingEngineConfig config = new ScrapingEngineConfig();
            List<IScrapingTaskFactory> taskFactories = new List<IScrapingTaskFactory>();
            taskFactories.Add(new ScrapingTaskFactoryTestClass(taskName, (scrapingEngine, s) =>
            {
                methodCalls++;
            }));

            string messageBody = "{" + $"\"task\": \"{taskName}\"" + "}";

            var engine = new ScrapingEngineFake(s3Mock.Object, sqsMock.Object, config, taskFactories);
            engine.CallOnSQSMessage(messageBody);

            Assert.Equal(1, methodCalls);
        }

        [Fact]
        public void TestMultiTasksOfSameTypeIsCalled()
        {
            string taskName = "TaskNameOne";
            int method1Calls = 0;
            int method2Calls = 0;
            Mock<IAmazonS3> s3Mock = new Mock<IAmazonS3>();
            Mock<IAmazonSQS> sqsMock = new Mock<IAmazonSQS>();
            ScrapingEngineConfig config = new ScrapingEngineConfig();
            List<IScrapingTaskFactory> taskFactories = new List<IScrapingTaskFactory>();
            taskFactories.Add(new ScrapingTaskFactoryTestClass(taskName, (scrapingEngine, s) =>
            {
                method1Calls++;
            }));
            taskFactories.Add(new ScrapingTaskFactoryTestClass(taskName, (scrapingEngine, s) =>
            {
                method2Calls++;
            }));

            string messageBody = "{" + $"\"task\": \"{taskName}\"" + "}";

            var engine = new ScrapingEngineFake(s3Mock.Object, sqsMock.Object, config, taskFactories);
            engine.CallOnSQSMessage(messageBody);

            Assert.Equal(1, method1Calls);
            Assert.Equal(1, method2Calls);
        }
        [Fact]
        public void TestOnlyCorrectTaskTypeIsCalled()
        {
            string taskName = "TaskNameOne";
            string taskNameOther = "TaskNameOneOther";
            int method1Calls = 0;
            int method2Calls = 0;
            Mock<IAmazonS3> s3Mock = new Mock<IAmazonS3>();
            Mock<IAmazonSQS> sqsMock = new Mock<IAmazonSQS>();
            ScrapingEngineConfig config = new ScrapingEngineConfig();
            List<IScrapingTaskFactory> taskFactories = new List<IScrapingTaskFactory>();
            taskFactories.Add(new ScrapingTaskFactoryTestClass(taskName, (scrapingEngine, s) =>
            {
                method1Calls++;
            }));
            taskFactories.Add(new ScrapingTaskFactoryTestClass(taskNameOther, (scrapingEngine, s) =>
            {
                method2Calls++;
            }));

            string messageBody = "{" + $"\"task\": \"{taskName}\"" + "}";

            var engine = new ScrapingEngineFake(s3Mock.Object, sqsMock.Object, config, taskFactories);
            engine.CallOnSQSMessage(messageBody);

            Assert.Equal(1, method1Calls);
            Assert.Equal(0, method2Calls);
        }

        [Fact]
        public void TestTaskThrowsException()
        {
            string taskName = "TaskNameOne";

            Mock<IAmazonS3> s3Mock = new Mock<IAmazonS3>();
            Mock<IAmazonSQS> sqsMock = new Mock<IAmazonSQS>();
            ScrapingEngineConfig config = new ScrapingEngineConfig();
            List<IScrapingTaskFactory> taskFactories = new List<IScrapingTaskFactory>();
            taskFactories.Add(new ScrapingTaskFactoryTestClass(taskName, (scrapingEngine, s) =>
            {
                throw new Exception("This is an exception!");
            }));

            int calls = 0;
            s3Mock.Setup(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<PutObjectResponse>(null))
                .Callback(() => calls++);

            string messageBody = "{" + $"\"task\": \"{taskName}\"" + "}";

            var engine = new ScrapingEngineFake(s3Mock.Object, sqsMock.Object, config, taskFactories);
            engine.CallOnSQSMessage(messageBody);

            Assert.Equal(1, calls);
        }
    }
}
