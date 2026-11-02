using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Web;
using WebApplication2.DAL;
using WebApplication2.Utils;

namespace WebApplication2.Services
{
    public static class AnalyticsService
    {
        public static DateTime Now{get{return DateTime.UtcNow.AddHours(8);}}
        public static string ReaderKey(HttpContext c)
        {
            int me=RequestSecurity.UserId(c);if(me>0)return "u:"+me;
            string id=c.Request.Cookies["ks-reader"]==null?null:c.Request.Cookies["ks-reader"].Value;Guid guid;
            if(!Guid.TryParseExact(id,"N",out guid))
            {
                id=Guid.NewGuid().ToString("N");c.Response.Cookies.Add(new HttpCookie("ks-reader",id){HttpOnly=true,Secure=c.Request.IsSecureConnection,SameSite=SameSiteMode.Lax,Expires=DateTime.UtcNow.AddYears(1)});
            }
            return "g:"+id;
        }
        public static object Heartbeat(HttpContext c)
        {
            int postId,seconds;
            if(!int.TryParse(c.Request.Form["postId"],out postId)||!int.TryParse(c.Request.Form["seconds"],out seconds)||seconds<0||seconds>20)throw new HttpException(400,"阅读记录参数无效");
            int me=RequestSecurity.UserId(c);string key=ReaderKey(c);DateTime now=Now;
            using(var db=new CommunityDAL(true))
            {
                var posts=db.Rows("SELECT UserId FROM Posts p WHERE p.PostId=@p0 AND "+PostDAL.PublicCondition,postId);
                if(posts.Count==0)throw new HttpException(404,"文章不存在或当前不可见");
                if(Convert.ToInt32(posts[0]["UserId"])==me)return new{ok=true,excluded=true};
                var readings=db.Rows("SELECT ActiveSeconds,Completed,LastHeartbeatAt FROM ReadingDaily WITH(UPDLOCK) WHERE PostId=@p0 AND ReaderKey=@p1 AND ReadingDate=@p2",postId,key,now.Date);
                if(readings.Count==0)throw new HttpException(409,"请先打开文章再记录阅读");
                var row=readings[0];double elapsed=Math.Max(0,(now-DateTime.Parse((string)row["LastHeartbeatAt"],CultureInfo.InvariantCulture)).TotalSeconds);
                int next=Math.Min(86400,Convert.ToInt32(row["ActiveSeconds"])+Math.Min(seconds,(int)Math.Ceiling(elapsed)));
                bool complete=(bool)row["Completed"]||(c.Request.Form["complete"]=="1"&&next>=10);
                db.Execute("UPDATE ReadingDaily SET ActiveSeconds=@p0,Completed=@p1,LastHeartbeatAt=@p2 WHERE PostId=@p3 AND ReaderKey=@p4 AND ReadingDate=@p5",next,complete,now,postId,key,now.Date);db.Commit();
                return new{ok=true,seconds=next,completed=complete};
            }
        }
        private static int N(Dictionary<string,object> r,string k){return r.ContainsKey(k)&&r[k]!=null?Convert.ToInt32(r[k]):0;}
        private static DateTime D(Dictionary<string,object> r,string k){return DateTime.Parse((string)r[k],CultureInfo.InvariantCulture).Date;}
        private static double Rate(double a,double b){return b==0?0:Math.Round(a/b*100,1);}
        private static Dictionary<string,object> Stats(List<Dictionary<string,object>> views,List<Dictionary<string,object>> reads,List<Dictionary<string,object>> events,HashSet<int> ids,DateTime from,DateTime to)
        {
            var vv=views.Where(r=>ids.Contains(N(r,"PostId"))&&D(r,"ViewDate")>=from&&D(r,"ViewDate")<=to).ToList();
            var rr=reads.Where(r=>ids.Contains(N(r,"PostId"))&&D(r,"ReadingDate")>=from&&D(r,"ReadingDate")<=to).ToList();
            var ee=events.Where(r=>D(r,"OccurredAt")>=from&&D(r,"OccurredAt")<=to&&(r["Kind"].ToString()=="follow"||ids.Contains(N(r,"PostId")))).ToList();
            var readers=new HashSet<string>(vv.Select(r=>(string)r["ViewerKey"]));
            var readPairs=new HashSet<string>(vv.Select(r=>N(r,"PostId")+":"+r["ViewerKey"]));
            int interacted=ee.Where(r=>r["Kind"].ToString()!="follow"&&N(r,"Delta")==1&&readPairs.Contains(N(r,"PostId")+":u:"+N(r,"ActorId"))).Select(r=>N(r,"ActorId")).Distinct().Count();
            var s=new Dictionary<string,object>{{"views",vv.Count},{"readers",readers.Count},{"trackedReads",rr.Count},{"seconds",rr.Sum(r=>(long)N(r,"ActiveSeconds"))},{"avgSeconds",rr.Count==0?0:Math.Round(rr.Average(r=>N(r,"ActiveSeconds")),1)},{"completionRate",Rate(rr.Count(r=>(bool)r["Completed"]),rr.Count)},{"interactionRate",Rate(interacted,readers.Count)}};
            foreach(string kind in new[]{"like","favorite","comment","repost","follow"})
            {
                var changes=ee.Where(r=>r["Kind"].ToString()==kind).ToList();s[kind+"Added"]=changes.Count(r=>N(r,"Delta")==1);s[kind+"Removed"]=changes.Count(r=>N(r,"Delta")==-1);s[kind+"Net"]=changes.Sum(r=>N(r,"Delta"));
            }
            s["heatNet"]=Convert.ToInt32(s["likeNet"])*3+Convert.ToInt32(s["favoriteNet"])*4+Convert.ToInt32(s["commentNet"])*2;return s;
        }
        public static object Read(int me,HttpRequest request)
        {
            DateTime today=Now.Date,from,to=today;string range=request.QueryString["range"]??"30";
            using(var db=new CommunityDAL())
            {
                var posts=db.Rows("SELECT p.PostId,p.Title,p.CreatedAt,p.Status,p.IsHidden,p.DeletedAt,p.ViewCount,"+TaxonomyDAL.CategoryNamesSql+" AS Category,(SELECT COUNT(*) FROM Likes WHERE PostId=p.PostId) AS Likes,(SELECT COUNT(*) FROM Favorites WHERE PostId=p.PostId) AS Favorites,(SELECT COUNT(*) FROM Comments WHERE PostId=p.PostId AND IsDeleted=0 AND IsHidden=0) AS Comments,(SELECT COUNT(*) FROM Reposts WHERE PostId=p.PostId AND IsHidden=0) AS Reposts FROM Posts p WHERE p.UserId=@p0 ORDER BY p.CreatedAt DESC",me);
                var ids=new HashSet<int>(posts.Select(r=>N(r,"PostId")));string selected=request.QueryString["posts"]??"";
                if(selected.Length>100)throw new HttpException(400,"文章选择无效");
                if(selected!="")
                {
                    var parsed=new HashSet<int>();foreach(string value in selected.Split(',')){int id;if(!int.TryParse(value,out id)||!ids.Contains(id))throw new HttpException(403,"只能分析自己的文章");parsed.Add(id);}
                    if(parsed.Count>3)throw new HttpException(400,"最多对比3篇文章");ids=parsed;
                }
                DateTime started=DateTime.Parse((string)db.Rows("SELECT StartedAt FROM AnalyticsState WHERE Id=1")[0]["StartedAt"],CultureInfo.InvariantCulture);
                if(range=="all")from=posts.Count==0?today:posts.Min(r=>D(r,"CreatedAt"));
                else if(range=="custom")
                {
                    if(!DateTime.TryParseExact(request.QueryString["from"],"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out from)||!DateTime.TryParseExact(request.QueryString["to"],"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out to))throw new HttpException(400,"请输入有效日期");
                }
                else{int days;if(!int.TryParse(range,out days)||!new[]{7,30,90}.Contains(days))throw new HttpException(400,"日期范围无效");from=today.AddDays(1-days);}
                if(from>to||to>today||from.Year<2000||(to-from).TotalDays>3660)throw new HttpException(400,"日期应在2000年以后且不晚于今天，范围最多10年");
                int length=(to-from).Days+1;DateTime priorEnd=from.AddDays(-1),priorStart=from.AddDays(-length);
                var views=db.Rows("SELECT v.PostId,v.ViewerKey,v.ViewDate FROM PostViews v JOIN Posts p ON p.PostId=v.PostId WHERE p.UserId=@p0 AND v.ViewDate>=@p1 AND v.ViewDate<=@p2",me,range=="all"?from:priorStart,to);
                var reads=db.Rows("SELECT r.PostId,r.ReaderKey,r.ReadingDate,r.ActiveSeconds,r.Completed FROM ReadingDaily r JOIN Posts p ON p.PostId=r.PostId WHERE p.UserId=@p0 AND r.ReadingDate>=@p1 AND r.ReadingDate<=@p2",me,range=="all"?from:priorStart,to);
                var events=db.Rows("SELECT PostId,ActorId,Kind,Delta,OccurredAt FROM CreatorEvents WHERE OwnerId=@p0",me);
                var heat=db.Rows("SELECT h.PostId,h.SnapshotDate,h.Heat FROM PostHeatDaily h JOIN Posts p ON p.PostId=h.PostId WHERE p.UserId=@p0 AND h.SnapshotDate<=@p2",me,from,to);
                int followers=Convert.ToInt32(db.Scalar("SELECT COUNT(*) FROM Follows WHERE FolloweeId=@p0",me));
                int initialFollowers=followers-events.Where(r=>r["Kind"].ToString()=="follow").Sum(r=>N(r,"Delta"));
                var series=new List<Dictionary<string,object>>();bool monthly=length>100;
                for(DateTime day=from;day<=to;)
                {
                    DateTime end=monthly?new DateTime(day.Year,day.Month,1).AddMonths(1).AddDays(-1):day;if(end>to)end=to;
                    var point=Stats(views,reads,events,ids,day,end);point["date"]=day.ToString(monthly?"yyyy-MM":"yyyy-MM-dd");
                    bool covered=end>=started.Date;
                    point["followers"]=covered?(object)Math.Max(0,initialFollowers+events.Where(r=>r["Kind"].ToString()=="follow"&&D(r,"OccurredAt")<=end).Sum(r=>N(r,"Delta"))):null;
                    var snaps=heat.Where(r=>ids.Contains(N(r,"PostId"))&&D(r,"SnapshotDate")<=end).ToList();
                    point["heat"]=covered?(object)snaps.GroupBy(r=>N(r,"PostId")).Sum(g=>N(g.OrderByDescending(r=>D(r,"SnapshotDate")).First(),"Heat")):null;
                    if(end==today)point["heat"]=posts.Where(p=>ids.Contains(N(p,"PostId"))).Sum(p=>N(p,"Likes")*3+N(p,"Favorites")*4+N(p,"Comments")*2);
                    if(!covered)foreach(string key in new[]{"likeAdded","favoriteAdded","commentAdded","repostAdded","followAdded","followRemoved","followNet","avgSeconds","completionRate","interactionRate","heatNet"})point[key]=null;
                    series.Add(point);day=end.AddDays(1);
                }
                foreach(var post in posts)
                {
                    int id=N(post,"PostId");post["heat"]=N(post,"Likes")*3+N(post,"Favorites")*4+N(post,"Comments")*2;
                    post["state"]=post["DeletedAt"]!=null?"回收站":(bool)post["IsHidden"]?"已隐藏":post["Status"].ToString()=="Draft"?"草稿":"已发布";
                    post["period"]=Stats(views,reads,events,new HashSet<int>{id},from,to);
                }
                var totals=new Dictionary<string,object>{{"posts",posts.Count(p=>p["state"].ToString()=="已发布")},{"followers",followers},{"views",posts.Where(p=>ids.Contains(N(p,"PostId"))).Sum(p=>(long)N(p,"ViewCount"))},{"likes",posts.Where(p=>ids.Contains(N(p,"PostId"))).Sum(p=>N(p,"Likes"))},{"favorites",posts.Where(p=>ids.Contains(N(p,"PostId"))).Sum(p=>N(p,"Favorites"))},{"comments",posts.Where(p=>ids.Contains(N(p,"PostId"))).Sum(p=>N(p,"Comments"))},{"reposts",posts.Where(p=>ids.Contains(N(p,"PostId"))).Sum(p=>N(p,"Reposts"))},{"heat",posts.Where(p=>ids.Contains(N(p,"PostId"))).Sum(p=>N(p,"heat"))}};
                var compare=new List<object>();foreach(int id in ids)if(selected!="")compare.Add(new{postId=id,title=posts.First(p=>N(p,"PostId")==id)["Title"],series=series.Select(point=>new{date=point["date"],views=views.Count(v=>N(v,"PostId")==id&&(monthly?D(v,"ViewDate").ToString("yyyy-MM"):D(v,"ViewDate").ToString("yyyy-MM-dd"))==point["date"].ToString()&&D(v,"ViewDate")>=from&&D(v,"ViewDate")<=to)}).ToList()});
                return new{from=from.ToString("yyyy-MM-dd"),to=to.ToString("yyyy-MM-dd"),startedAt=started.ToString("yyyy-MM-dd HH:mm:ss"),updatedAt=Now.ToString("yyyy-MM-dd HH:mm:ss"),range=range,partial=from<started.Date,monthly=monthly,totals=totals,period=Stats(views,reads,events,ids,from,to),previous=range=="all"?null:Stats(views,reads,events,ids,priorStart,priorEnd),series=series,posts=posts,comparison=compare,ownerId=me};
            }
        }
    }
}
