using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using System.Xml;

namespace PasswordManager
{
    public partial class MainForm : Form
    {
        private string _username;
        private string _password;
        private string _dataFile;
        private string _usersFile = "users.dat";
        private BindingSource _bindingSource = new BindingSource();
        private bool _isClosing = false;

        public MainForm(string username, string password)
        {
            InitializeComponent();
            _username = username;
            _password = password;
            _dataFile = Path.Combine("UserData", username, "data.enc");

            try
            {
                var entries = LoadEntries();
                if (entries == null)
                {
                    this.DialogResult = DialogResult.Retry;
                    this.Close();
                    return;
                }

                _bindingSource.DataSource = entries;
                dataGridView.DataSource = _bindingSource;
                dataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
                _bindingSource.DataSource = new PasswordEntryList();
            }
        }

        private byte[] GetUserSalt()
        {
            try
            {
                if (!File.Exists(_usersFile)) return null;

                foreach (var line in File.ReadAllLines(_usersFile))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var parts = line.Split('|');
                    if (parts.Length >= 3 && parts[0] == _username)
                    {
                        return Convert.FromBase64String(parts[1]);
                    }
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        private PasswordEntryList LoadEntries()
        {
            try
            {
                if (!File.Exists(_dataFile))
                {
                    return CreateAndSaveEmptyList();
                }

                var encryptedData = File.ReadAllBytes(_dataFile);

                if (encryptedData.Length == 0)
                {
                    return CreateAndSaveEmptyList();
                }

                var salt = GetUserSalt();
                if (salt == null)
                {
                    MessageBox.Show("Не удалось получить данные пользователя. Пожалуйста, войдите снова.",
                                  "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return null;
                }

                try
                {
                    var decryptedData = DecryptData(encryptedData, _password, salt);
                    var list = PasswordEntryList.Deserialize(decryptedData);
                    return list ?? CreateAndSaveEmptyList();
                }
                catch (CryptographicException ex)
                {
                    string errorMsg = "Не удалось расшифровать данные. Возможные причины:\n" +
                                    "1. Неверный пароль\n" +
                                    "2. Файл данных поврежден\n" +
                                    $"Подробности: {ex.Message}";

                    var result = MessageBox.Show(errorMsg + "\n\nХотите попробовать ввести пароль снова?",
                                              "Ошибка шифрования",
                                              MessageBoxButtons.YesNo,
                                              MessageBoxIcon.Warning);

                    if (result == DialogResult.Yes)
                    {
                        this.Close();
                        return null;
                    }
                    else
                    {
                        try { File.Delete(_dataFile); } catch { }
                        return CreateAndSaveEmptyList();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Создан новый файл данных. Ошибка: {ex.Message}",
                              "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return CreateAndSaveEmptyList();
            }
        }

        private PasswordEntryList CreateAndSaveEmptyList()
        {
            var emptyList = new PasswordEntryList();
            SaveEntries(emptyList);
            return emptyList;
        }

        private void SaveEntries(PasswordEntryList entries)
        {
            try
            {
                if (entries == null)
                {
                    throw new ArgumentNullException(nameof(entries), "Список записей не может быть null");
                }

                var data = entries.Serialize();
                var salt = GetUserSalt();
                if (salt == null)
                {
                    throw new CryptographicException("Не удалось получить соль для пользователя");
                }

                var encryptedData = EncryptData(data, _password, salt);

                Directory.CreateDirectory(Path.GetDirectoryName(_dataFile));

                var tempFile = _dataFile + ".tmp";
                File.WriteAllBytes(tempFile, encryptedData);

                if (File.Exists(_dataFile))
                {
                    File.Replace(tempFile, _dataFile, _dataFile + ".bak");
                }
                else
                {
                    File.Move(tempFile, _dataFile);
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Не удалось сохранить данные: {ex.Message}", ex);
            }
        }

        private byte[] EncryptData(byte[] data, string password, byte[] salt)
        {
            try
            {
                if (data == null || data.Length == 0)
                {
                    throw new ArgumentException("Данные для шифрования не могут быть пустыми", nameof(data));
                }

                if (string.IsNullOrEmpty(password))
                {
                    throw new ArgumentException("Пароль не может быть пустым", nameof(password));
                }

                if (salt == null || salt.Length == 0)
                {
                    throw new ArgumentException("Соль не может быть пустой", nameof(salt));
                }

                using (var aes = Aes.Create())
                {
                    var key = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256);
                    aes.Key = key.GetBytes(32);
                    aes.IV = key.GetBytes(16);

                    using (var ms = new MemoryStream())
                    {
                        using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                        {
                            cs.Write(data, 0, data.Length);
                            cs.FlushFinalBlock();
                            return ms.ToArray();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new CryptographicException($"Ошибка шифрования данных: {ex.Message}", ex);
            }
        }

        private byte[] DecryptData(byte[] encryptedData, string password, byte[] salt)
        {
            try
            {
                if (encryptedData == null || encryptedData.Length == 0)
                {
                    throw new ArgumentException("Данные для дешифрования не могут быть пустыми", nameof(encryptedData));
                }

                if (string.IsNullOrEmpty(password))
                {
                    throw new ArgumentException("Пароль не может быть пустым", nameof(password));
                }

                if (salt == null || salt.Length == 0)
                {
                    throw new ArgumentException("Соль не может быть пустой", nameof(salt));
                }

                using (var aes = Aes.Create())
                {
                    var key = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256);
                    aes.Key = key.GetBytes(32);
                    aes.IV = key.GetBytes(16);

                    using (var ms = new MemoryStream())
                    {
                        using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
                        {
                            cs.Write(encryptedData, 0, encryptedData.Length);
                            cs.FlushFinalBlock();
                            return ms.ToArray();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new CryptographicException($"Ошибка дешифрования данных: {ex.Message}", ex);
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            try
            {
                if (!ValidateInput()) return;

                var entry = new PasswordEntry
                {
                    Title = txtTitle.Text.Trim(),
                    Username = txtUsername.Text.Trim(),
                    Password = txtEntryPassword.Text,
                    Description = txtDescription.Text.Trim(),
                    CreatedDate = DateTime.Now,
                    ModifiedDate = DateTime.Now
                };

                var entries = (PasswordEntryList)_bindingSource.DataSource;
                entries.AddEntry(entry);
                _bindingSource.ResetBindings(false);

                SaveEntries(entries);
                ClearFields();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении записи: {ex.Message}", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(txtTitle.Text))
            {
                MessageBox.Show("Пожалуйста, укажите название записи", "Не заполнено поле",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtTitle.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtEntryPassword.Text))
            {
                MessageBox.Show("Пароль не может быть пустым", "Не заполнено поле",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtEntryPassword.Focus();
                return false;
            }

            return true;
        }

        private void ClearFields()
        {
            txtTitle.Clear();
            txtUsername.Clear();
            txtEntryPassword.Clear();
            txtDescription.Clear();
            txtTitle.Focus();
        }

        private void btnGeneratePassword_Click(object sender, EventArgs e)
        {
            try
            {
                txtEntryPassword.Text = GeneratePassword(12);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка генерации пароля: {ex.Message}", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string GeneratePassword(int length)
        {
            const string validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%^&*()";
            var res = new StringBuilder();

            using (var rng = RandomNumberGenerator.Create())
            {
                var uintBuffer = new byte[sizeof(uint)];

                while (length-- > 0)
                {
                    rng.GetBytes(uintBuffer);
                    var num = BitConverter.ToUInt32(uintBuffer, 0);
                    res.Append(validChars[(int)(num % (uint)validChars.Length)]);
                }
            }

            return res.ToString();
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Filter = "XML файлы (*.xml)|*.xml",
                    Title = "Экспорт данных"
                };

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    var entries = (PasswordEntryList)_bindingSource.DataSource;
                    var xml = entries.SerializeToXml();
                    var salt = GetUserSalt();
                    if (salt == null)
                    {
                        throw new CryptographicException("Не удалось получить соль для пользователя");
                    }
                    var encryptedXml = EncryptData(Encoding.UTF8.GetBytes(xml), _password, salt);
                    File.WriteAllBytes(saveDialog.FileName, encryptedXml);

                    MessageBox.Show("Данные успешно экспортированы", "Успех",
                                  MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при экспорте данных: {ex.Message}", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnImport_Click(object sender, EventArgs e)
        {
            try
            {
                var openDialog = new OpenFileDialog
                {
                    Filter = "XML файлы (*.xml)|*.xml",
                    Title = "Импорт данных"
                };

                if (openDialog.ShowDialog() == DialogResult.OK)
                {
                    var encryptedData = File.ReadAllBytes(openDialog.FileName);
                    var salt = GetUserSalt();
                    if (salt == null)
                    {
                        throw new CryptographicException("Не удалось получить соль для пользователя");
                    }
                    var decryptedData = DecryptData(encryptedData, _password, salt);
                    var xml = Encoding.UTF8.GetString(decryptedData);
                    var entries = PasswordEntryList.DeserializeFromXml(xml);

                    _bindingSource.DataSource = entries;
                    dataGridView.DataSource = _bindingSource;
                    SaveEntries(entries);

                    MessageBox.Show("Данные успешно импортированы", "Успех",
                                  MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (CryptographicException ex)
            {
                MessageBox.Show($"Не удалось расшифровать файл: {ex.Message}\nВозможно, он был создан другим пользователем.",
                              "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при импорте данных: {ex.Message}", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_isClosing) return;
            _isClosing = true;

            try
            {
                var result = MessageBox.Show("Сохранить изменения перед выходом?", "Подтверждение",
                                          MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    SaveEntries((PasswordEntryList)_bindingSource.DataSource);
                }
                else if (result == DialogResult.Cancel)
                {
                    e.Cancel = true;
                    _isClosing = false;
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}\nПриложение будет закрыто.",
                              "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Application.Exit();
                Environment.Exit(0);
            }
        }
    }
}