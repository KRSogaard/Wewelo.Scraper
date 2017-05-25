using System;

namespace Wewelo.Scraper.Exceptions
{
    public class ScraperException : Exception
    {
        public ScraperException(string message) : base(message)
        {
        }
        public ScraperException(string message, Exception exp) : base(message, exp)
        {
        }
    }
}
