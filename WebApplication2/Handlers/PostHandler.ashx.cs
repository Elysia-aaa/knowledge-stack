using System;
using System.Web;
using System.Web.SessionState;
using WebApplication2.DAL;
using WebApplication2.Services;
using WebApplication2.Utils;
namespace WebApplication2.Handlers
{
    public class PostHandler:IHttpHandler,IRequiresSessionState
    {
        public void ProcessRequest(HttpContext context)
        {
            try
            {
                RequestSecurity.RequireWrite(context);
                string action=context.Request.Form["action"]??"";
                if(action!="like"&&action!="unlike"&&action!="delete")throw new HttpException(400,"未知操作");
                int me=RequestSecurity.UserId(context),postId=CommunityService.Number(context.Request.Form["postId"]);
                CommunityService.Write(action,me,context.Request);
                RequestSecurity.Json(context,new {ok=true,liked=action=="like",likeCount=PostDAL.GetLikeCount(postId)});
            }
            catch(HttpException ex){RequestSecurity.Json(context,new {error=ex.Message},ex.GetHttpCode());}
            catch(Exception ex){System.Diagnostics.Trace.TraceError(ex.ToString());RequestSecurity.Json(context,new {error="操作失败，请稍后重试"},500);}
        }
        public bool IsReusable{get{return false;}}
    }
}
