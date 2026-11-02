<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Login.aspx.cs" Inherits="WebApplication2.Login" MasterPageFile="~/Site.Master" %>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="card form-card">
        <p class="eyebrow">WELCOME BACK</p><h1>欢迎回到小栈。</h1>
        <p class="form-hint">登录后可以发布文章、点赞、管理自己的笔记。</p>
        <%= ErrorHtml %>
        <div class="form-group">
            <label for="account">用户名 / 邮箱</label>
            <input type="text" id="account" name="account" value="<%: AccountValue %>" required />
        </div>
        <div class="form-group">
            <label for="password">密码</label>
            <input type="password" id="password" name="password" required />
        </div>
        <div class="form-group">
            <label><input type="checkbox" name="remember" /> 记住我（7 天内免输入用户名）</label>
        </div>
        <div class="form-actions">
            <button type="submit" class="btn">登录</button>
            <span>还没有账号？<a href="Register.aspx">去注册</a></span>
        </div>
    </div>
</asp:Content>
