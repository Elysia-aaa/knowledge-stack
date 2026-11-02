using System;
using System.Text.RegularExpressions;
using System.Web;

namespace WebApplication2.Utils
{
    /// <summary>
    /// 简单的 Markdown 转 HTML 工具类。
    /// 先对整个文本做 HTML 转义（防止 XSS），再做少量语法转换，
    /// 支持：代码块、标题、粗体、斜体、行内代码、链接、无序列表、换行。
    /// </summary>
    public static class MarkdownHelper
    {
        public static string ToHtml(string markdown)
        {
            if (string.IsNullOrEmpty(markdown))
            {
                return "";
            }

            // 1. 先转义，避免用户输入 <script> 等造成 XSS
            string html = HttpUtility.HtmlEncode(markdown);

            // 2. 代码块 ``` ... ```
            html = Regex.Replace(html, @"```([\s\S]*?)```",
                m => "<pre><code>" + m.Groups[1].Value.Trim('\r', '\n') + "</code></pre>");

            // 3. 标题
            html = Regex.Replace(html, @"^### (.*)$", "<h3>$1</h3>", RegexOptions.Multiline);
            html = Regex.Replace(html, @"^## (.*)$", "<h2>$1</h2>", RegexOptions.Multiline);
            html = Regex.Replace(html, @"^# (.*)$", "<h1>$1</h1>", RegexOptions.Multiline);

            // 4. 粗体 **文字**
            html = Regex.Replace(html, @"\*\*(.+?)\*\*", "<strong>$1</strong>");
            // 5. 斜体 *文字*
            html = Regex.Replace(html, @"\*(.+?)\*", "<em>$1</em>");
            // 6. 行内代码 `code`
            html = Regex.Replace(html, @"`([^`]+)`", "<code>$1</code>");
            // 7. 链接 [文字](http://...)
            html = Regex.Replace(html, @"!\[([^\]\r\n]*)\]\(Handlers/MediaHandler\.ashx\?id=(\d+)\)",
                "<img src=\"Handlers/MediaHandler.ashx?id=$2\" alt=\"$1\" loading=\"lazy\" />");
            html = Regex.Replace(html, @"\[([^\]]+)\]\((https?://[^\)]+)\)",
                "<a href=\"$2\" target=\"_blank\" rel=\"noopener\">$1</a>");

            // 8. 无序列表 "- 项目"
            html = Regex.Replace(html, @"^\- (.+)$", "• $1", RegexOptions.Multiline);

            // 9. 换行 -> <br>
            html = html.Replace("\r\n", "\n").Replace("\n", "<br>");

            return html;
        }
    }
}
