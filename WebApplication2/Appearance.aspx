<%@ Page Language="C#" MasterPageFile="~/Site.Master" %>
<asp:Content ID="AppearanceContent" ContentPlaceHolderID="MainContent" runat="server">
 <div class="studio-page appearance-page" id="appearanceRoot">
  <header class="studio-heading"><div><p class="eyebrow">MAKE IT YOURS</p><h1>你的界面，你的色彩<span class="title-dot">.</span></h1><p class="muted">从一套喜欢的主题开始，让记录有自己的样子。</p></div><span class="studio-pill">全站个性化</span></header>
  <section aria-labelledby="presetHeading"><div class="section-title"><h2 id="presetHeading">精选主题</h2><span class="muted">六种气质，一键切换</span></div><div id="themePresets" class="theme-presets"></div></section>
  <section class="appearance-workspace">
   <div class="card color-panel"><div class="section-title"><h2>自定义配色</h2><button type="button" id="useCustom" class="btn btn-sm btn-ghost">载入我的自定义</button></div><p class="muted">在任一预设上调整颜色，保存为专属于你的一套主题。</p><div id="colorFields" class="color-fields"></div></div>
   <aside class="theme-preview-column"><div class="card theme-live-preview"><p class="eyebrow">LIVE PREVIEW</p><div class="preview-orbit" aria-hidden="true"><i></i><i></i><i></i></div><span class="tag">灵感手记</span><h2>知识的颜色，<br />由你定义。</h2><p class="muted">这是文章、按钮、标签与代码的即时预览。调整也会立即应用到当前页面。</p><pre><code>const idea = "让知识生长";</code></pre><div class="form-actions"><button type="button" class="btn">主要按钮</button><button type="button" class="btn btn-ghost">次要按钮</button></div></div><div class="card contrast-panel"><h3>让文字保持清楚</h3><p id="contrastStatus" role="status"></p><button type="button" class="btn btn-sm btn-ghost" id="improveContrast">一键改善文字对比</button></div></aside>
  </section>
  <div class="appearance-savebar"><div><strong id="appearanceStatus" role="status">正在读取设置…</strong><p class="muted" id="appearanceHint">配色仅影响你自己的浏览界面。</p></div><div class="form-actions"><button type="button" id="resetAppearance" class="btn btn-ghost">恢复默认</button><button type="button" id="cancelAppearance" class="btn btn-ghost">取消修改</button><button type="button" id="saveAppearance" class="btn">保存主题</button></div></div>
 </div>
 <script src="Scripts/appearance.js?v=20261026"></script>
</asp:Content>
