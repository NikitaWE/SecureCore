using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace PasswordManager
{
    public partial class MainForm : Form
    {
        private readonly int _userId;
        private readonly string _username;
        private readonly string _password;
        private readonly BindingSource _bindingSource = new BindingSource();
        private bool _isClosing = false;

        public MainForm(int userId, string username, string password)
        {
            InitializeComponent();
            _userId = userId;
            _username = username;
            _password = password;

            try
            {
                var entries = PasswordRepository.GetAll(_userId, _password);
                _bindingSource.DataSource = entries;
                dataGridView.DataSource = _bindingSource;
                dataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

                PasswordRepository.Add(_userId, entry, _password);

                // Обновляем интерфейс
                var updatedList = PasswordRepository.GetAll(_userId, _password);
                _bindingSource.DataSource = updatedList;
                dataGridView.DataSource = _bindingSource;
                _bindingSource.ResetBindings(false);

                ClearFields();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении записи: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ClearFields()
        {
            txtTitle.Clear();
            txtUsername.Clear();
            txtEntryPassword.Clear();
            txtDescription.Clear();
            txtTitle.Focus();
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(txtTitle.Text))
            {
                MessageBox.Show("Пожалуйста, укажите название записи", "Не заполнено поле", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtTitle.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtEntryPassword.Text))
            {
                MessageBox.Show("Пароль не может быть пустым", "Не заполнено поле", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtEntryPassword.Focus();
                return false;
            }

            return true;
        }

        private void btnGeneratePassword_Click(object sender, EventArgs e)
        {
            try
            {
                txtEntryPassword.Text = GeneratePassword(12);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка генерации пароля: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    var entries = (List<PasswordEntry>)_bindingSource.DataSource;
                    var list = new PasswordEntryList(entries);
                    var xml = list.SerializeToXml();
                    var encryptedXml = CryptoService.Encrypt(xml, _password);
                    System.IO.File.WriteAllText(saveDialog.FileName, encryptedXml);

                    MessageBox.Show("Данные успешно экспортированы", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при экспорте: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    var encryptedXml = System.IO.File.ReadAllText(openDialog.FileName);
                    var decryptedXml = CryptoService.Decrypt(encryptedXml, _password);
                    var entries = PasswordEntryList.DeserializeFromXml(decryptedXml);

                    foreach (var entry in entries)
                    {
                        PasswordRepository.Add(_userId, entry, _password);
                    }

                    _bindingSource.DataSource = PasswordRepository.GetAll(_userId, _password);
                    dataGridView.DataSource = _bindingSource;

                    MessageBox.Show("Импорт завершён", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при импорте: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_isClosing) return;
            _isClosing = true;

            var result = MessageBox.Show("Закрыть приложение?", "Выход", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.No)
            {
                e.Cancel = true;
                _isClosing = false;
            }
            else
            {
                Application.Exit();
            }
        }
    }
}
