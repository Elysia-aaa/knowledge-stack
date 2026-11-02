using System;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI;
using WebApplication2.DAL;
using WebApplication2.Models;
using WebApplication2.Utils;

namespace WebApplication2
{
    public partial class Register : System.Web.UI.Page
    {
        protected string ErrorHtml = "";
        protected string UsernameValue = "", EmailValue = "", NicknameValue = "", BioValue = "";

        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack)
            {
                // 回填表单（校验失败时不丢失用户输入）
                UsernameValue = Request.Form["username"] ?? "";
                EmailValue = Request.Form["email"] ?? "";
                NicknameValue = Request.Form["nickname"] ?? "";
                BioValue = Request.Form["bio"] ?? "";
                HandleRegister();
            }
        }

        /// <summary>处理注册：服务端校验 + 密码加密 + 写入数据库</summary>
        private void HandleRegister()
        {
            string username = (Request.Form["username"] ?? "").Trim();
            string email = (Request.Form["email"] ?? "").Trim();
            string password = Request.Form["password"] ?? "";
            string confirm = Request.Form["confirm"] ?? "";
            string nickname = (Request.Form["nickname"] ?? "").Trim();
            string bio = (Request.Form["bio"] ?? "").Trim();

            if (username.Length < 3 || username.Length > 20) { ErrorHtml = Alert("用户名长度需在 3-20 个字符之间"); return; }
            if (!Regex.IsMatch(username, @"^[a-zA-Z0-9_一-龥]+$")) { ErrorHtml = Alert("用户名只能包含字母、数字、下划线或中文"); return; }
            if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$")) { ErrorHtml = Alert("邮箱格式不正确"); return; }
            if (password.Length < 6) { ErrorHtml = Alert("密码长度至少 6 位"); return; }
            if (password != confirm) { ErrorHtml = Alert("两次输入的密码不一致"); return; }
            if (UserDAL.UsernameExists(username)) { ErrorHtml = Alert("该用户名已被注册"); return; }
            if (UserDAL.EmailExists(email)) { ErrorHtml = Alert("该邮箱已被注册"); return; }

            // 生成随机盐 + 哈希密码，不存明文
            string salt = SecurityHelper.GenerateSalt();
            string hash = SecurityHelper.HashPassword(password, salt);

            User user = new User
            {
                Username = username,
                Email = email,
                PasswordHash = hash,
                Salt = salt,
                Nickname = string.IsNullOrEmpty(nickname) ? username : nickname,
                Bio = bio
            };
            int userId = UserDAL.Create(user);

            // 注册成功后自动登录
            Session["UserId"] = userId;
            Session["Username"] = user.Username;
            Session["Nickname"] = user.Nickname;

            Response.Redirect("Default.aspx");
        }

        private string Alert(string msg)
        {
            return "<div class=\"alert-error\">" + HttpUtility.HtmlEncode(msg) + "</div>";
        }
    }
}
