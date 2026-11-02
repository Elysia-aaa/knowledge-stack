using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using WebApplication2.DAL;
using WebApplication2.Models;
using WebApplication2.Utils;

namespace WebApplication2
{
    public partial class Default : System.Web.UI.Page
    {
        protected string UserCardHtml = "";
        protected string PostsHtml = "";
        protected string EmptyHtml = "";

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                LoadUserCard();
                LoadRecentPosts();
            }
        }

        /// <summary>
        /// 首页左侧个人中心卡片：登录后展示当前账号的详细信息（来自 Session + 数据库）。
        /// </summary>
        private void LoadUserCard()
        {
            if (Session["UserId"] == null)
            {
                UserCardHtml = "<div class=\"card profile-card\">" +
                    "<div class=\"avatar\"><svg class=\"icon\" aria-hidden=\"true\"><use href=\"Content/icons.svg#user\"></use></svg></div>" +
                    "<p>尚未登录</p>" +
                    "<p><a class=\"btn\" href=\"Login.aspx\">登录</a> <a class=\"btn btn-ghost\" href=\"Register.aspx\">注册</a></p>" +
                    "</div>";
                return;
            }

            int userId = Convert.ToInt32(Session["UserId"]);
            User user = UserDAL.GetById(userId);
            if (user == null)
            {
                UserCardHtml = "";
                return;
            }

            List<Post> myPosts = PostDAL.GetByUser(userId);
            string nickname = string.IsNullOrEmpty(user.Nickname) ? user.Username : user.Nickname;
            string bio = string.IsNullOrEmpty(user.Bio) ? "这个人很懒，什么都没写~" : user.Bio;
            string avatarChar = HttpUtility.HtmlEncode(nickname.Substring(0, 1));

            UserCardHtml = "<div class=\"card profile-card\">" +
                "<div class=\"avatar\">" + avatarChar + "</div>" +
                "<p class=\"profile-name\">" + HttpUtility.HtmlEncode(nickname) + "</p>" +
                "<p class=\"profile-username\">@" + HttpUtility.HtmlEncode(user.Username) + "</p>" +
                "<div class=\"profile-info\">" +
                "<div><span class=\"label\">邮箱</span>" + HttpUtility.HtmlEncode(user.Email) + "</div>" +
                "<div><span class=\"label\">简介</span>" + HttpUtility.HtmlEncode(bio) + "</div>" +
                "<div><span class=\"label\">注册时间</span>" + user.CreatedAt.ToString("yyyy-MM-dd") + "</div>" +
                "<div><span class=\"label\">文章数</span>" + myPosts.Count + " 篇</div>" +
                "</div>" +
                "</div>";
        }

        /// <summary>加载最新文章（最多 6 篇）</summary>
        private void LoadRecentPosts()
        {
            List<Post> posts = PostDAL.GetRecent(6);
            if (posts.Count == 0)
            {
                EmptyHtml = "<div class=\"card empty\">还没有文章，<a href=\"PostEdit.aspx\">写一篇吧</a>！</div>";
                PostsHtml = "";
            }
            else
            {
                PostsHtml = HtmlRenderer.PostListHtml(posts, false);
                EmptyHtml = "";
            }
        }
    }
}
