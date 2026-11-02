using System;

namespace WebApplication2.Models
{
    /// <summary>
    /// 文章/笔记实体类，对应数据库 Posts 表
    /// </summary>
    public class Post
    {
        public int PostId { get; set; }
        public int UserId { get; set; }             // 外键，关联 Users 表
        public string Title { get; set; }
        public string Summary { get; set; }         // 摘要
        public string Content { get; set; }         // 正文（Markdown 文本）
        public string Category { get; set; }        // 关联表读出的学科名称，以 | 分隔；Posts.Category 仅保留兼容快照
        public string Tags { get; set; }            // 标签
        public string Status { get; set; }
        public DateTime? DeletedAt { get; set; }
        public bool IsHidden { get; set; }
        public int LikeCount { get; set; }          // 点赞数
        public int ViewCount { get; set; }          // 浏览量
        public DateTime CreatedAt { get; set; }     // 创建时间
        public DateTime? UpdatedAt { get; set; }    // 最后更新时间

        // 以下为联表查询（INNER JOIN Users）时带出的作者信息
        public string AuthorName { get; set; }      // 作者用户名
        public string AuthorNickname { get; set; }  // 作者昵称
    }
}
