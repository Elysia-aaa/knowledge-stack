<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Setup.aspx.cs" Inherits="WebApplication2.Setup" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>数据库初始化 - 我的知识小栈</title>
    <script src="Scripts/theme.js?v=20261019-mono2"></script>
    <link rel="stylesheet" href="Content/site.css?v=20261019-reactions" />
</head>
<body>
    <main class="container main">
        <div class="card form-card" style="text-align:center;">
            <h1>🛠️ 数据库初始化</h1>
            <p>
                首次运行请点击下方按钮，系统将自动创建 <strong>KnowledgeStack</strong> 数据库、
                基础表与社区扩展表。已有账号和文章会保留。仅空库创建演示账号 <code>demo</code>（密码 <code>demo123</code>）。
            </p>
            <form method="post">
                <input type="hidden" name="csrf" value="<%: CsrfToken %>" />
                <button type="submit" name="action" value="init" class="btn">一键初始化数据库</button>
            </form>
            <%= ResultHtml %>
            <p style="margin-top:20px;"><a href="Default.aspx">前往首页 →</a></p>
        </div>
    </main>
</body>
</html>
