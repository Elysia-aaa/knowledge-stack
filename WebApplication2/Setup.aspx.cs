using System;
using System.Configuration;
using System.Web.UI;
using WebApplication2.Utils;
using WebApplication2.DAL;

namespace WebApplication2
{
    public partial class Setup : Page
    {
        protected string ResultHtml = "";
        protected string CsrfToken {get{return RequestSecurity.Token(Session);}}

        protected void Page_Load(object sender, EventArgs e)
        {
            if(!Request.IsLocal||ConfigurationManager.AppSettings["EnableLocalSetup"]!="true")
            {Response.StatusCode=404;Response.TrySkipIisCustomErrors=true;Response.Write("初始化入口未开放");Response.End();return;}
            if(Request.HttpMethod=="POST")RequestSecurity.ValidateToken(Context);
            // 直接用 Request.Form 判断（本页不使用服务端表单，避免 IsPostBack 依赖）
            if (Request.Form["action"] == "init")
            {
                ResultHtml = InitializeDatabase();
            }
            else if (Request.Form["action"] == "reset")
            {
                Response.StatusCode=405;
                ResultHtml = ErrBox("已停用清空重建；请使用增量脚本，避免丢失数据。");
            }
        }

        /// <summary>本机增量初始化；保留已有数据。</summary>
        private string InitializeDatabase()
        {
            try
            {
                DatabaseInitializer.EnsureInitialized();
                return OkBox("数据库初始化成功，基础表和社区扩展表已就绪，现有数据保留。");
            }
            catch (Exception ex)
            {
                return ErrBox("初始化失败：" + ex.Message);
            }
        }

        private string OkBox(string msg)
        {
            return "<div class=\"alert-error\" style=\"background:#eafaf1;color:#1f7a4d;border-color:#b7e4c7;margin-top:18px;\">✅ " + System.Web.HttpUtility.HtmlEncode(msg) + "</div>";
        }

        private string ErrBox(string msg)
        {
            return "<div class=\"alert-error\" style=\"margin-top:18px;\">❌ " + System.Web.HttpUtility.HtmlEncode(msg) + "</div>";
        }
    }
}
