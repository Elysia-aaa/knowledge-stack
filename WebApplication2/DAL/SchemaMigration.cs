using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
namespace WebApplication2.DAL
{
    public static class SchemaMigration
    {
        public static void Apply()
        {
            foreach(string resource in new[]{"WebApplication2.Extensions.sql","WebApplication2.Taxonomy.sql","WebApplication2.CommentLikes.sql","WebApplication2.Studio.sql"})
            using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream(resource))
            using(var reader=new StreamReader(stream))
                foreach(string sql in Regex.Split(reader.ReadToEnd(),@"^GO\s*$",RegexOptions.Multiline|RegexOptions.IgnoreCase))
                    if(!string.IsNullOrWhiteSpace(sql))DbHelper.ExecuteNonQuery(sql);
        }
    }
}
