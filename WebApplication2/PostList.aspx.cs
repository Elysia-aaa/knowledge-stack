using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using WebApplication2.DAL;
using WebApplication2.Utils;
namespace WebApplication2
{
    public partial class PostList:Page
    {
        protected string PostsHtml="",PagerHtml="",EmptyHtml="",KeywordValue="",CategoryValue="",TagValue="",SortValue="newest",CategoryOptions="",TagOptions="";
        protected int TotalCount;
        protected void Page_Load(object sender,EventArgs e)
        {
            KeywordValue=(Request.QueryString["q"]??"").Trim();if(KeywordValue.Length>100)KeywordValue=KeywordValue.Substring(0,100);
            CategoryValue=Request.QueryString["category"]??"";TagValue=Request.QueryString["tag"]??"";SortValue=Request.QueryString["sort"]??"newest";
            int page=1;Int32.TryParse(Request.QueryString["page"],out page);page=Math.Max(1,Math.Min(page,100000));
            int total;var posts=PostDAL.GetPaged(page,6,KeywordValue,CategoryValue,TagValue,SortValue,out total);TotalCount=total;
            PostsHtml=HtmlRenderer.PostListHtml(posts,false);
            if(posts.Count==0)EmptyHtml="<div class=\"card empty\">没有找到相关文章。试试清除筛选，或换一个关键词。</div>";
            var tags=new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
            using(var db=new CommunityDAL())foreach(var row in db.Rows("SELECT p.Tags FROM Posts p WHERE "+PostDAL.PublicCondition))
            {foreach(string tag in PostDAL.SplitTags(Convert.ToString(row["Tags"])))tags.Add(tag);}
            CategoryOptions=Options(TaxonomyDAL.GetCategories(),CategoryValue);TagOptions=Options(tags,TagValue);
            int pages=(int)Math.Ceiling(total/6.0);var pager=new StringBuilder();
            if(pages>1)
            {
                pager.Append("<div class=\"pagination\">");
                if(page>1)pager.Append(Link(page-1,"上一页"));
                for(int n=Math.Max(1,page-2);n<=Math.Min(pages,page+2);n++)pager.Append(n==page?"<span class=\"current\">"+n+"</span>":Link(n,n.ToString()));
                if(page<pages)pager.Append(Link(page+1,"下一页"));pager.Append("</div>");
            }
            PagerHtml=pager.ToString();
        }
        private static string Options(IEnumerable<string> items,string selected)
        {return String.Join("",items.Select(x=>"<option value=\""+HttpUtility.HtmlAttributeEncode(x)+"\""+(x==selected?" selected":"")+">"+HttpUtility.HtmlEncode(x)+"</option>"));}
        private string Link(int page,string label)
        {return "<a href=\"PostList.aspx?page="+page+"&amp;q="+HttpUtility.UrlEncode(KeywordValue)+"&amp;category="+HttpUtility.UrlEncode(CategoryValue)+"&amp;tag="+HttpUtility.UrlEncode(TagValue)+"&amp;sort="+HttpUtility.UrlEncode(SortValue)+"\">"+label+"</a>";}
    }
}
