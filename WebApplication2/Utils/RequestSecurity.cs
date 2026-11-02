using System;
using System.Security.Cryptography;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.SessionState;

namespace WebApplication2.Utils
{
    public static class RequestSecurity
    {
        public static string Token(HttpSessionState session)
        {
            if (session["CsrfToken"] == null)
            {
                byte[] bytes = new byte[32];
                using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(bytes);
                session["CsrfToken"] = Convert.ToBase64String(bytes);
            }
            return (string)session["CsrfToken"];
        }
        public static int UserId(HttpContext context) { return context.Session["UserId"] == null ? 0 : Convert.ToInt32(context.Session["UserId"]); }
        public static void RequireWrite(HttpContext context)
        {
            if (context.Request.HttpMethod != "POST") throw new HttpException(405, "请使用 POST 请求");
            if (UserId(context) == 0) throw new HttpException(401, "请先登录");
            ValidateToken(context);
        }
        public static void ValidateToken(HttpContext context)
        {
            string supplied = context.Request.Headers["X-CSRF-Token"] ?? context.Request.Form["csrf"];
            if (String.IsNullOrEmpty(supplied) || supplied != Token(context.Session)) throw new HttpException(403, "页面已过期，请刷新后再试");
        }
        public static void Json(HttpContext context, object value, int status = 200)
        {
            context.Response.StatusCode = status;
            context.Response.TrySkipIisCustomErrors = true;
            context.Response.ContentType = "application/json; charset=utf-8";
            context.Response.Cache.SetCacheability(HttpCacheability.NoCache);
            context.Response.Cache.SetNoStore();
            context.Response.Write(new JavaScriptSerializer().Serialize(value));
        }
    }
}
