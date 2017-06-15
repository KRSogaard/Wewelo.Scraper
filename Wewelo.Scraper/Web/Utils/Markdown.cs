using System;
using System.Collections.Generic;
using System.Text;
using Wewelo.Common;

namespace Wewelo.Scraper.Web.Utils
{
    public static class Markdown
    {
        public static string HtmlToMarkdown(string html)
        {
            if (String.IsNullOrWhiteSpace(html))
                return null;

            html = html.Replace("<style>(.*?)</style>", html);

            html = html.Replace("&nbsp;", " ");
            html = RegexHelper.Replace(@"\s*<br\s*\/*>\s*", "\n", html);
            html = RegexHelper.Replace(@"\s*<\/*p>\s*", "\n", html);

            // Headers
            html = RegexHelper.Replace(@"<h1>\s*", "# ", html);
            html = RegexHelper.Replace(@"<h2>\s*", "## ", html);
            html = RegexHelper.Replace(@"<h3>\s*", "### ", html);
            html = RegexHelper.Replace(@"<h4>\s*", "#### ", html);
            html = RegexHelper.Replace(@"<h5>\s*", "##### ", html);
            html = RegexHelper.Replace(@"<h6>\s*", "###### ", html);
            // Remove closing tag
            html = RegexHelper.Replace(@"<\/h\d+>\s*", "", html);

            // Bold
            html = RegexHelper.Replace(@"<\/*b>[\t\f ]*", "**", html);
            // Italic
            html = RegexHelper.Replace(@"<\/*i>\s*", "*", html);

            html = RegexHelper.Replace(@"<li>\s*", "* ", html);

            // remove all html tags
            html = RegexHelper.Replace(@"<[^>]*>", "", html);

            // Try and fix spacing
            html = RegexHelper.Replace(@"[\n\r]{3,}", "\n", html);
            html = RegexHelper.Replace(@"[\t\f ]{2,}", " ", html);

            StringBuilder builder = new StringBuilder();
            foreach (var line in html.Split('\n'))
            {
                builder.AppendLine(line.Trim());
            }

            return builder.ToString().Trim();
        }
    }
}
