using System;

namespace PasswordManager
{
    public class PasswordEntry
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Description { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }

        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(Title) && !string.IsNullOrWhiteSpace(Password);
        }

        public void UpdateModifiedDate()
        {
            ModifiedDate = DateTime.Now;
        }
    }
}
