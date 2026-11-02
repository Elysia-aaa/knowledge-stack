<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="WebApplication2.Default" MasterPageFile="~/Site.Master" %>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <section class="home-hero">
        <div><p class="eyebrow"><span class="workspace-dot"></span> A SPACE FOR YOUR IDEAS</p><h1>让每一次记录，<br />都有迹可循<span class="hero-period">。</span></h1><p class="hero-description">整理知识，分享想法。搭建属于你的知识小栈。</p><div class="hero-actions"><a class="btn" href="PostEdit.aspx">记录新想法 <svg class="icon" aria-hidden="true"><use href="Content/icons.svg#arrow"></use></svg></a><a class="text-link" href="PostList.aspx">探索全部文章 ↗</a></div></div>
        <div class="hero-index" aria-hidden="true"><span>01 <b>记录</b><i>RECORD</i></span><span>02 <b>连接</b><i>CONNECT</i></span><span>03 <b>成长</b><i>GROW</i></span><svg class="hero-stack" viewBox="0 0 100 75" fill="none"><path d="M50 5 90 26 50 47 10 26 50 5Zm-40 34 40 21 40-21M10 52l40 21 40-21" stroke="currentColor" stroke-width="1.3" /></svg></div>
    </section>
    <div class="dashboard-grid">
        <section>
            <div class="page-header">
                <div><p class="eyebrow">THE LATEST</p><h2>最新文章</h2></div>
                <a class="text-link" href="PostList.aspx">查看全部 ↗</a>
            </div>
            <%= EmptyHtml %>
            <div class="post-list-grid"><%= PostsHtml %></div>
        </section>
        <aside class="home-aside"><%= UserCardHtml %><div class="card discovery-card"><p class="eyebrow">EXPLORE</p><h2>下一条灵感，<br />在这里。</h2><a class="discovery-link" href="Community.aspx?view=rankings">发现热榜与标签 <span>↗</span></a><a class="discovery-link" href="Community.aspx?view=feed">看看关注动态 <span>↗</span></a></div></aside>
    </div>
</asp:Content>
