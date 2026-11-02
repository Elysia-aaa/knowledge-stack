using System;

namespace WebApplication2.Models
{
    /// <summary>
    /// 用户实体类，对应数据库 Users 表
    /// </summary>
    public class User
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }   // 加密后的密码，不存明文
        public string Salt { get; set; }            // 每个用户独立的随机盐
        public string Nickname { get; set; }        // 昵称（可选）
        public string Bio { get; set; }             // 个人简介（可选）
        public string Avatar { get; set; }          // 头像（可选）
        public DateTime CreatedAt { get; set; }     // 注册时间
    }
}
