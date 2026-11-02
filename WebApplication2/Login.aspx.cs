using System;
using System.Web;
using System.Web.UI;
using WebApplication2.DAL;
using WebApplication2.Models;
using WebApplication2.Utils;

namespace WebApplication2
{
    public partial class Login : System.Web.UI.Page
    {
        protected string ErrorHtml = "";
        protected string AccountValue = "";

        protected void Page_Load(object sender, EventArgs e)
        {
            // 已登录则直接进入首页
            if (Session["UserId"] != null && !IsPostBack)
            {
                Response.Redirect("Default.aspx");
                return;
            }

            if (!IsPostBack)
            {
                // 从 Cookie 读取「记住我」的用户名
                if (Request.Cookies["remember_username"] != null)
                {
                    AccountValue = Request.Cookies["remember_username"].Value;
                }
            }
            else
            {
                AccountValue = Request.Form["account"] ?? "";
                HandleLogin();
            }
        }

        /// <summary>处理登录：校验账号密码，写入 Session 保持登录态</summary>
        private void HandleLogin()
        {
            string account = (Request.Form["account"] ?? "").Trim();
            string password = Request.Form["password"] ?? "";
            bool remember = Request.Form["remember"] == "on";

            if (account.Length == 0 || password.Length == 0)
            {
                ErrorHtml = Alert("请输入账号和密码");
                return;
            }

            // 支持用「用户名」或「邮箱」登录
            User user = account.Contains("@")
                ? UserDAL.GetByEmail(account)
                : UserDAL.GetByUsername(account);

            if (user == null || !SecurityHelper.VerifyPassword(password, user.Salt, user.PasswordHash))
            {
                ErrorHtml = Alert("账号或密码错误");
                return;
            }

            // 用 Session 保持登录状态
            Session["UserId"] = user.UserId;
            Session["Username"] = user.Username;
            Session["Nickname"] = string.IsNullOrEmpty(user.Nickname) ? user.Username : user.Nickname;

            // 「记住我」：把用户名写入 Cookie（7 天），下次自动填充
            HttpCookie cookie = new HttpCookie("remember_username");
            if (remember)
            {
                cookie.Value = user.Username;
                cookie.Expires = DateTime.Now.AddDays(7);
            }
            else
            {
                cookie.Expires = DateTime.Now.AddYears(-1);
            }
            Response.Cookies.Add(cookie);

            Response.Redirect("Default.aspx");
        }

        private string Alert(string msg)
        {
            return "<div class=\"alert-error\">" + HttpUtility.HtmlEncode(msg) + "</div>";
        }
    }
}
