using System;
using System.Data.SQLite;
using System.Security.Cryptography;
using System.Text;

namespace PasswordManager
{
    public static class UserRepository
    {
        public static void Register(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || username.Length < 4)
                throw new ArgumentException("Имя пользователя должно содержать минимум 4 символа");

            if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
                throw new ArgumentException("Пароль должен содержать минимум 6 символов");

            var salt = GenerateSalt();
            var hash = HashPassword(password, salt);

            using var conn = new SQLiteConnection($"Data Source={Database.UsersDb};");
            conn.Open();
            var cmd = new SQLiteCommand("INSERT INTO Users (Username, Salt, PasswordHash) VALUES (@u, @s, @h)", conn);
            cmd.Parameters.AddWithValue("@u", username);
            cmd.Parameters.AddWithValue("@s", Convert.ToBase64String(salt));
            cmd.Parameters.AddWithValue("@h", Convert.ToBase64String(hash));
            cmd.ExecuteNonQuery();
        }

        public static int? ValidateUser(string username, string password)
        {
            using var conn = new SQLiteConnection($"Data Source={Database.UsersDb};");
            conn.Open();
            var cmd = new SQLiteCommand("SELECT Id, Salt, PasswordHash FROM Users WHERE Username = @u", conn);
            cmd.Parameters.AddWithValue("@u", username);
            using var reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                var id = reader.GetInt32(0);
                var salt = Convert.FromBase64String(reader.GetString(1));
                var storedHash = Convert.FromBase64String(reader.GetString(2));
                var computedHash = HashPassword(password, salt);

                if (CompareByteArrays(storedHash, computedHash))
                    return id;
            }

            return null;
        }

        private static byte[] GenerateSalt()
        {
            using var rng = RandomNumberGenerator.Create();
            var salt = new byte[32];
            rng.GetBytes(salt);
            return salt;
        }

        private static byte[] HashPassword(string password, byte[] salt)
        {
            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256);
            return pbkdf2.GetBytes(32);
        }

        private static bool CompareByteArrays(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i]) return false;
            }
            return true;
        }
    }
}
