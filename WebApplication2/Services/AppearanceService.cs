using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Script.Serialization;
using WebApplication2.DAL;

namespace WebApplication2.Services
{
    public static class AppearanceService
    {
        private static readonly HashSet<string> Themes=new HashSet<string>{"light","dark","ocean","forest","sakura","amber","custom"};
        private static readonly HashSet<string> Keys=new HashSet<string>{"--primary","--primary-dark","--on-primary","--link","--bg","--card-bg","--surface","--header-bg","--text","--text-light","--border","--strong-border","--code-bg","--pre-bg","--pre-text","--selected-bg","--selected-text","--sidebar-bg"};
        public static object Read(int me)
        {
            if(me==0)return new{userId=0,theme="system",customBase="light",colors=new Dictionary<string,string>()};
            using(var db=new CommunityDAL())
            {
                var rows=db.Rows("SELECT ThemeKey,CustomBase,CustomJson FROM UserAppearance WHERE UserId=@p0",me);
                if(rows.Count==0)return new{userId=me,theme="light",customBase="light",colors=new Dictionary<string,string>()};
                var r=rows[0];return new{userId=me,theme=r["ThemeKey"],customBase=r["CustomBase"],colors=new JavaScriptSerializer().Deserialize<Dictionary<string,string>>((string)r["CustomJson"])};
            }
        }
        public static object Save(int me,HttpRequest request)
        {
            string theme=request.Form["theme"]??"",basis=request.Form["customBase"]??"light",raw=request.Form["colors"]??"{}";
            if(!Themes.Contains(theme)||!Themes.Contains(basis)||basis=="custom"||raw.Length>5000)throw new HttpException(400,"主题参数无效");
            Dictionary<string,string> colors;
            try{colors=new JavaScriptSerializer().Deserialize<Dictionary<string,string>>(raw);}catch{throw new HttpException(400,"配色格式无效");}
            if(colors==null||colors.Count>Keys.Count)throw new HttpException(400,"配色数量无效");
            foreach(var pair in colors)if(!Keys.Contains(pair.Key)||pair.Value==null||!Regex.IsMatch(pair.Value,@"\A#[0-9a-fA-F]{6}\z"))throw new HttpException(400,"配色只允许六位 HEX 颜色");
            using(var db=new CommunityDAL(true))
            {
                string json=new JavaScriptSerializer().Serialize(colors);
                if(db.Execute("UPDATE UserAppearance SET ThemeKey=@p0,CustomBase=@p1,CustomJson=@p2,UpdatedAt=GETDATE() WHERE UserId=@p3",theme,basis,json,me)==0)
                    db.Execute("INSERT UserAppearance(UserId,ThemeKey,CustomBase,CustomJson) VALUES(@p0,@p1,@p2,@p3)",me,theme,basis,json);
                db.Commit();
            }
            return Read(me);
        }
    }
}
