/* ============================================================
   我的知识小栈 · 前端公共脚本
   包含：HTML 转义、Markdown 预览、Toast 提示、fetch 封装、
        点赞/取消赞、删除确认、异步表单校验等。
   ============================================================ */

/* ---------- 基础工具 ---------- */

// HTML 转义（防止 XSS）
function escapeHtml(s) {
    return String(s == null ? '' : s)
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;');
}

// 简单 Markdown 转 HTML（与服务端 MarkdownHelper 保持一致）
function renderMarkdown(md) {
    if (!md) return '';
    var html = escapeHtml(md);
    // 代码块
    html = html.replace(/```([\s\S]*?)```/g, function (m, c) {
        return '<pre><code>' + c.replace(/^\r?\n/, '') + '</code></pre>';
    });
    // 标题
    html = html.replace(/^### (.*)$/gm, '<h3>$1</h3>');
    html = html.replace(/^## (.*)$/gm, '<h2>$1</h2>');
    html = html.replace(/^# (.*)$/gm, '<h1>$1</h1>');
    // 粗体 / 斜体
    html = html.replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>');
    html = html.replace(/\*(.+?)\*/g, '<em>$1</em>');
    // 行内代码
    html = html.replace(/`([^`]+)`/g, '<code>$1</code>');
    // 链接
    html = html.replace(/!\[([^\]\r\n]*)\]\(Handlers\/MediaHandler\.ashx\?id=(\d+)\)/g,
        '<img src="Handlers/MediaHandler.ashx?id=$2" alt="$1" loading="lazy" />');
    html = html.replace(/\[([^\]]+)\]\((https?:\/\/[^\)]+)\)/g,
        '<a href="$2" target="_blank" rel="noopener">$1</a>');
    // 无序列表
    html = html.replace(/^\- (.+)$/gm, '• $1');
    // 换行
    html = html.replace(/\r\n/g, '\n').replace(/\n/g, '<br>');
    return html;
}

// 轻量级 Toast 提示
function showToast(msg, type) {
    var toast = document.createElement('div');
    toast.className = 'toast ' + (type || 'info');
    toast.textContent = msg;
    document.body.appendChild(toast);
    requestAnimationFrame(function () { toast.classList.add('show'); });
    setTimeout(function () {
        toast.classList.remove('show');
        setTimeout(function () { toast.remove(); }, 300);
    }, 2600);
}

// fetch 封装：直接返回 JSON
function fetchJSON(url, options) {
    options = options || {};
    options.credentials = 'same-origin';
    if (options.method && options.method.toUpperCase() !== 'GET') {
        options.headers = Object.assign({}, options.headers, { 'X-CSRF-Token': (document.getElementById('csrfToken') || {}).value || '' });
    }
    return fetch(url, options).then(function (r) {
        return r.json().catch(function () { throw new Error('服务器暂未完成请求，请稍后重试'); }).then(function (data) {
            if (!r.ok) throw new Error(data.error || '请求失败');
            return data;
        });
    });
}

/* ---------- 点赞 / 取消赞 ---------- */
function toggleLike(postId) {
    var btn = document.getElementById('likeBtn');
    var action = btn.classList.contains('liked') ? 'unlike' : 'like';
    fetchJSON('Handlers/PostHandler.ashx', {
        method: 'POST',
        body: new URLSearchParams({ action: action, postId: postId })
    }).then(function (res) {
        if (res.error) { showToast(res.error, 'error'); return; }
        document.getElementById('likeCount').textContent = res.likeCount;
        document.getElementById('likeIcon').textContent = res.liked ? '❤️' : '🤍';
        if (res.liked) { btn.classList.add('liked'); } else { btn.classList.remove('liked'); }
        showToast(res.liked ? '已点赞' : '已取消点赞', 'success');
    }).catch(function (e) { showToast(e.message, 'error'); });
}

/* ---------- 删除文章（带确认对话框） ---------- */
function deletePost(postId, redirectUrl) {
    if (!confirm('将文章移入回收站？可以在回收站恢复。')) {
        return;
    }
    fetchJSON('Handlers/PostHandler.ashx', {
        method: 'POST',
        body: new URLSearchParams({ action: 'delete', postId: postId })
    }).then(function (res) {
        if (res.error) { showToast(res.error, 'error'); return; }
        showToast('删除成功', 'success');
        setTimeout(function () { location.href = redirectUrl || 'PostList.aspx'; }, 800);
    }).catch(function (e) { showToast(e.message, 'error'); });
}

/* ---------- 文章搜索（GET 跳转） ---------- */
function doSearch() {
    var kw = document.getElementById('searchInput').value.trim();
    var query = new URLSearchParams({q: kw});
    [['categoryFilter','category'],['tagFilter','tag'],['sortFilter','sort']].forEach(function (pair) { var el=document.getElementById(pair[0]); if(el && el.value)query.set(pair[1],el.value); });
    var navigate=function(){location.href='PostList.aspx?'+query.toString();};
    if(kw) fetchJSON('Handlers/CommunityHandler.ashx',{method:'POST',body:new URLSearchParams({action:'recordSearch',keyword:kw})}).catch(function(){}).then(navigate);
    else navigate();
}
