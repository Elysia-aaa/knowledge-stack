<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="MyPosts.aspx.cs" Inherits="WebApplication2.MyPosts" MasterPageFile="~/Site.Master" %>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="quick-links"><a href="Community.aspx?view=drafts">草稿箱</a><a href="Community.aspx?view=trash">回收站</a><a href="Community.aspx?view=favorites">我的收藏</a><a href="Community.aspx?view=profile">个人主页</a></div>
    <div class="page-header">
        <h1>我的文章</h1>
        <a class="btn btn-sm" href="PostEdit.aspx">写文章 ＋</a>
    </div>
    <%= EmptyHtml %>
    <div class="post-list-grid"><%= PostsHtml %></div>
</asp:Content>
