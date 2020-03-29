using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Wewelo.Scraper
{
    public class TaskPayload
    {
        public string Task { get; set; }
        public string Payload { get; set; }

        public TaskPayload()
        {

        }
        public TaskPayload(string task, string payload)
        {
            this.Task = task;
            this.Payload = payload;
        }
    }
}
