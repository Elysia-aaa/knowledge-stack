using System;
using System.Web;
using System.Web.UI;
using WebApplication2.DAL;
using WebApplication2.Services;
using WebApplication2.Utils;

namespace WebApplication2
{
    public partial class Site : System.Web.UI.MasterPage
    {
        protected string AppearanceJson { get { return new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(AppearanceService.Read(RequestSecurity.UserId(Context))).Replace("<", "\\u003c"); } }
        protected string CsrfToken { get { return RequestSecurity.Token(Session); } }
        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);
            Page.ViewStateUserKey=Session.SessionID;
            if(Request.HttpMethod=="POST")RequestSecurity.ValidateToken(Context);
            // 若数据库尚未初始化（首次运行），自动跳转到 Setup 页，避免报错
            try
            {
                DbHelper.ExecuteScalar("SELECT COUNT(*) FROM Users");
            }
            catch
            {
                Response.Redirect("Setup.aspx");
            }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
        }

        /// <summary>
        /// 根据 Session 登录状态生成导航栏右侧内容：
        /// 已登录显示「写文章 / 我的文章 / 昵称 / 登出」，未登录显示「登录 / 注册」。
        /// </summary>
        protected string GetNavHtml()
        {
            if (Session["UserId"] != null)
            {
                string nickname = Session["Nickname"] != null
                    ? Session["Nickname"].ToString()
                    : Session["Username"].ToString();
                return "<a class=\"nav-compose\" href=\"PostEdit.aspx\">写文章 <span aria-hidden=\"true\">＋</span></a>" +
                       "<a class=\"nav-myposts\" href=\"MyPosts.aspx\">我的文章</a>" +
                       "<a class=\"nav-notifications\" href=\"Community.aspx?view=notifications\">通知 <span id=\"notificationBadge\"></span></a>" +
                       "<a class=\"nav-user\" href=\"Community.aspx?view=account\">" + HttpUtility.HtmlEncode(nickname) + "</a>" +
                       "<a class=\"nav-logout\" href=\"Logout.aspx\">登出</a>";
            }
            return "<a href=\"Login.aspx\">登录</a><a class=\"nav-compose\" href=\"Register.aspx\">加入小栈</a>";
        }
    }
}
