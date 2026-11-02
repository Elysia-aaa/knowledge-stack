using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using WebApplication2.DAL;
using WebApplication2.Utils;

namespace WebApplication2.Services
{
    public static class CommunityService
    {
        private const string Public = PostDAL.PublicCondition;
        public static int Number(string text) { int n;return Int32.TryParse(text,out n)?Math.Max(0,n):0; }
        public static bool IsAdmin(int me) { if(me==0)return false;using(var db=new CommunityDAL())return Convert.ToInt32(db.Scalar("SELECT COUNT(*) FROM Users WHERE UserId=@p0 AND IsAdmin=1",me))==1; }
        private static string Text(HttpRequest request,string key,int max,bool required=false,bool trim=true)
        {
            string value=request.Unvalidated.Form[key]??"";if(trim)value=value.Trim();
            if(value.Length>max||(required&&value.Length==0))throw new HttpException(400,key+" 长度不符合要求（最多 "+max+" 字）");
            return value;
        }
        private static void SignedIn(int me) { if(me==0)throw new HttpException(401,"请先登录"); }
        private static int Author(CommunityDAL db,int postId,bool requirePublic=true)
        {
            object result=db.Scalar("SELECT p.UserId FROM Posts p WHERE p.PostId=@p0"+(requirePublic?" AND "+Public:""),postId);
            if(result==null||result==DBNull.Value)throw new HttpException(404,"文章不存在或当前不可见");return Convert.ToInt32(result);
        }
        private static object Page(CommunityDAL db,string columns,string from,string order,int page,params object[] values)
        {
            int total=Convert.ToInt32(db.Scalar("SELECT COUNT(*) "+from,values));
            int offset=(Math.Max(1,Math.Min(page,100000))-1)*10;
            // order/columns/from are internal constants, never client SQL.
            return new {items=db.Rows("SELECT "+columns+" "+from+" ORDER BY "+order+" OFFSET "+offset+" ROWS FETCH NEXT 10 ROWS ONLY",values),total=total,page=Math.Max(1,page),pageSize=10};
        }
        private static void Notify(CommunityDAL db,int recipient,int actor,string kind,int? postId,string key)
        {
            if(recipient==actor)return;
            db.Execute("IF NOT EXISTS(SELECT 1 FROM Notifications WHERE UserId=@p0 AND EventKey=@p5) INSERT Notifications(UserId,ActorId,Kind,PostId,EventKey) VALUES(@p0,@p1,@p2,@p3,@p5)",recipient,actor,kind,postId,null,key);
        }
        public static object Read(string action,int me,HttpRequest request)
        {
            int id=Number(request.QueryString["id"]),page=Math.Max(1,Number(request.QueryString["page"]));
            using(var db=new CommunityDAL())
            {
                if(action=="profile"||action=="account")
                {
                    if(action=="account")SignedIn(me);
                    int userId=id==0?me:id;if(userId==0)throw new HttpException(400,"请选择用户");
                    var users=db.Rows("SELECT UserId,Username,Nickname,Bio,Avatar,CreatedAt FROM Users WHERE UserId=@p0",userId);
                    if(users.Count==0)throw new HttpException(404,"用户不存在");var user=users[0];
                    if(userId==me)user["Email"]=db.Scalar("SELECT Email FROM Users WHERE UserId=@p0",me);
                    user["IsSelf"]=userId==me;
                    user["Following"]=Convert.ToInt32(db.Scalar("SELECT COUNT(*) FROM Follows WHERE FollowerId=@p0 AND FolloweeId=@p1",me,userId))>0;
                    user["Mutual"]=Convert.ToInt32(db.Scalar("SELECT COUNT(*) FROM Follows WHERE FollowerId=@p0 AND FolloweeId=@p1",userId,me))>0;
                    user["Followers"]=db.Scalar("SELECT COUNT(*) FROM Follows WHERE FolloweeId=@p0",userId);
                    user["FollowingCount"]=db.Scalar("SELECT COUNT(*) FROM Follows WHERE FollowerId=@p0",userId);
                    user["PostCount"]=db.Scalar("SELECT COUNT(*) FROM Posts p WHERE p.UserId=@p0 AND "+Public,userId);
                    user["LikesReceived"]=db.Scalar("SELECT ISNULL(SUM(p.LikeCount),0) FROM Posts p WHERE p.UserId=@p0 AND "+Public,userId);
                    user["FavoritesReceived"]=db.Scalar("SELECT COUNT(*) FROM Favorites f JOIN Posts p ON p.PostId=f.PostId WHERE p.UserId=@p0 AND "+Public,userId);
                    user["CommentsReceived"]=db.Scalar("SELECT COUNT(*) FROM Comments c JOIN Posts p ON p.PostId=c.PostId WHERE p.UserId=@p0 AND "+Public+" AND c.IsDeleted=0 AND c.IsHidden=0",userId);
                    if(userId==me){user["DraftCount"]=db.Scalar("SELECT COUNT(*) FROM Posts WHERE UserId=@p0 AND Status=N'Draft' AND DeletedAt IS NULL",me);user["TrashCount"]=db.Scalar("SELECT COUNT(*) FROM Posts WHERE UserId=@p0 AND DeletedAt IS NOT NULL",me);}
                    return user;
                }
                if(action=="posts")
                {
                    string view=request.QueryString["view"]??"profile";
                    string from="FROM Posts p JOIN Users u ON u.UserId=p.UserId WHERE ";
                    if(view=="drafts"||view=="trash")
                    {
                        SignedIn(me);from+="p.UserId=@p0 AND "+(view=="drafts"?"p.Status=N'Draft' AND p.DeletedAt IS NULL":"p.DeletedAt IS NOT NULL");id=me;
                    }
                    else if(view=="favorites") {SignedIn(me);from+=Public+" AND EXISTS(SELECT 1 FROM Favorites f WHERE f.UserId=@p0 AND f.PostId=p.PostId)";id=me;}
                    else {if(id==0)id=me;from+=Public+" AND p.UserId=@p0";}
                    return Page(db,"p.PostId,p.UserId,p.Title,p.Summary,"+TaxonomyDAL.CategoryNamesSql+" AS Category,p.Tags,p.CreatedAt,p.Status,p.IsHidden,p.LikeCount,p.ViewCount,u.Nickname,u.Username",from,"p.CreatedAt DESC,p.PostId DESC",page,id);
                }
                if(action=="feed"||action=="reposts")
                {
                    if(action=="feed")SignedIn(me);if(id==0)id=me;
                    string original="SELECT p.PostId,p.Title,p.Summary,p.UserId,u.Nickname,u.Username,0 AS RepostId,N'' AS Note,p.CreatedAt AS EventTime,p.UserId AS ActorId,u.Nickname AS ActorName FROM Posts p JOIN Users u ON u.UserId=p.UserId WHERE "+Public+" AND EXISTS(SELECT 1 FROM Follows f WHERE f.FollowerId=@p0 AND f.FolloweeId=p.UserId)";
                    string repost="SELECT CASE WHEN "+Public+" THEN p.PostId END AS PostId,CASE WHEN "+Public+" THEN p.Title END AS Title,CASE WHEN "+Public+" THEN p.Summary END AS Summary,p.UserId,u.Nickname,u.Username,r.RepostId,r.Note,r.CreatedAt AS EventTime,r.UserId AS ActorId,a.Nickname AS ActorName FROM Reposts r JOIN Users a ON a.UserId=r.UserId LEFT JOIN Posts p ON p.PostId=r.PostId LEFT JOIN Users u ON u.UserId=p.UserId WHERE r.IsHidden=0 AND "+(action=="feed"?"EXISTS(SELECT 1 FROM Follows f WHERE f.FollowerId=@p0 AND f.FolloweeId=r.UserId)":"r.UserId=@p0");
                    return Page(db,"*","FROM ("+(action=="feed"?original+" UNION ALL ":"")+repost+") timeline","EventTime DESC,RepostId DESC,PostId DESC",page,action=="feed"?me:id);
                }
                if(action=="comments")
                {
                    Author(db,id);
                    return Page(db,"c.CommentId,c.UserId,c.ParentId,CASE WHEN c.IsDeleted=1 THEN N'该评论已删除' WHEN c.IsHidden=1 THEN N'该评论已被隐藏' ELSE c.Content END AS Content,c.IsDeleted,c.IsHidden,c.CreatedAt,u.Nickname,u.Avatar,pu.Nickname AS ReplyTo,CAST(CASE WHEN c.UserId=@p1 THEN 1 ELSE 0 END AS bit) AS CanDelete,CAST(CASE WHEN c.UserId=p.UserId THEN 1 ELSE 0 END AS bit) AS IsAuthor,CASE WHEN c.IsDeleted=0 AND c.IsHidden=0 THEN (SELECT COUNT(*) FROM CommentLikes cl WHERE cl.CommentId=c.CommentId) ELSE 0 END AS LikeCount,CAST(CASE WHEN c.IsDeleted=0 AND c.IsHidden=0 AND EXISTS(SELECT 1 FROM CommentLikes cl WHERE cl.CommentId=c.CommentId AND cl.UserId=@p1) THEN 1 ELSE 0 END AS bit) AS Liked",
                        "FROM Comments c JOIN Users u ON u.UserId=c.UserId JOIN Posts p ON p.PostId=c.PostId LEFT JOIN Comments parent ON parent.CommentId=c.ParentId LEFT JOIN Users pu ON pu.UserId=parent.UserId WHERE c.PostId=@p0","c.CreatedAt,c.CommentId",page,id,me);
                }
                if(action=="state")
                {
                    Author(db,id);return new {liked=Convert.ToInt32(db.Scalar("SELECT COUNT(*) FROM Likes WHERE UserId=@p0 AND PostId=@p1",me,id))>0,favorited=Convert.ToInt32(db.Scalar("SELECT COUNT(*) FROM Favorites WHERE UserId=@p0 AND PostId=@p1",me,id))>0,reposted=Convert.ToInt32(db.Scalar("SELECT COUNT(*) FROM Reposts WHERE UserId=@p0 AND PostId=@p1",me,id))>0,likeCount=db.Scalar("SELECT LikeCount FROM Posts WHERE PostId=@p0",id),favoriteCount=db.Scalar("SELECT COUNT(*) FROM Favorites WHERE PostId=@p0",id),repostCount=db.Scalar("SELECT COUNT(*) FROM Reposts WHERE PostId=@p0 AND IsHidden=0",id)};
                }
                if(action=="users")
                {
                    string mode=request.QueryString["mode"]??"followers";if(id==0)id=me;
                    string from=mode=="following"?"FROM Follows f JOIN Users u ON u.UserId=f.FolloweeId WHERE f.FollowerId=@p0":"FROM Follows f JOIN Users u ON u.UserId=f.FollowerId WHERE f.FolloweeId=@p0";
                    return Page(db,"u.UserId,u.Username,u.Nickname,u.Bio,u.Avatar,CAST(CASE WHEN EXISTS(SELECT 1 FROM Follows x WHERE x.FollowerId=@p1 AND x.FolloweeId=u.UserId) THEN 1 ELSE 0 END AS bit) AS Following",from,"f.CreatedAt DESC,u.UserId DESC",page,id,me);
                }
                if(action=="notifications") {SignedIn(me);return Page(db,"n.NotificationId,n.Kind,n.PostId,n.IsRead,n.CreatedAt,n.ActorId,u.Nickname","FROM Notifications n JOIN Users u ON u.UserId=n.ActorId WHERE n.UserId=@p0","n.CreatedAt DESC,n.NotificationId DESC",page,me);}
                if(action=="unread")return new {count=me==0?0:Convert.ToInt32(db.Scalar("SELECT COUNT(*) FROM Notifications WHERE UserId=@p0 AND IsRead=0",me))};
                if(action=="history") {SignedIn(me);return Page(db,"SearchId,Keyword,CreatedAt","FROM SearchEvents WHERE UserId=@p0","CreatedAt DESC,SearchId DESC",page,me);}
                if(action=="reports")
                {
                    SignedIn(me);bool admin=IsAdmin(me);
                    return Page(db,"r.ReportId,r.TargetType,r.TargetId,r.Reason,r.State,r.Decision,r.CreatedAt,r.ReviewedAt,u.Nickname AS Reporter",
                        "FROM Reports r JOIN Users u ON u.UserId=r.UserId WHERE "+(admin?"1=1":"r.UserId=@p0"),"r.ReportId DESC",page,me);
                }
                if(action=="reportLog") {if(!IsAdmin(me))throw new HttpException(403,"需要管理员权限");return db.Rows("SELECT a.Verdict,a.Note,a.CreatedAt,u.Nickname FROM ModerationActions a JOIN Users u ON u.UserId=a.AdminId WHERE a.ReportId=@p0 ORDER BY a.ActionId DESC",id);}
                if(action=="taxonomies"||action=="rankings")
                {
                    var tagCounts=new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase);
                    foreach(var row in db.Rows("SELECT p.Tags FROM Posts p WHERE "+Public))
                    {foreach(string tag in PostDAL.SplitTags(Convert.ToString(row["Tags"]))) {if(!tagCounts.ContainsKey(tag))tagCounts[tag]=0;tagCounts[tag]++;}}
                    var tags=tagCounts.OrderByDescending(x=>x.Value).ThenBy(x=>x.Key).Select(x=>new {name=x.Key,count=x.Value}).ToArray();
                    if(action=="taxonomies")return new {categories=TaxonomyDAL.GetCategories(),tags=tags};
                    var hot=db.Rows("SELECT TOP 10 p.PostId,p.Title,p.UserId,u.Nickname,p.LikeCount,p.ViewCount,(p.LikeCount*3+(SELECT COUNT(*) FROM Favorites f WHERE f.PostId=p.PostId)*4+(SELECT COUNT(*) FROM Comments c WHERE c.PostId=p.PostId AND c.IsDeleted=0 AND c.IsHidden=0)*2) AS Score FROM Posts p JOIN Users u ON u.UserId=p.UserId WHERE "+Public+" AND p.CreatedAt>=DATEADD(day,-7,GETDATE()) ORDER BY Score DESC,p.CreatedAt DESC,p.PostId DESC");
                    var authors=db.Rows("SELECT TOP 10 u.UserId,u.Nickname,COUNT(*) AS PostCount FROM Posts p JOIN Users u ON u.UserId=p.UserId WHERE "+Public+" AND p.CreatedAt>=DATEADD(day,-7,GETDATE()) GROUP BY u.UserId,u.Nickname ORDER BY PostCount DESC,u.UserId");
                    var searches=db.Rows("SELECT TOP 10 Keyword,COUNT(DISTINCT UserId) AS Participants FROM SearchEvents WHERE SearchDate>=DATEADD(day,-7,CAST(GETDATE() AS date)) GROUP BY Keyword HAVING COUNT(DISTINCT UserId)>=3 ORDER BY Participants DESC,Keyword");
                    return new {posts=hot,authors=authors,tags=tags.Take(15).ToArray(),searches=searches};
                }
                throw new HttpException(400,"未知查询");
            }
        }
        public static object Write(string action,int me,HttpRequest request)
        {
            SignedIn(me);int id=Number(request.Form["id"]);int postId=Number(request.Form["postId"]);
            using(var db=new CommunityDAL(true))
            {
                if(action=="follow"||action=="unfollow")
                {
                    if(id==me)throw new HttpException(400,"不能关注自己");
                    if(Convert.ToInt32(db.Scalar("SELECT COUNT(*) FROM Users WHERE UserId=@p0",id))==0)throw new HttpException(404,"用户不存在");
                    if(action=="follow") {if(Convert.ToInt32(db.Scalar("SELECT COUNT(*) FROM Follows WHERE FollowerId=@p0 AND FolloweeId=@p1",me,id))==0){db.Execute("INSERT Follows(FollowerId,FolloweeId) VALUES(@p0,@p1)",me,id);Notify(db,id,me,"follow",null,"follow:"+me);}}
                    else db.Execute("DELETE Follows WHERE FollowerId=@p0 AND FolloweeId=@p1",me,id);
                }
                else if(new[]{"like","unlike","favorite","unfavorite","repost","unrepost"}.Contains(action))
                {
                    int owner=Author(db,postId);
                    if(action=="like"||action=="favorite"||action=="repost")
                    {
                        string table=action=="like"?"Likes":action=="favorite"?"Favorites":"Reposts";
                        if(Convert.ToInt32(db.Scalar("SELECT COUNT(*) FROM "+table+" WHERE UserId=@p0 AND PostId=@p1",me,postId))==0)
                        {if(action=="repost")db.Execute("INSERT Reposts(UserId,PostId,Note) VALUES(@p0,@p1,@p2)",me,postId,Text(request,"note",300));else db.Execute("INSERT "+table+"(UserId,PostId) VALUES(@p0,@p1)",me,postId);Notify(db,owner,me,action,postId,action+":"+postId+":"+me);}
                    }
                    else {string table=action=="unlike"?"Likes":action=="unfavorite"?"Favorites":"Reposts";db.Execute("DELETE "+table+" WHERE UserId=@p0 AND PostId=@p1",me,postId);}
                    db.Execute("UPDATE Posts SET LikeCount=(SELECT COUNT(*) FROM Likes WHERE PostId=@p0) WHERE PostId=@p0",postId);
                }
                else if(action=="likeComment"||action=="unlikeComment")
                {
                    // Lock the target comment before modifying likes, so concurrent
                    // sessions and comment deletion cannot create duplicate/stale writes.
                    var comments=db.Rows("SELECT c.UserId,c.PostId FROM Comments c WITH (UPDLOCK) JOIN Posts p ON p.PostId=c.PostId WHERE c.CommentId=@p0 AND c.IsDeleted=0 AND c.IsHidden=0 AND "+Public,id);
                    if(comments.Count==0)throw new HttpException(404,"评论不存在或当前不可见");
                    if(action=="likeComment")
                    {
                        if(Convert.ToInt32(db.Scalar("SELECT COUNT(*) FROM CommentLikes WHERE CommentId=@p0 AND UserId=@p1",id,me))==0)
                        {
                            db.Execute("INSERT CommentLikes(CommentId,UserId) VALUES(@p0,@p1)",id,me);
                            Notify(db,Convert.ToInt32(comments[0]["UserId"]),me,"commentLike",Convert.ToInt32(comments[0]["PostId"]),"commentLike:"+id+":"+me);
                        }
                    }
                    else db.Execute("DELETE CommentLikes WHERE CommentId=@p0 AND UserId=@p1",id,me);
                    int count=Convert.ToInt32(db.Scalar("SELECT COUNT(*) FROM CommentLikes WHERE CommentId=@p0",id));
                    bool liked=Convert.ToInt32(db.Scalar("SELECT COUNT(*) FROM CommentLikes WHERE CommentId=@p0 AND UserId=@p1",id,me))>0;
                    db.Commit();return new {ok=true,commentId=id,likeCount=count,liked=liked};
                }
                else if(action=="addComment")
                {
                    int owner=Author(db,postId);string content=Text(request,"content",1000,true);int parent=Number(request.Form["parentId"]);int? parentId=parent==0?(int?)null:parent;
                    if(parentId.HasValue&&Convert.ToInt32(db.Scalar("SELECT COUNT(*) FROM Comments WHERE CommentId=@p0 AND PostId=@p1 AND ParentId IS NULL AND IsDeleted=0 AND IsHidden=0",parent,postId))==0)throw new HttpException(400,"只能回复有效的一级评论");
                    if(Convert.ToInt32(db.Scalar("SELECT COUNT(*) FROM Comments WHERE UserId=@p0 AND PostId=@p1 AND CONVERT(varbinary(max),Content)=CONVERT(varbinary(max),@p2) AND CreatedAt>DATEADD(second,-30,GETDATE()) AND IsDeleted=0",me,postId,content))>0)throw new HttpException(409,"这条评论刚刚发布过，请勿重复提交");
                    int commentId=Convert.ToInt32(db.Scalar("INSERT Comments(PostId,UserId,ParentId,Content) VALUES(@p0,@p1,@p2,@p3);SELECT CAST(SCOPE_IDENTITY() AS int)",postId,me,parentId,content));
                    Notify(db,owner,me,"comment",postId,"comment:"+commentId);
                    if(parentId.HasValue)Notify(db,Convert.ToInt32(db.Scalar("SELECT UserId FROM Comments WHERE CommentId=@p0",parent)),me,"reply",postId,"reply:"+commentId);
                }
                else if(action=="deleteComment") {if(db.Execute("UPDATE Comments SET IsDeleted=1,Content=N'' WHERE CommentId=@p0 AND UserId=@p1 AND IsDeleted=0",id,me)==0)throw new HttpException(403,"评论不存在或不属于你");}
                else if(action=="deleteRepost") {if(db.Execute("DELETE Reposts WHERE RepostId=@p0 AND UserId=@p1",id,me)==0)throw new HttpException(403,"转发不存在或不属于你");}
                else if(action=="delete"||action=="restore"||action=="purge"||action=="withdraw")
                {
                    if(Author(db,postId,false)!=me)throw new HttpException(403,"只能操作自己的文章");
                    if(action=="delete")db.Execute("UPDATE Posts SET DeletedAt=GETDATE() WHERE PostId=@p0 AND DeletedAt IS NULL",postId);
                    if(action=="restore")db.Execute("UPDATE Posts SET DeletedAt=NULL WHERE PostId=@p0",postId);
                    if(action=="withdraw")db.Execute("UPDATE Posts SET Status=N'Draft',UpdatedAt=GETDATE() WHERE PostId=@p0 AND DeletedAt IS NULL",postId);
                    if(action=="purge")
                    {
                        if(Convert.ToInt32(db.Scalar("SELECT COUNT(*) FROM Posts WHERE PostId=@p0 AND DeletedAt IS NOT NULL",postId))==0)throw new HttpException(400,"请先将文章移入回收站");
                        db.Execute("UPDATE Reposts SET PostId=NULL WHERE PostId=@p0;UPDATE Notifications SET PostId=NULL WHERE PostId=@p0;DELETE Favorites WHERE PostId=@p0;DELETE Likes WHERE PostId=@p0;UPDATE Comments SET ParentId=NULL WHERE PostId=@p0;DELETE Comments WHERE PostId=@p0;DELETE PostImages WHERE PostId=@p0;DELETE PostViews WHERE PostId=@p0;DELETE Posts WHERE PostId=@p0",postId);
                    }
                }
                else if(action=="markRead")db.Execute("UPDATE Notifications SET IsRead=1 WHERE UserId=@p0 AND (@p1=0 OR NotificationId=@p1)",me,id);
                else if(action=="clearHistory")db.Execute("DELETE SearchEvents WHERE UserId=@p0",me);
                else if(action=="recordSearch")
                {
                    string keyword=Text(request,"keyword",100).ToLowerInvariant();
                    if(keyword.Length>=2&&keyword.Length<=30&&Regex.IsMatch(keyword,@"^[\p{L}\p{N} .#+_-]+$")&&!Regex.IsMatch(keyword,@"\d{6,}")&&Convert.ToInt32(db.Scalar("SELECT COUNT(*) FROM Posts p WHERE "+Public+" AND (CHARINDEX(@p0,p.Title)>0 OR CHARINDEX(@p0,p.Content)>0)",keyword))>0)
                        db.Execute("IF NOT EXISTS(SELECT 1 FROM SearchEvents WHERE UserId=@p0 AND Keyword=@p1 AND SearchDate=CAST(GETDATE() AS date)) INSERT SearchEvents(UserId,Keyword) VALUES(@p0,@p1)",me,keyword);
                }
                else if(action=="profile")
                {db.Execute("UPDATE Users SET Nickname=@p0,Bio=@p1 WHERE UserId=@p2",Text(request,"nickname",50,true),Text(request,"bio",500),me);}
                else if(action=="password")
                {
                    var user=UserDAL.GetById(me);string old=Text(request,"oldPassword",200,true,false),password=Text(request,"newPassword",200,true,false);
                    if(!SecurityHelper.VerifyPassword(old,user.Salt,user.PasswordHash))throw new HttpException(400,"原密码不正确");
                    if(password.Length<8)throw new HttpException(400,"新密码至少 8 位");
                    if(password!=Text(request,"confirm",200,true,false))throw new HttpException(400,"两次新密码不一致");
                    string salt=SecurityHelper.GenerateSalt();db.Execute("UPDATE Users SET Salt=@p0,PasswordHash=@p1 WHERE UserId=@p2",salt,SecurityHelper.HashPassword(password,salt),me);
                }
                else if(action=="report")
                {
                    string type=Text(request,"targetType",12,true),reason=Text(request,"reason",500,true);
                    if(type=="post")Author(db,id);
                    else if(type=="comment") {object p=db.Scalar("SELECT PostId FROM Comments WHERE CommentId=@p0 AND IsDeleted=0 AND IsHidden=0",id);if(p==null)throw new HttpException(404,"评论不可举报");Author(db,Convert.ToInt32(p));}
                    else if(type=="repost") {if(Convert.ToInt32(db.Scalar("SELECT COUNT(*) FROM Reposts WHERE RepostId=@p0 AND IsHidden=0",id))==0)throw new HttpException(404,"转发不存在");}
                    else throw new HttpException(400,"未知举报类型");
                    if(Convert.ToInt32(db.Scalar("SELECT COUNT(*) FROM Reports WHERE UserId=@p0 AND TargetType=@p1 AND TargetId=@p2 AND State='Pending'",me,type,id))>0)throw new HttpException(409,"你已经提交过举报，请等待处理");
                    db.Execute("INSERT Reports(UserId,TargetType,TargetId,Reason) VALUES(@p0,@p1,@p2,@p3)",me,type,id,reason);
                }
                else if(action=="reviewReport")
                {
                    if(Convert.ToInt32(db.Scalar("SELECT COUNT(*) FROM Users WHERE UserId=@p0 AND IsAdmin=1",me))==0)throw new HttpException(403,"需要管理员权限");
                    string verdict=Text(request,"verdict",12,true),decision=Text(request,"decision",500,true);
                    if(!new[]{"hide","unhide","dismiss"}.Contains(verdict))throw new HttpException(400,"处理方式不正确");
                    var reports=db.Rows("SELECT TargetType,TargetId FROM Reports WHERE ReportId=@p0",id);if(reports.Count==0)throw new HttpException(404,"举报不存在");
                    var report=reports[0];string type=Convert.ToString(report["TargetType"]);
                    if(verdict!="dismiss") {string table=type=="post"?"Posts":type=="comment"?"Comments":"Reposts";string key=type=="post"?"PostId":type=="comment"?"CommentId":"RepostId";db.Execute("UPDATE "+table+" SET IsHidden=@p0 WHERE "+key+"=@p1",verdict=="hide",report["TargetId"]);}
                    db.Execute("UPDATE Reports SET State='Resolved',Decision=@p0,ReviewerId=@p1,ReviewedAt=GETDATE() WHERE ReportId=@p2",decision,me,id);
                    db.Execute("INSERT ModerationActions(ReportId,AdminId,Verdict,Note) VALUES(@p0,@p1,@p2,@p3)",id,me,verdict,decision);
                }
                else throw new HttpException(400,"未知操作");
                db.Commit();return new {ok=true};
            }
        }
    }
}
