using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace PasswordManager
{
    [Serializable]
    [XmlRoot("PasswordEntries")]
    public class PasswordEntryList : List<PasswordEntry>
    {
        public PasswordEntryList() : base() { }

        public PasswordEntryList(IEnumerable<PasswordEntry> collection) : base(collection)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection), "Коллекция не может быть null");
        }

        public new void Add(PasswordEntry entry)
        {
            try
            {
                if (entry == null)
                    throw new ArgumentNullException(nameof(entry), "Запись не может быть null");

                if (!entry.IsValid())
                    throw new ArgumentException("Некорректная запись пароля: отсутствует название или пароль");

                if (this.Any(e => e.Title.Equals(entry.Title, StringComparison.OrdinalIgnoreCase) &&
                                e.Username.Equals(entry.Username, StringComparison.OrdinalIgnoreCase)))
                {
                    throw new InvalidOperationException(
                        "Запись с таким названием и логином уже существует");
                }

                base.Add(entry);
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Ошибка добавления записи", ex);
            }
        }

        public void AddEntry(PasswordEntry entry)
        {
            this.Add(entry);
        }

        public bool UpdateEntry(int index, PasswordEntry newEntry)
        {
            try
            {
                if (index < 0 || index >= this.Count)
                    return false;

                if (newEntry == null || !newEntry.IsValid())
                    return false;

                this[index] = newEntry;
                newEntry.UpdateModifiedDate();
                return true;
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Ошибка обновления записи", ex);
            }
        }

        public bool RemoveEntry(int index)
        {
            try
            {
                if (index < 0 || index >= this.Count)
                    return false;

                this.RemoveAt(index);
                return true;
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Ошибка удаления записи", ex);
            }
        }

        public byte[] Serialize()
        {
            try
            {
                var serializer = new XmlSerializer(typeof(PasswordEntryList));
                using (var ms = new MemoryStream())
                {
                    serializer.Serialize(ms, this);
                    return ms.ToArray();
                }
            }
            catch (Exception ex)
            {
                throw new SerializationException("Ошибка сериализации записей", ex);
            }
        }

        public static PasswordEntryList Deserialize(byte[] data)
        {
            try
            {
                if (data == null || data.Length == 0)
                    return new PasswordEntryList();

                var serializer = new XmlSerializer(typeof(PasswordEntryList));
                using (var ms = new MemoryStream(data))
                {
                    return (PasswordEntryList)serializer.Deserialize(ms);
                }
            }
            catch (Exception ex)
            {
                throw new SerializationException("Ошибка десериализации записей", ex);
            }
        }

        public string SerializeToXml()
        {
            try
            {
                var serializer = new XmlSerializer(typeof(PasswordEntryList));
                var settings = new XmlWriterSettings
                {
                    Indent = true,
                    IndentChars = "  ",
                    NewLineChars = "\r\n",
                    Encoding = Encoding.UTF8
                };

                using (var writer = new StringWriter())
                using (var xmlWriter = XmlWriter.Create(writer, settings))
                {
                    serializer.Serialize(xmlWriter, this);
                    return writer.ToString();
                }
            }
            catch (Exception ex)
            {
                throw new SerializationException("Ошибка сериализации в XML", ex);
            }
        }

        public static PasswordEntryList DeserializeFromXml(string xml)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(xml))
                    return new PasswordEntryList();

                var serializer = new XmlSerializer(typeof(PasswordEntryList));
                using (var reader = new StringReader(xml))
                {
                    return (PasswordEntryList)serializer.Deserialize(reader);
                }
            }
            catch (Exception ex)
            {
                throw new SerializationException("Ошибка десериализации из XML", ex);
            }
        }
    }
}