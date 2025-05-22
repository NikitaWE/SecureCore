using System;
using System.Security.Cryptography;
using System.Text;

namespace PasswordManager
{
    public static class CryptoService
    {
        public static string Encrypt(string text, string password)
        {
            using var aes = Aes.Create();
            var key = new Rfc2898DeriveBytes(password, Encoding.UTF8.GetBytes("MySalt123"), 10000);
            aes.Key = key.GetBytes(32);
            aes.IV = key.GetBytes(16);

            using var encryptor = aes.CreateEncryptor();
            var data = Encoding.UTF8.GetBytes(text);
            var cipher = encryptor.TransformFinalBlock(data, 0, data.Length);
            return Convert.ToBase64String(cipher);
        }

        public static string Decrypt(string cipherText, string password)
        {
            using var aes = Aes.Create();
            var key = new Rfc2898DeriveBytes(password, Encoding.UTF8.GetBytes("MySalt123"), 10000);
            aes.Key = key.GetBytes(32);
            aes.IV = key.GetBytes(16);

            using var decryptor = aes.CreateDecryptor();
            var cipher = Convert.FromBase64String(cipherText);
            var plain = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
            return Encoding.UTF8.GetString(plain);
        }
    }
}
