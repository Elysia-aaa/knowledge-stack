using System;
using System.Collections.Generic;
using System.Web.UI;
using WebApplication2.DAL;
using WebApplication2.Models;
using WebApplication2.Utils;

namespace WebApplication2
{
    public partial class MyPosts : System.Web.UI.Page
    {
        protected string PostsHtml = "";
        protected string EmptyHtml = "";

        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["UserId"] == null)
            {
                Response.Redirect("Login.aspx");
                return;
            }
            int userId = Convert.ToInt32(Session["UserId"]);

            if (!IsPostBack)
            {
                List<Post> posts = PostDAL.GetByUser(userId);
                if (posts.Count == 0)
                {
                    EmptyHtml = "<div class=\"card empty\">你还没有发布过文章，<a href=\"PostEdit.aspx\">写一篇吧</a>！</div>";
                    PostsHtml = "";
                }
                else
                {
                    PostsHtml = HtmlRenderer.PostListHtml(posts, true);
                    EmptyHtml = "";
                }
            }
        }
    }
}
