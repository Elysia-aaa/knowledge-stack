<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="PostList.aspx.cs" Inherits="WebApplication2.PostList" MasterPageFile="~/Site.Master" %>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="page-header">
        <div><p class="eyebrow">THE KNOWLEDGE LIBRARY</p><h1>全部文章</h1></div>
        <div class="search-bar">
            <input type="search" id="searchInput" maxlength="100" placeholder="搜索标题、正文或作者" value="<%: KeywordValue %>" onkeydown="if(event.key==='Enter'){event.preventDefault();doSearch();}" />
            <button type="button" class="btn btn-sm" onclick="doSearch()">搜索</button>
        </div>
    </div>
    <div class="card filter-bar">
        <label>分类 <select id="categoryFilter" onchange="doSearch()"><option value="">全部分类</option><%= CategoryOptions %></select></label>
        <label>标签 <select id="tagFilter" onchange="doSearch()"><option value="">全部标签</option><%= TagOptions %></select></label>
        <label>排序 <select id="sortFilter" data-selected="<%: SortValue %>" onchange="doSearch()"><option value="newest">最新发布</option><option value="oldest">最早发布</option><option value="likes">最多点赞</option><option value="views">最多浏览</option></select></label>
        <a href="PostList.aspx">清除条件</a><span class="muted">共 <%= TotalCount %> 篇</span>
    </div>
    <%= EmptyHtml %>
    <div class="post-list-grid"><%= PostsHtml %></div>
    <%= PagerHtml %>
</asp:Content>
