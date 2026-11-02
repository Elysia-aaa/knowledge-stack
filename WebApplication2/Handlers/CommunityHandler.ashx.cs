using System;
using System.Web;
using System.Web.SessionState;
using WebApplication2.DAL;
using WebApplication2.Services;
using WebApplication2.Utils;

namespace WebApplication2.Handlers
{
    public class CommunityHandler : IHttpHandler, IRequiresSessionState
    {
        public void ProcessRequest(HttpContext context)
        {
            try
            {
                int me=RequestSecurity.UserId(context);
                if(context.Request.HttpMethod=="GET")
                {
                    string action=context.Request.QueryString["action"]??"session";
                    RequestSecurity.Json(context,action=="session"?(object)new {userId=me,csrf=RequestSecurity.Token(context.Session),isAdmin=CommunityService.IsAdmin(me)}:CommunityService.Read(action,me,context.Request));
                }
                else
                {
                    RequestSecurity.RequireWrite(context);
                    string action=context.Request.Form["action"]??"";
                    object result=CommunityService.Write(action,me,context.Request);
                    if(action=="profile")context.Session["Nickname"]=UserDAL.GetById(me).Nickname;
                    if(action=="password") { context.Session.Clear();context.Session.Abandon(); }
                    RequestSecurity.Json(context,result);
                }
            }
            catch(HttpException ex){RequestSecurity.Json(context,new {error=ex.Message},ex.GetHttpCode());}
            catch(Exception ex){System.Diagnostics.Trace.TraceError(ex.ToString());RequestSecurity.Json(context,new {error="操作暂未完成，请刷新后重试"},500);}
        }
        public bool IsReusable { get { return false; } }
    }
}
