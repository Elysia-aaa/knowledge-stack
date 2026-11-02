<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="PostDetail.aspx.cs" Inherits="WebApplication2.PostDetail" MasterPageFile="~/Site.Master" %>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="card">
        <h1 class="post-detail-title"><%= PostTitleHtml %></h1>
        <div class="post-detail-meta"><%= MetaHtml %></div>

        <div class="post-actions" style="margin-bottom:16px;">
            <% if(PublicVisible) { %>
            <button type="button" id="likeBtn" class="btn btn-like<%= IsLiked ? " liked" : "" %>" onclick="toggleLike(<%= PostId %>)">
                <span id="likeIcon"><%= IsLiked ? "❤️" : "🤍" %></span> <span id="likeCount"><%= LikeCount %></span> 赞
            </button>
            <button type="button" class="btn btn-ghost" id="favoriteBtn">收藏</button>
            <button type="button" class="btn btn-ghost" id="repostBtn">转发</button>
            <button type="button" class="btn btn-ghost" data-report-type="post" data-report-id="<%= PostId %>">举报</button>
            <% } %>
            <%= OwnerActionsHtml %>
        </div>

        <div class="post-content markdown-body" data-track-post="<%= PublicVisible ? PostId : 0 %>"><%= ContentHtml %></div>
    </div>
    <% if(PublicVisible) { %>
    <section class="card" id="commentsRoot" data-post-id="<%= PostId %>">
        <h2>评论与讨论 <span id="commentTotal" class="muted"></span></h2>
        <div id="commentList" aria-live="polite">正在加载评论…</div><div id="commentPager" class="pagination"></div>
        <div id="commentComposer" hidden>
            <p id="replyHint" class="muted"></p><div class="comment-compose-tools"><span class="muted">文字与表情，都是表达。</span><button type="button" class="emoji-trigger" data-emoji-target="commentContent" aria-haspopup="dialog">☺ 表情</button></div><textarea id="commentContent" rows="3" maxlength="1000" placeholder="分享你的想法（支持 emoji，最多 1000 字）" aria-label="评论内容"></textarea>
            <div class="form-actions"><button type="button" id="submitComment" class="btn">发表评论</button><button type="button" id="cancelReply" class="btn btn-ghost" hidden>取消回复</button></div>
        </div><p id="commentLogin" hidden><a href="Login.aspx">登录</a>后可以评论、收藏和转发。</p>
    </section>
    <% } %>
    <p><a href="PostList.aspx">← 返回文章列表</a></p>
</asp:Content>
