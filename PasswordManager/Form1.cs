using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using System.Linq;

namespace PasswordManager
{
    public partial class Form1 : Form
    {
        private Dictionary<string, PasswordEntry> passwordEntries = new Dictionary<string, PasswordEntry>();
        private string currentUser = "";
        private string masterPasswordHash = "";

        public Form1()
        {
            InitializeComponent();
            InitializeDataGridView();
            SetControlsState(false);
        }

        private void InitializeDataGridView()
        {
            dataGridView1.Columns.Add("Description", "Описание");
            dataGridView1.Columns.Add("Password", "Пароль");
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridView1.ReadOnly = true;
        }

        private void SetControlsState(bool loggedIn)
        {
            btnAdd.Enabled = loggedIn;
            btnGenerate.Enabled = loggedIn;
            btnExport.Enabled = loggedIn;
            btnImport.Enabled = loggedIn;
            txtDescription.Enabled = loggedIn;
            txtPassword.Enabled = loggedIn;
            dataGridView1.Enabled = loggedIn;

            btnLogin.Enabled = !loggedIn;
            txtLogin.Enabled = !loggedIn;
            txtMasterPassword.Enabled = !loggedIn;
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtLogin.Text) || string.IsNullOrWhiteSpace(txtMasterPassword.Text))
            {
                MessageBox.Show("Введите логин и мастер-пароль");
                return;
            }

            currentUser = txtLogin.Text;
            masterPasswordHash = HashPassword(txtMasterPassword.Text);
            SetControlsState(true);
            MessageBox.Show("Вход выполнен успешно");
        }

        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtDescription.Text) || string.IsNullOrWhiteSpace(txtPassword.Text))
            {
                MessageBox.Show("Введите описание и пароль");
                return;
            }

            string description = txtDescription.Text;
            string password = EncryptPassword(txtPassword.Text);

            if (passwordEntries.ContainsKey(description))
            {
                MessageBox.Show("Запись с таким описанием уже существует");
                return;
            }

            passwordEntries.Add(description, new PasswordEntry(description, password));
            UpdateDataGridView();
            ClearInputFields();
        }

        private string EncryptPassword(string password)
        {
            byte[] encrypted;
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Encoding.UTF8.GetBytes(masterPasswordHash.Substring(0, 32));
                aesAlg.IV = new byte[16]; // Простая реализация, в реальном приложении нужно генерировать IV для каждой записи

                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(password);
                        }
                        encrypted = msEncrypt.ToArray();
                    }
                }
            }
            return Convert.ToBase64String(encrypted);
        }

        private string DecryptPassword(string encryptedPassword)
        {
            string plaintext = null;
            byte[] cipherText = Convert.FromBase64String(encryptedPassword);

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Encoding.UTF8.GetBytes(masterPasswordHash.Substring(0, 32));
                aesAlg.IV = new byte[16];

                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {
                            plaintext = srDecrypt.ReadToEnd();
                        }
                    }
                }
            }
            return plaintext;
        }

        private void btnGenerate_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtDescription.Text))
            {
                MessageBox.Show("Введите описание для пароля");
                return;
            }

            string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()";
            StringBuilder password = new StringBuilder();
            Random random = new Random();

            for (int i = 0; i < 12; i++)
            {
                password.Append(chars[random.Next(chars.Length)]);
            }

            txtPassword.Text = password.ToString();
        }

        private void UpdateDataGridView()
        {
            dataGridView1.Rows.Clear();
            foreach (var entry in passwordEntries.Values)
            {
                dataGridView1.Rows.Add(entry.Description, "••••••••");
            }
        }

        private void ClearInputFields()
        {
            txtDescription.Text = "";
            txtPassword.Text = "";
        }

        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                string description = dataGridView1.Rows[e.RowIndex].Cells["Description"].Value.ToString();
                if (passwordEntries.TryGetValue(description, out PasswordEntry entry))
                {
                    txtDescription.Text = entry.Description;
                    txtPassword.Text = DecryptPassword(entry.EncryptedPassword);
                }
            }
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Файлы паролей (*.pwd)|*.pwd";
            saveFileDialog.Title = "Экспорт паролей";

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (var entry in passwordEntries.Values)
                    {
                        sb.AppendLine($"{entry.Description}|{entry.EncryptedPassword}");
                    }

                    string encryptedData = EncryptPassword(sb.ToString());
                    File.WriteAllText(saveFileDialog.FileName, encryptedData);
                    MessageBox.Show("Данные успешно экспортированы");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при экспорте: {ex.Message}");
                }
            }
        }

        private void btnImport_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Файлы паролей (*.pwd)|*.pwd";
            openFileDialog.Title = "Импорт паролей";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    string encryptedData = File.ReadAllText(openFileDialog.FileName);
                    string decryptedData = DecryptPassword(encryptedData);

                    passwordEntries.Clear();
                    foreach (string line in decryptedData.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        string[] parts = line.Split('|');
                        if (parts.Length == 2)
                        {
                            passwordEntries.Add(parts[0], new PasswordEntry(parts[0], parts[1]));
                        }
                    }

                    UpdateDataGridView();
                    MessageBox.Show("Данные успешно импортированы");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при импорте: {ex.Message}");
                }
            }
        }
    }

    public class PasswordEntry
    {
        public string Description { get; }
        public string EncryptedPassword { get; }

        public PasswordEntry(string description, string encryptedPassword)
        {
            Description = description;
            EncryptedPassword = encryptedPassword;
        }
    }
}