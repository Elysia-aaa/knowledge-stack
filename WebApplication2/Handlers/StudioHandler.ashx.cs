using System;
using System.Web;
using System.Web.SessionState;
using WebApplication2.Services;
using WebApplication2.Utils;
namespace WebApplication2.Handlers
{
    public class StudioHandler:IHttpHandler,IRequiresSessionState
    {
        public void ProcessRequest(HttpContext c)
        {
            try
            {
                int me=RequestSecurity.UserId(c);string action=c.Request["action"]??"";object result;
                if(c.Request.HttpMethod=="GET")
                {
                    if(action=="appearance")result=AppearanceService.Read(me);
                    else if(action=="analytics")
                    {
                        if(me==0)throw new HttpException(401,"请先登录创作数据后台");
                        foreach(string key in new[]{"id","userId","ownerId"})if(c.Request.QueryString[key]!=null&&c.Request.QueryString[key]!=me.ToString())throw new HttpException(403,"只能查看自己的创作数据");
                        result=AnalyticsService.Read(me,c.Request);
                    }
                    else throw new HttpException(404,"接口不存在");
                }
                else if(c.Request.HttpMethod=="POST")
                {
                    if(action=="heartbeat"){RequestSecurity.ValidateToken(c);result=AnalyticsService.Heartbeat(c);}
                    else if(action=="appearance"){RequestSecurity.RequireWrite(c);result=AppearanceService.Save(me,c.Request);}
                    else throw new HttpException(404,"接口不存在");
                }
                else throw new HttpException(405,"请求方式不支持");
                RequestSecurity.Json(c,result);
            }
            catch(HttpException ex){RequestSecurity.Json(c,new{error=ex.Message},ex.GetHttpCode());}
            catch(Exception ex){System.Diagnostics.Trace.TraceError(ex.ToString());RequestSecurity.Json(c,new{error="数据暂未加载，请重试"},500);}
        }
        public bool IsReusable{get{return false;}}
    }
}
