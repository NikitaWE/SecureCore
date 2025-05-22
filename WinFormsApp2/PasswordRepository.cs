using System;
using System.Collections.Generic;
using System.Data.SQLite;

namespace PasswordManager
{
    public static class PasswordRepository
    {
        public static List<PasswordEntry> GetAll(int userId, string password)
        {
            var list = new List<PasswordEntry>();
            using var conn = new SQLiteConnection($"Data Source={Database.PasswordsDb};");
            conn.Open();
            var cmd = new SQLiteCommand("SELECT * FROM Passwords WHERE UserId = @uid", conn);
            cmd.Parameters.AddWithValue("@uid", userId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new PasswordEntry
                {
                    Id = reader.GetInt32(0),
                    Title = reader.GetString(2),
                    Username = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    Password = CryptoService.Decrypt(reader.GetString(4), password),
                    Description = reader.IsDBNull(5) ? "" : reader.GetString(5),
                    CreatedDate = DateTime.Parse(reader.GetString(6)),
                    ModifiedDate = DateTime.Parse(reader.GetString(7))
                });
            }
            return list;
        }

        public static void Add(int userId, PasswordEntry entry, string password)
        {
            using var conn = new SQLiteConnection($"Data Source={Database.PasswordsDb};");
            conn.Open();
            var cmd = new SQLiteCommand(@"
                INSERT INTO Passwords (UserId, Title, Login, EncryptedPassword, Description, CreatedAt, ModifiedAt)
                VALUES (@uid, @title, @login, @pass, @desc, @created, @modified)", conn);
            cmd.Parameters.AddWithValue("@uid", userId);
            cmd.Parameters.AddWithValue("@title", entry.Title);
            cmd.Parameters.AddWithValue("@login", entry.Username);
            cmd.Parameters.AddWithValue("@pass", CryptoService.Encrypt(entry.Password, password));
            cmd.Parameters.AddWithValue("@desc", entry.Description);
            cmd.Parameters.AddWithValue("@created", entry.CreatedDate.ToString("s"));
            cmd.Parameters.AddWithValue("@modified", entry.ModifiedDate.ToString("s"));
            cmd.ExecuteNonQuery();
        }
    }
}
