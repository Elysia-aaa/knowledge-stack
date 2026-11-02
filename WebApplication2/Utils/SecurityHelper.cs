using System;
using System.Security.Cryptography;
using System.Text;

namespace WebApplication2.Utils
{
    /// <summary>
    /// 密码加密工具类：采用 SHA256 + 随机盐（Salt）的方式存储密码，
    /// 数据库里绝不保存明文密码。
    /// </summary>
    public static class SecurityHelper
    {
        /// <summary>生成 16 字节的随机盐，返回 Base64 字符串</summary>
        public static string GenerateSalt()
        {
            byte[] bytes = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            return Convert.ToBase64String(bytes);
        }

        /// <summary>对“盐 + 明文密码”做 SHA256 哈希，返回小写十六进制字符串</summary>
        public static string HashPassword(string password, string salt)
        {
            using(var derive=new Rfc2898DeriveBytes(password,Convert.FromBase64String(salt),100000,HashAlgorithmName.SHA256))
                return "pbkdf2$100000$"+Convert.ToBase64String(derive.GetBytes(32));
        }

        /// <summary>校验输入的明文密码是否与数据库中的哈希一致</summary>
        public static bool VerifyPassword(string password, string salt, string hash)
        {
            string computed;
            if(hash.StartsWith("pbkdf2$",StringComparison.Ordinal))computed=HashPassword(password,salt);
            else using(var sha=SHA256.Create())computed=BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(salt+password))).Replace("-","").ToLowerInvariant();
            if(computed.Length!=hash.Length)return false;
            int difference=0;for(int i=0;i<computed.Length;i++)difference|=computed[i]^hash[i];return difference==0;
        }
    }
}
