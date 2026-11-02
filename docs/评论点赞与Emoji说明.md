# 评论点赞与 Emoji

## 功能

- 登录用户可为评论及一级回复点赞、取消点赞；同一账号对同一评论最多一条点赞记录。数量和本人状态保存在数据库，点击后局部更新，刷新仍保留。
- 访客可看数量，点击时提示登录。已隐藏、已删除的评论，以及草稿、回收站、隐藏文章下的评论，禁止继续点赞或取消。
- 新点赞通知评论作者；取消后重新点赞不会重复通知。评论点赞单独统计，不改变原有文章点赞数和热榜计算规则。
- 标题、摘要、正文及评论/回复均支持 emoji。点击输入区旁的“表情”按钮，使用六类共 100 个快捷表情或搜索关键词；在光标处插入，也可替换选中文本。仍可用系统表情键盘或粘贴其他 Unicode emoji。
- 正文插入后同步 Markdown 预览；标题、摘要、评论遵循原有长度限制，不能完整插入时给出提醒，不截断表情。支持 Escape / 关闭按钮及点击面板外关闭。
- 黑白主题共用组件。手机采用底部面板，已检查 390px 和 320px 宽度无横向溢出。表情外观由系统字体决定。

## 数据与代码

- `database/003_comment_likes.sql`：增量创建 CommentLikes，联合主键 (CommentId,UserId) 防重复；有用户、评论外键，评论彻底删除时级联清理。可重复执行。
- `DAL/SchemaMigration.cs`：初始化时嵌入并执行新脚本；新环境依次执行 000、001、002、003，或使用本机 Setup 页面。
- `Services/CommunityService.cs`：通过自定义 CommunityDAL 和事务处理 `likeComment` / `unlikeComment`，验证公开状态、登录和防伪令牌；返回计数与本人状态。
- `Scripts/community.js`：评论点赞状态、局部更新、通知文案。`PostDetail.aspx`：评论 emoji 入口。
- `Scripts/emoji-picker.js`、`PostEdit.aspx`、`Content/site.css`：复用选择器、搜索、光标处理、黑白主题和移动端布局。资源均在本地，不需新增依赖或外部服务。
- 文章、评论继续通过 UTF-8 与 SQL Server NVARCHAR 保存。评论防重复按原始 Unicode 字节比较，避免不同 emoji 被数据库排序规则误判相同。
- 增量升级前备份：`.vs/KnowledgeStack-before-comment-likes-20261019.bak`。

## 验收

MSBuild Debug 构建成功；自动验收 **104 项通过**，结果见 `local-acceptance-results.json`。新增范围覆盖重复/取消点赞、其他用户状态、访客与防伪保护、回复点赞、隐藏/删除/草稿限制、通知去重、级联清理及 emoji 保存/再次编辑。

组合表情 `👨‍💻`、肤色 `👍🏽`、家庭 `👨‍👩‍👧‍👦`、爱心 `❤️` 均通过文章及评论的数据库/JSON 读写；不同 emoji-only 评论可分别提交，完全相同内容仍执行防重复。

浏览器另外实测：正文 `前后` 在中间插入后成为 `前😊后`；表情搜索、200 字符标题上限、Escape 关闭；评论点赞/取消/刷新记忆、回复点赞、emoji 评论真实发布、黑色主题及手机面板。

浏览器验收使用专门的临时账号和文章，已清理其账号、文章、评论、点赞和通知；验收前后的原有账号数均为 2，文章数均为 3。可用 `python tests/comment_emoji_ui_fixture.py create` 建立临时界面验收数据，用完执行 `cleanup`。

截图：`screenshots/article-emoji-picker.png`、`comment-likes-desktop.png`、`comment-emoji-desktop.png`、`comment-emoji-mobile-dark.png`。截图内文章为已清理的验收材料。
