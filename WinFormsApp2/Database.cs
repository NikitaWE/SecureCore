using System;
using System.Data.SQLite;
using System.IO;

namespace PasswordManager
{
    public static class Database
    {
        public static string UsersDb = "Users.db";
        public static string PasswordsDb = "Passwords.db";

        public static void Initialize()
        {
            if (!File.Exists(UsersDb))
            {
                SQLiteConnection.CreateFile(UsersDb);
                using var conn = new SQLiteConnection($"Data Source={UsersDb};");
                conn.Open();
                var cmd = new SQLiteCommand(@"
                    CREATE TABLE Users (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Username TEXT UNIQUE NOT NULL,
                        Salt TEXT NOT NULL,
                        PasswordHash TEXT NOT NULL
                    );", conn);
                cmd.ExecuteNonQuery();
            }

            if (!File.Exists(PasswordsDb))
            {
                SQLiteConnection.CreateFile(PasswordsDb);
                using var conn = new SQLiteConnection($"Data Source={PasswordsDb};");
                conn.Open();
                var cmd = new SQLiteCommand(@"
                    CREATE TABLE Passwords (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        UserId INTEGER NOT NULL,
                        Title TEXT NOT NULL,
                        Login TEXT,
                        EncryptedPassword TEXT NOT NULL,
                        Description TEXT,
                        CreatedAt TEXT,
                        ModifiedAt TEXT,
                        FOREIGN KEY(UserId) REFERENCES Users(Id)
                    );", conn);
                cmd.ExecuteNonQuery();
            }
        }
    }
}