using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Web;
using WebApplication2.DAL;
using WebApplication2.Models;

namespace WebApplication2.Utils
{
    /// <summary>
    /// 文章卡片 / 列表的 HTML 渲染工具类。
    /// 把 Post 实体对象转换成统一的 HTML 卡片，供首页、列表页、我的文章页复用。
    /// </summary>
    public static class HtmlRenderer
    {
        /// <summary>渲染一张文章卡片，showActions 为 true 时额外显示「编辑/删除」按钮</summary>
        public static string PostCard(Post p, bool showActions)
        {
            StringBuilder sb = new StringBuilder();
            string title = HttpUtility.HtmlEncode(p.Title);
            string author = HttpUtility.HtmlEncode(string.IsNullOrEmpty(p.AuthorNickname) ? p.AuthorName : p.AuthorNickname);
            string summary = HttpUtility.HtmlEncode(GetSummary(p));
            string time = p.CreatedAt.ToString("yyyy-MM-dd HH:mm");

            sb.Append("<article class=\"card post-card\">");
            if(showActions&&p.IsHidden)sb.Append("<p class=\"notice\">该文章已被管理员隐藏</p>");
            sb.Append("<h2 class=\"post-title\"><a href=\"PostDetail.aspx?id=" + p.PostId + "\">" + title + "</a></h2>");
            sb.Append("<div class=\"post-meta\">");
            sb.Append("<a href=\"Community.aspx?view=profile&id="+p.UserId+"\">" + author + "</a>");
            sb.Append(BuildCategories(p.Category));
            sb.Append("<span>" + time + "</span>");
            sb.Append("<span>" + p.LikeCount + " 赞</span>");
            sb.Append("<span>" + p.ViewCount + " 浏览</span>");
            sb.Append("</div>");

            if (!string.IsNullOrEmpty(summary))
            {
                sb.Append("<p class=\"post-summary\">" + summary + "</p>");
            }

            string tags = BuildTags(p.Tags);
            if (!string.IsNullOrEmpty(tags))
            {
                sb.Append("<div class=\"post-tags\">" + tags + "</div>");
            }

            if (showActions)
            {
                sb.Append("<div class=\"post-actions\">");
                sb.Append("<a class=\"btn btn-sm btn-primary\" href=\"PostEdit.aspx?id=" + p.PostId + "\">编辑</a> ");
                sb.Append("<button type=\"button\" class=\"btn btn-sm btn-danger\" onclick=\"deletePost(" + p.PostId + ",'MyPosts.aspx')\">删除</button>");
                sb.Append("</div>");
            }

            sb.Append("</article>");
            return sb.ToString();
        }

        /// <summary>渲染整组文章卡片</summary>
        public static string PostListHtml(List<Post> posts, bool showActions)
        {
            StringBuilder sb = new StringBuilder();
            foreach (Post p in posts)
            {
                sb.Append(PostCard(p, showActions));
            }
            return sb.ToString();
        }

        /// <summary>取文章摘要：优先用 Summary，为空则截取正文前 120 字符</summary>
        private static string GetSummary(Post p)
        {
            if (!string.IsNullOrEmpty(p.Summary))
            {
                return p.Summary.Length > 120 ? p.Summary.Substring(0, 120) + "…" : p.Summary;
            }
            if (!string.IsNullOrEmpty(p.Content))
            {
                string plain = p.Content.Replace("\r", " ").Replace("\n", " ").Trim();
                if (plain.Length > 120)
                {
                    return plain.Substring(0, 120) + "…";
                }
                return plain;
            }
            return "";
        }

        /// <summary>把标签字符串拆分成 #标签 样式的小标签</summary>
        public static string BuildCategories(string categories)
        {
            var names=TaxonomyDAL.SplitCategories(categories);
            if(names.Length==0)return "<span class=\"muted\">未分类</span>";
            return String.Join(" ",names.Select(name=>"<a class=\"category-link\" href=\"PostList.aspx?category="+HttpUtility.UrlEncode(name)+"\">"+HttpUtility.HtmlEncode(name)+"</a>"));
        }

        private static string BuildTags(string tags)
        {
            if (string.IsNullOrWhiteSpace(tags))
            {
                return "";
            }
            string[] arr = PostDAL.SplitTags(tags);
            StringBuilder sb = new StringBuilder();
            foreach (string t in arr)
            {
                sb.Append("<a class=\"tag\" href=\"PostList.aspx?tag="+HttpUtility.UrlEncode(t)+"\">#" + HttpUtility.HtmlEncode(t) + "</a>");
            }
            return sb.ToString();
        }
    }
}
