<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="PostEdit.aspx.cs" Inherits="WebApplication2.PostEdit" MasterPageFile="~/Site.Master" ValidateRequest="false" %>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="page-header">
        <h1><%= PageTitleText %></h1>
        <span class="tag"><%: StatusValue=="Draft" ? "草稿 · 仅自己可见" : "文章编辑" %></span>
    </div>

    <div class="card">
        <%= ErrorHtml %>
        <input type="hidden" name="postId" value="<%= EditingId %>" />

        <div class="form-group">
            <div class="field-heading"><label for="title">标题 *</label><button type="button" class="emoji-trigger" data-emoji-target="title" aria-haspopup="dialog">☺ 表情</button></div>
            <input type="text" id="title" name="title" value="<%: TitleValue %>" maxlength="200" required />
        </div>

        <fieldset class="form-group category-picker" aria-describedby="categoryHint categoryStatus">
            <legend>学科分类 <span class="muted">可多选</span></legend>
            <p id="categoryHint" class="muted">从固定学科目录中选择，发布时至少选择一项；草稿可暂不选择。</p>
            <div class="category-options"><%= CategoryOptionsHtml %></div>
            <p id="categoryStatus" class="field-error" role="alert"></p>
        </fieldset>

        <div class="form-group tag-editor">
            <label for="tagInput">文章标签 <span id="tagCount" class="muted">0 / 10</span></label>
            <input type="hidden" id="tagsJson" name="tagsJson" value="<%: TagsJsonValue %>" />
            <div id="tagChips" class="tag-chips" aria-label="已添加标签"></div>
            <div class="tag-input-row">
                <input type="text" id="tagInput" placeholder="#示例标签A" aria-describedby="tagHint tagStatus" autocomplete="off" />
                <button type="button" id="addTag" class="btn btn-ghost">添加标签</button>
            </div>
            <p id="tagHint" class="muted">输入后点击添加或按回车；开头 # 可省略。最多 10 个，每个 20 个字符，不含空格、逗号或分号。点击 × 移除。</p>
            <p id="tagStatus" class="field-error" role="alert"></p>
        </div>

        <div class="form-group">
            <div class="field-heading"><label for="summary">摘要</label><button type="button" class="emoji-trigger" data-emoji-target="summary" aria-haspopup="dialog">☺ 表情</button></div>
            <textarea id="summary" name="summary" rows="2" maxlength="500"><%: SummaryValue %></textarea>
        </div>

        <div class="editor-grid">
            <div>
                <label for="content">正文（支持 Markdown）*</label>
                <div class="editor-toolbar">
                    <button type="button" onclick="insertMd('# ','')">H1</button>
                    <button type="button" onclick="insertMd('## ','')">H2</button>
                    <button type="button" onclick="insertMd('**','**')">粗体</button>
                    <button type="button" onclick="insertMd('*','*')">斜体</button>
                    <button type="button" onclick="insertMd('`','`')">代码</button>
                    <button type="button" onclick="insertMd('- ','')">列表</button>
                    <button type="button" class="emoji-trigger" data-emoji-target="content" aria-haspopup="dialog">☺ 表情</button>
                </div>
                <div class="upload-row"><label for="articleImage">插入图片（JPEG / PNG，单张最多 2 MB）</label><input type="file" id="articleImage" accept="image/jpeg,image/png" multiple /><span id="uploadStatus" role="status" aria-live="polite"></span></div>
                <p id="imageDropHint" class="image-drop-hint">📷 将图片拖入下方正文框，在放置位置插入；支持一次拖入多张，也可点击上方按钮选择图片。</p>
                <textarea id="content" name="content" rows="16" aria-describedby="imageDropHint uploadStatus" placeholder="开始记录你的知识，也可以将 JPEG / PNG 图片拖到这里…"><%: ContentValue %></textarea>
            </div>
            <div>
                <label>实时预览</label>
                <div id="preview" class="markdown-preview markdown-body"></div>
            </div>
        </div>

        <div class="form-actions" style="margin-top:18px;">
            <button type="submit" name="saveAs" value="Published" class="btn">发布 / 保存发布</button>
            <button type="submit" name="saveAs" value="Draft" class="btn btn-ghost">保存草稿</button>
            <a class="btn btn-ghost" href="PostList.aspx">取消</a>
        </div>
    </div>

    <script src="Scripts/post-taxonomy.js" defer></script>
    <script src="Scripts/editor-images.js" defer></script>
    <script>
        var contentEl = document.getElementById('content');
        var previewEl = document.getElementById('preview');
        function updatePreview() { previewEl.innerHTML = renderMarkdown(contentEl.value); }
        contentEl.addEventListener('input', updatePreview);
        updatePreview();

        // 在光标处插入 Markdown 语法
        function insertMd(before, after) {
            var start = contentEl.selectionStart;
            var end = contentEl.selectionEnd;
            var selected = contentEl.value.substring(start, end);
            contentEl.value =
                contentEl.value.substring(0, start) + before + selected + after + contentEl.value.substring(end);
            contentEl.focus();
            contentEl.selectionStart = start + before.length;
            contentEl.selectionEnd = start + before.length + selected.length;
            updatePreview();
        }
    </script>
</asp:Content>
