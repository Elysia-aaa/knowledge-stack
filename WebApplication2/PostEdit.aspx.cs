using System;
using System.Web;
using System.Web.UI;
using System.Linq;
using System.Web.Script.Serialization;
using WebApplication2.DAL;
using WebApplication2.Models;

namespace WebApplication2
{
    public partial class PostEdit : System.Web.UI.Page
    {
        protected string ErrorHtml = "";
        protected string PageTitleText = "写文章";
        protected string TitleValue = "", SummaryValue = "", ContentValue = "", CategoryValue = "", TagsJsonValue = "[]", CategoryOptionsHtml = "";
        protected int EditingId = 0;
        protected string StatusValue="Published";

        protected void Page_Load(object sender, EventArgs e)
        {
            // 必须登录才能写文章
            if (Session["UserId"] == null)
            {
                Response.Redirect("Login.aspx");
                return;
            }
            int userId = Convert.ToInt32(Session["UserId"]);

            // 优先从表单隐藏域读取（postback 时查询串可能丢失），其次从 URL 查询串读取
            int id = 0;
            string idStr = Request.Form["postId"] ?? Request.QueryString["id"];
            int.TryParse(idStr, out id);

            if (id > 0)
            {
                // 编辑模式
                PageTitleText = "编辑文章";
                EditingId = id;
                Post post = PostDAL.GetById(id);
                if (post == null || post.DeletedAt.HasValue)
                {
                    Response.Redirect("PostList.aspx");
                    return;
                }
                // 简单权限控制：只能编辑自己发布的文章
                if (post.UserId != userId)
                {
                    Response.Redirect("PostList.aspx");
                    return;
                }

                if (!IsPostBack)
                {
                    TitleValue = post.Title;
                    SummaryValue = post.Summary;
                    ContentValue = post.Content;
                    CategoryValue = post.Category;
                    TagsJsonValue = new JavaScriptSerializer().Serialize(PostDAL.SplitTags(post.Tags));
                    StatusValue=post.Status;
                }
            }

            if (IsPostBack)
            {
                TitleValue = Request.Form["title"] ?? "";
                SummaryValue = Request.Form["summary"] ?? "";
                ContentValue = Request.Form["content"] ?? "";
                CategoryValue = String.Join("|",Request.Form.GetValues("categories")??new string[0]);
                TagsJsonValue = Request.Form["tagsJson"] ?? "[]";
                try { HandleSave(userId, id); }
                catch(HttpException ex) {ErrorHtml=Alert(ex.Message);}
            }
            var selected=TaxonomyDAL.SplitCategories(CategoryValue);
            CategoryOptionsHtml=String.Join("",TaxonomyDAL.GetCategories().Select((name,index)=>
                "<label class=\"category-option\" for=\"discipline"+index+"\"><input type=\"checkbox\" id=\"discipline"+index+"\" name=\"categories\" value=\""+HttpUtility.HtmlAttributeEncode(name)+"\""+(selected.Contains(name)?" checked":"")+" /><span>"+HttpUtility.HtmlEncode(name)+"</span></label>"));
        }

        private void HandleSave(int userId, int id)
        {
            string title = (Request.Form["title"] ?? "").Trim();
            string summary = (Request.Form["summary"] ?? "").Trim();
            string content = Request.Form["content"] ?? "";
            string status=Request.Form["saveAs"]=="Draft"?"Draft":"Published";
            StatusValue=status;
            string category = TaxonomyDAL.NormalizeCategories(Request.Form.GetValues("categories"),status=="Published");
            string tags = TaxonomyDAL.ParseTags(Request.Form["tagsJson"]??"[]");
            CategoryValue=category;
            TagsJsonValue=new JavaScriptSerializer().Serialize(PostDAL.SplitTags(tags));

            if (title.Length == 0) { ErrorHtml = Alert("标题不能为空"); return; }
            if (status=="Published"&&content.Trim().Length == 0) { ErrorHtml = Alert("发布文章时正文不能为空"); return; }
            if(title.Length>200||summary.Length>500||content.Length>100000) {ErrorHtml=Alert("内容过长：标题最多200字，摘要500字，正文10万字");return;}

            if (id > 0)
            {
                Post post = new Post
                {
                    PostId = id,
                    UserId=userId,
                    Status=status,
                    Title = title,
                    Summary = summary,
                    Content = content,
                    Category = category,
                    Tags = tags
                };
                PostDAL.Update(post);
                Response.Redirect("PostDetail.aspx?id=" + id);
            }
            else
            {
                Post post = new Post
                {
                    UserId = userId,
                    Status=status,
                    Title = title,
                    Summary = summary,
                    Content = content,
                    Category = category,
                    Tags = tags
                };
                int newId = PostDAL.Create(post);
                Response.Redirect("PostDetail.aspx?id=" + newId);
            }
        }

        private string Alert(string msg)
        {
            return "<div class=\"alert-error\">" + HttpUtility.HtmlEncode(msg) + "</div>";
        }
    }
}
