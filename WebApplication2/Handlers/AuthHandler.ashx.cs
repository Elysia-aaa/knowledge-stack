using System;
using System.Web;
using System.Web.Script.Serialization;
using WebApplication2.DAL;
using WebApplication2.Utils;

namespace WebApplication2.Handlers
{
    /// <summary>
    /// 认证相关的 AJAX 处理器：用于注册时的用户名 / 邮箱异步查重。
    /// </summary>
    public class AuthHandler : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "application/json";
            var serializer = new JavaScriptSerializer();
            string action = context.Request["action"] ?? "";

            try
            {
                if (action == "checkUsername")
                {
                    string username = (context.Request["value"] ?? "").Trim();
                    bool exists = UserDAL.UsernameExists(username);
                    context.Response.Write(serializer.Serialize(new { available = !exists }));
                }
                else if (action == "checkEmail")
                {
                    string email = (context.Request["value"] ?? "").Trim();
                    bool exists = UserDAL.EmailExists(email);
                    context.Response.Write(serializer.Serialize(new { available = !exists }));
                }
                else
                {
                    context.Response.Write(serializer.Serialize(new { error = "未知操作" }));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError(ex.ToString());
                RequestSecurity.Json(context,new {error="暂时无法检查，请稍后重试"},500);
            }
        }

        public bool IsReusable
        {
            get { return false; }
        }
    }
}
