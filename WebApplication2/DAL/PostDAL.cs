using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using WebApplication2.Models;

namespace WebApplication2.DAL
{
    public static class PostDAL
    {
        public const string PublicCondition = "p.Status=N'Published' AND p.DeletedAt IS NULL AND p.IsHidden=0";
        private const string SelectPost = "SELECT p.*,"+TaxonomyDAL.CategoryNamesSql+" AS CategoryNames,u.Username AS AuthorName,u.Nickname AS AuthorNickname FROM Posts p INNER JOIN Users u ON p.UserId=u.UserId ";
        private static Post Map(DataRow row)
        {
            return new Post { PostId=(int)row["PostId"], UserId=(int)row["UserId"], Title=row["Title"].ToString(), Summary=row["Summary"].ToString(), Content=row["Content"].ToString(), Category=row["CategoryNames"].ToString(), Tags=row["Tags"].ToString(), LikeCount=(int)row["LikeCount"], ViewCount=(int)row["ViewCount"], CreatedAt=(DateTime)row["CreatedAt"], UpdatedAt=row["UpdatedAt"]==DBNull.Value?(DateTime?)null:(DateTime)row["UpdatedAt"], Status=row["Status"].ToString(), DeletedAt=row["DeletedAt"]==DBNull.Value?(DateTime?)null:(DateTime)row["DeletedAt"], IsHidden=(bool)row["IsHidden"], AuthorName=row["AuthorName"].ToString(), AuthorNickname=row["AuthorNickname"].ToString() };
        }
        private static List<Post> List(string sql, params SqlParameter[] parameters) { return DbHelper.ExecuteDataTable(sql,parameters).AsEnumerable().Select(Map).ToList(); }
        public static string[] SplitTags(string tags) { return Regex.Split(tags ?? "", @"[\s,，;；]+").Where(t=>t.Length>0).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(); }
        public static string NormalizeTags(string tags) { return String.Join(" ",SplitTags(tags)); }
        public static List<Post> GetPaged(int page,int size,string keyword,out int total) { return GetPaged(page,size,keyword,"","","newest",out total); }
        public static List<Post> GetPaged(int page,int size,string keyword,string category,string tag,string sort,out int total)
        {
            string where=" WHERE "+PublicCondition;
            var values=new List<SqlParameter>();
            if (!String.IsNullOrWhiteSpace(keyword))
            {
                where+=" AND (p.Title LIKE @Keyword ESCAPE '\\' OR p.Content LIKE @Keyword ESCAPE '\\' OR p.Summary LIKE @Keyword ESCAPE '\\' OR u.Username LIKE @Keyword ESCAPE '\\')";
                string escaped=keyword.Replace("\\","\\\\").Replace("%","\\%").Replace("_","\\_").Replace("[","\\[");
                values.Add(new SqlParameter("@Keyword","%"+escaped+"%"));
            }
            if (!String.IsNullOrEmpty(category)) { where+=" AND EXISTS(SELECT 1 FROM PostCategories pc JOIN Categories c ON c.CategoryId=pc.CategoryId WHERE pc.PostId=p.PostId AND c.Name=@Category)";values.Add(new SqlParameter("@Category",category)); }
            if (!String.IsNullOrEmpty(tag))
            {
                where+=" AND CHARINDEX(N' '+@Tag+N' ',N' '+REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(ISNULL(p.Tags,N''),N',',N' '),N'，',N' '),N';',N' '),N'；',N' '),CHAR(9),N' '),CHAR(10),N' ')+N' ')>0";
                values.Add(new SqlParameter("@Tag",tag));
            }
            total=Convert.ToInt32(DbHelper.ExecuteScalar("SELECT COUNT(*) FROM Posts p INNER JOIN Users u ON p.UserId=u.UserId"+where,values.ToArray()));
            string order=sort=="oldest"?"p.CreatedAt ASC,p.PostId ASC":sort=="likes"?"p.LikeCount DESC,p.CreatedAt DESC,p.PostId DESC":sort=="views"?"p.ViewCount DESC,p.CreatedAt DESC,p.PostId DESC":"p.CreatedAt DESC,p.PostId DESC";
            values.Add(new SqlParameter("@Offset",(Math.Max(1,Math.Min(page,100000))-1)*size));values.Add(new SqlParameter("@Size",size));
            return List(SelectPost+where+" ORDER BY "+order+" OFFSET @Offset ROWS FETCH NEXT @Size ROWS ONLY",values.ToArray());
        }
        public static Post GetById(int id) { return List(SelectPost+" WHERE p.PostId=@Id",new SqlParameter("@Id",id)).FirstOrDefault(); }
        public static List<Post> GetByUser(int id) { return List(SelectPost+" WHERE p.UserId=@Id AND p.DeletedAt IS NULL AND p.Status=N'Published' ORDER BY p.CreatedAt DESC,p.PostId DESC",new SqlParameter("@Id",id)); }
        public static List<Post> GetRecent(int count) { int total;return GetPaged(1,count,"",out total); }
        public static bool IsPublic(Post p) { return p!=null&&p.Status=="Published"&&!p.IsHidden&&!p.DeletedAt.HasValue; }
        public static int Save(Post post)
        {
            post.Category=TaxonomyDAL.NormalizeCategories(TaxonomyDAL.SplitCategories(post.Category),post.Status=="Published");
            post.Tags=TaxonomyDAL.NormalizeTags(SplitTags(post.Tags));
            var imageIds=Regex.Matches(post.Content??"",@"!\[[^\]\r\n]*\]\(Handlers/MediaHandler\.ashx\?id=(\d+)\)").Cast<Match>().Select(m=>Int32.Parse(m.Groups[1].Value)).Distinct().ToArray();
            using(var db=new CommunityDAL(true))
            {
                foreach(int imageId in imageIds)
                    if(Convert.ToInt32(db.Scalar("SELECT COUNT(*) FROM PostImages WHERE ImageId=@p0 AND UserId=@p1 AND IsAvatar=0 AND (PostId IS NULL OR PostId=@p2)",imageId,post.UserId,post.PostId))==0) throw new HttpException(400,"正文包含不可关联的图片，请重新上传");
                if(post.PostId==0)
                    post.PostId=Convert.ToInt32(db.Scalar("INSERT Posts(UserId,Title,Summary,Content,Category,Tags,Status) VALUES(@p0,@p1,@p2,@p3,@p4,@p5,@p6);SELECT CAST(SCOPE_IDENTITY() AS int)",post.UserId,post.Title,post.Summary,post.Content,post.Category,post.Tags,post.Status));
                else if(db.Execute("UPDATE Posts SET Title=@p0,Summary=@p1,Content=@p2,Category=@p3,Tags=@p4,Status=@p5,UpdatedAt=GETDATE() WHERE PostId=@p6 AND UserId=@p7 AND DeletedAt IS NULL",post.Title,post.Summary,post.Content,post.Category,post.Tags,post.Status,post.PostId,post.UserId)!=1) throw new HttpException(403,"文章不存在或没有编辑权限");
                db.Execute("DELETE PostCategories WHERE PostId=@p0",post.PostId);
                foreach(string category in TaxonomyDAL.SplitCategories(post.Category))
                    db.Execute("INSERT PostCategories(PostId,CategoryId) SELECT @p0,CategoryId FROM Categories WHERE Name=@p1",post.PostId,category);
                foreach(var row in db.Rows("SELECT ImageId FROM PostImages WHERE PostId=@p0",post.PostId)) if(!imageIds.Contains(Convert.ToInt32(row["ImageId"]))) db.Execute("DELETE PostImages WHERE ImageId=@p0",row["ImageId"]);
                foreach(int imageId in imageIds) db.Execute("UPDATE PostImages SET PostId=@p0 WHERE ImageId=@p1",post.PostId,imageId);
                db.Commit();return post.PostId;
            }
        }
        public static int Create(Post post) { post.Status=post.Status??"Published";return Save(post); }
        public static bool Update(Post post) { Save(post);return true; }
        public static bool Delete(int postId) { return DbHelper.ExecuteNonQuery("UPDATE Posts SET DeletedAt=GETDATE() WHERE PostId=@Id AND DeletedAt IS NULL",new SqlParameter("@Id",postId))==1; }
        public static void RecordView(int id,string viewer)
        {
            using(var db=new CommunityDAL(true))
            {
                if(Convert.ToInt32(db.Scalar("SELECT COUNT(*) FROM PostViews WHERE PostId=@p0 AND ViewerKey=@p1 AND ViewDate=CAST(GETDATE() AS date)",id,viewer))==0)
                { db.Execute("INSERT PostViews(PostId,ViewerKey) VALUES(@p0,@p1)",id,viewer);db.Execute("UPDATE Posts SET ViewCount=ViewCount+1 WHERE PostId=@p0",id); }
                db.Execute("IF NOT EXISTS(SELECT 1 FROM ReadingDaily WHERE PostId=@p0 AND ReaderKey=@p1 AND ReadingDate=@p2) INSERT ReadingDaily(PostId,ReaderKey,ReadingDate,LastHeartbeatAt) VALUES(@p0,@p1,@p2,@p3)",id,viewer,WebApplication2.Services.AnalyticsService.Now.Date,WebApplication2.Services.AnalyticsService.Now);
                db.Commit();
            }
        }
        public static int GetLikeCount(int id) { return Convert.ToInt32(DbHelper.ExecuteScalar("SELECT ISNULL(MAX(LikeCount),0) FROM Posts WHERE PostId=@Id",new SqlParameter("@Id",id))); }
    }
}
