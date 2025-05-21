using System;
using System.Xml.Serialization;

namespace PasswordManager
{
    [Serializable]
    public class PasswordEntry
    {
        public string Title { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Description { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime ModifiedDate { get; set; } = DateTime.Now;
        public string Category { get; set; } = "General";
        public string Url { get; set; }

        public PasswordEntry()
        {
            // Конструктор по умолчанию для сериализации
        }

        public PasswordEntry(string title, string username, string password, string description = "")
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Название не может быть пустым", nameof(title));

            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Пароль не может быть пустым", nameof(password));

            Title = title.Trim();
            Username = username?.Trim();
            Password = password;
            Description = description?.Trim();
        }

        public override string ToString()
        {
            return $"{Title} ({Username})";
        }

        public void UpdateModifiedDate()
        {
            ModifiedDate = DateTime.Now;
        }

        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(Title) &&
                   !string.IsNullOrWhiteSpace(Password);
        }
    }
}