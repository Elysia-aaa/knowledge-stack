using System;
using System.Web;
using System.Web.UI;
using WebApplication2.DAL;
using WebApplication2.Models;
using WebApplication2.Utils;

namespace WebApplication2
{
    public partial class PostDetail : System.Web.UI.Page
    {
        protected string PostTitleHtml = "";
        protected string MetaHtml = "";
        protected string ContentHtml = "";
        protected string OwnerActionsHtml = "";
        protected int PostId = 0;
        protected bool IsLiked = false;
        protected int LikeCount = 0;
        protected bool PublicVisible=false;

        protected void Page_Load(object sender, EventArgs e)
        {
            int id;
            if (!int.TryParse(Request.QueryString["id"], out id))
            {
                Response.Redirect("PostList.aspx");
                return;
            }

            Post post = PostDAL.GetById(id);
            int me=Session["UserId"]==null?0:Convert.ToInt32(Session["UserId"]);
            if (post == null || post.DeletedAt.HasValue || (!PostDAL.IsPublic(post)&&post.UserId!=me))
            {
                Response.StatusCode=404;
                Response.TrySkipIisCustomErrors=true;
                PostTitleHtml="文章不存在或当前不可见";
                return;
            }
            PublicVisible=PostDAL.IsPublic(post);

            // 浏览量 +1（首次加载时）
            if (!IsPostBack && PublicVisible && post.UserId!=me)
            {
                PostDAL.RecordView(id,WebApplication2.Services.AnalyticsService.ReaderKey(Context));
                post=PostDAL.GetById(id);
            }

            PostId = post.PostId;
            PostTitleHtml = HttpUtility.HtmlEncode(post.Title);

            string author = string.IsNullOrEmpty(post.AuthorNickname) ? post.AuthorName : post.AuthorNickname;
            MetaHtml = "<a href=\"Community.aspx?view=profile&id="+post.UserId+"\">" + HttpUtility.HtmlEncode(author) +"</a>"+
                       "　"+HtmlRenderer.BuildCategories(post.Category)+
                       "　" + post.CreatedAt.ToString("yyyy-MM-dd HH:mm") +
                       "　浏览 " + post.ViewCount;
            foreach(string tag in PostDAL.SplitTags(post.Tags))MetaHtml+=" <a class=\"tag\" href=\"PostList.aspx?tag="+HttpUtility.UrlEncode(tag)+"\">#"+HttpUtility.HtmlEncode(tag)+"</a>";
            if(!PublicVisible)MetaHtml+="<p class=\"notice\">"+(post.IsHidden?"此文章已被管理员隐藏，仅作者可见":"这是一篇草稿，仅作者可见")+"</p>";

            // Markdown 转 HTML
            ContentHtml = MarkdownHelper.ToHtml(post.Content);

            LikeCount = post.LikeCount;

            // 登录用户可点赞；作者本人可看到「编辑/删除」按钮（简单权限控制）
            if (Session["UserId"] != null)
            {
                int userId = Convert.ToInt32(Session["UserId"]);
                IsLiked = LikeDAL.HasLiked(userId, post.PostId);
                if (post.UserId == userId)
                {
                    OwnerActionsHtml =
                        "<a class=\"btn btn-primary\" href=\"PostEdit.aspx?id=" + post.PostId + "\">编辑</a> " +
                        "<button type=\"button\" class=\"btn btn-danger\" onclick=\"deletePost(" + post.PostId + ")\">删除</button>";
                    if(post.Status=="Published")OwnerActionsHtml+=" <button type=\"button\" class=\"btn btn-ghost\" data-action=\"withdraw\" data-id=\""+post.PostId+"\">撤回草稿</button>";
                }
            }
        }
    }
}
