using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Script.Serialization;

namespace WebApplication2.DAL
{
    /// <summary>固定学科目录和标签规则；分类通过 PostCategories 建立多对多关联。</summary>
    public static class TaxonomyDAL
    {
        // Queries using this expression must alias Posts as p. No user text enters SQL.
        public const string CategoryNamesSql = "ISNULL(STUFF((SELECT N'|'+c.Name FROM PostCategories pc JOIN Categories c ON c.CategoryId=pc.CategoryId WHERE pc.PostId=p.PostId ORDER BY c.SortOrder,c.CategoryId FOR XML PATH(''),TYPE).value('.','nvarchar(max)'),1,1,N''),N'')";

        public static string[] GetCategories()
        {
            using(var db=new CommunityDAL())
                return db.Rows("SELECT Name FROM Categories ORDER BY SortOrder,CategoryId").Select(row=>Convert.ToString(row["Name"])).ToArray();
        }

        public static string[] SplitCategories(string value)
        { return (value??"").Split(new[]{'|'},StringSplitOptions.RemoveEmptyEntries); }

        public static string NormalizeCategories(IEnumerable<string> values,bool required)
        {
            var requested=(values??new string[0]).Select(x=>(x??"").Trim()).Distinct(StringComparer.Ordinal).ToArray();
            var allowed=GetCategories();
            if(requested.Any(x=>!allowed.Contains(x)))throw new HttpException(400,"分类只能从固定学科目录中选择");
            if(required&&requested.Length==0)throw new HttpException(400,"发布文章时请至少选择一个学科分类");
            return String.Join("|",allowed.Where(requested.Contains));
        }

        public static string ParseTags(string json)
        {
            if(String.IsNullOrWhiteSpace(json))return "";
            if(json.Length>4096)throw new HttpException(400,"标签数据过长");
            string[] tags;
            try { tags=new JavaScriptSerializer().Deserialize<string[]>(json); }
            catch(Exception ex) when(ex is ArgumentException||ex is InvalidOperationException)
            { throw new HttpException(400,"标签格式无效，请逐个添加标签"); }
            return NormalizeTags(tags??new string[0]);
        }

        public static string NormalizeTags(IEnumerable<string> values)
        {
            var tags=new List<string>();
            foreach(string raw in values)
            {
                string tag=(raw??"").Trim().TrimStart('#');
                if(tag.Length==0||tag.Length>20||tag.Any(c=>Char.IsWhiteSpace(c)||Char.IsControl(c)||",，;；".Contains(c)))
                    throw new HttpException(400,"每个标签须为 1–20 个字符，不能包含空格、逗号、分号或换行");
                if(!tags.Contains(tag,StringComparer.OrdinalIgnoreCase))tags.Add(tag);
            }
            if(tags.Count>10)throw new HttpException(400,"每篇文章最多添加 10 个标签");
            return String.Join(" ",tags);
        }
    }
}
