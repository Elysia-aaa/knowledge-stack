using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Web;
using System.Web.SessionState;
using WebApplication2.DAL;
using WebApplication2.Services;
using WebApplication2.Utils;

namespace WebApplication2.Handlers
{
    public class MediaHandler : IHttpHandler, IRequiresSessionState
    {
        public void ProcessRequest(HttpContext context)
        {
            try
            {
                int me=RequestSecurity.UserId(context);
                if(context.Request.HttpMethod=="GET")
                {
                    int id=CommunityService.Number(context.Request.QueryString["id"]);
                    using(var db=new CommunityDAL())
                    {
                        var images=db.Rows("SELECT i.UserId,i.PostId,i.IsAvatar,i.ImageData,i.ContentType FROM PostImages i WHERE ImageId=@p0",id);
                        if(images.Count==0)throw new HttpException(404,"图片不存在");var image=images[0];
                        if(!(bool)image["IsAvatar"])
                        {
                            bool owner=me>0&&Convert.ToInt32(image["UserId"])==me;
                            bool visible=image["PostId"]!=null&&PostDAL.IsPublic(PostDAL.GetById(Convert.ToInt32(image["PostId"])));
                            if(!owner&&!visible)throw new HttpException(404,"图片不可访问");
                        }
                        context.Response.ContentType=(string)image["ContentType"];
                        context.Response.Headers["X-Content-Type-Options"]="nosniff";
                        context.Response.Cache.SetCacheability(HttpCacheability.Private);
                        context.Response.Cache.SetNoStore();
                        context.Response.BinaryWrite((byte[])image["ImageData"]);
                    }
                    return;
                }
                RequestSecurity.RequireWrite(context);
                var file=context.Request.Files["file"];
                if(file==null||file.ContentLength==0||file.ContentLength>2*1024*1024)throw new HttpException(400,"请选择不超过 2 MB 的 JPEG 或 PNG 图片");
                string extension=Path.GetExtension(file.FileName).ToLowerInvariant();
                if(extension!=".png"&&extension!=".jpg"&&extension!=".jpeg")throw new HttpException(400,"只支持 JPEG、PNG 图片");
                byte[] data;
                try
                {
                    using(var input=Image.FromStream(file.InputStream,true,true))
                    {
                        bool png=input.RawFormat.Guid==ImageFormat.Png.Guid,jpeg=input.RawFormat.Guid==ImageFormat.Jpeg.Guid;
                        if((!png&&!jpeg)||(png&&extension!=".png")||(jpeg&&extension==".png")||input.Width>6000||input.Height>6000||(long)input.Width*input.Height>16000000)throw new ArgumentException();
                        // Decode and re-encode: uploaded names, metadata and appended payloads are not served.
                        using(var bitmap=new Bitmap(input))using(var output=new MemoryStream()){bitmap.Save(output,ImageFormat.Png);data=output.ToArray();}
                    }
                }
                catch(ArgumentException){throw new HttpException(400,"图片格式或尺寸无效，请选择正常的 JPEG/PNG 图片");}
                if(data.Length>2*1024*1024)throw new HttpException(400,"图片解码后超过 2 MB，请压缩后再上传");
                bool avatar=context.Request.QueryString["avatar"]=="1";
                using(var db=new CommunityDAL(true))
                {
                    db.Execute("DELETE PostImages WHERE UserId=@p0 AND PostId IS NULL AND IsAvatar=0 AND CreatedAt<DATEADD(day,-1,GETDATE())",me);
                    if(avatar)db.Execute("DELETE PostImages WHERE UserId=@p0 AND IsAvatar=1",me);
                    long used=Convert.ToInt64(db.Scalar("SELECT ISNULL(SUM(CAST(SizeBytes AS bigint)),0) FROM PostImages WHERE UserId=@p0",me));
                    if(used+data.Length>20*1024*1024)throw new HttpException(400,"图片总量已达到 20 MB，请先清理不再需要的文章图片");
                    int id=Convert.ToInt32(db.Scalar("INSERT PostImages(UserId,IsAvatar,ContentType,ImageData,SizeBytes) VALUES(@p0,@p1,'image/png',@p2,@p3);SELECT CAST(SCOPE_IDENTITY() AS int)",me,avatar,data,data.Length));
                    string url="Handlers/MediaHandler.ashx?id="+id;
                    if(avatar)db.Execute("UPDATE Users SET Avatar=@p0 WHERE UserId=@p1",url,me);
                    db.Commit();RequestSecurity.Json(context,new {ok=true,id=id,url=url},201);
                }
            }
            catch(HttpException ex){RequestSecurity.Json(context,new {error=ex.Message},ex.GetHttpCode());}
            catch(Exception ex){System.Diagnostics.Trace.TraceError(ex.ToString());RequestSecurity.Json(context,new {error="图片处理失败，请稍后再试"},500);}
        }
        public bool IsReusable {get{return false;}}
    }
}
