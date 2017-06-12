namespace Wewelo.Scraper.Web
{
    public interface IWebFetcherFactory
    {
        IWebFetcher GetInstance();
    }
}