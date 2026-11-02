using System;
using WebApplication2.DAL;

namespace WebApplication2
{
    public class Global : System.Web.HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            // 应用启动时自动建库/建表/补齐迁移，任何人（含拉代码的队员）第一次 F5 即可正常使用，
            // 无需手动访问 Setup.aspx。方法幂等，重复执行安全。
            DatabaseInitializer.EnsureInitialized();
        }
    }
}
