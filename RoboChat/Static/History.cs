using System;
using System.IO;
using System.Linq;

namespace RoboChat.Model
{
    public static class History
    {
        private const string HISTORY_DIRECTORY = "history";

        private static object _historyLocker = new object();

        public static bool GetUserPath(string name, out string path)
        {
            path = null;

            // Папка истории
            string tempPath = Path.Combine(Settings.Directory, HISTORY_DIRECTORY);
            if (!Directory.Exists(tempPath))
                return false;

            // Папка контакта в истории
            tempPath = Path.Combine(tempPath, name);
            if (!Directory.Exists(tempPath))
                return false;

            path = tempPath;
            return true;
        }

        public static string SetUserPath(string name)
        {
            // Папка истории
            string path = Path.Combine(Settings.Directory, HISTORY_DIRECTORY);
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            // Папка контакта в истории
            path = Path.Combine(path, name);
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            return path;
        }

        public static string StoreFile(string name, string filepath)
        {
            string path = SetUserPath(name);

            path = Path.Combine(path, DateTime.Now.ToString("yyyy-MM-dd"));
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            path = Path.Combine(path, Path.GetFileName(filepath));
            if (File.Exists(path))
                File.Delete(path);

            File.Move(filepath, path);

            // For external use
            return path;
        }

        public static void AddToHistory(string contactNameForHistory, string contactNameForAuthor, Message message)
        {
            lock (_historyLocker)
            {
                try
                {
                    string path = SetUserPath(contactNameForHistory);

                    // Plain файл истории, для удобного ручного поиска
                    if (!message.IsNotification)
                    {
                        var plainLogPath = Path.Combine(path, DateTime.Now.ToString("yyyy-MM-dd") + ".log");
                        File.AppendAllText(plainLogPath, string.Format("[{0}] {1}: {2}",
                            message.Sent, contactNameForAuthor, message.Text) + Environment.NewLine);
                    }

                    // XML файл истории, основной
                    var xmlLogPath = Path.Combine(path, DateTime.Now.ToString("yyyy-MM-dd") + ".xml");
                    string xml = Helpers.SerializeToXml(message);
                    File.AppendAllText(xmlLogPath, xml + Environment.NewLine);
                }
                catch (Exception ex)
                {
                    ex.Process(ErrorHandlingLevels.Tray, "Cannot add message to history");
                }
            }
        }

        public static Message[] GetFromHistory(string name, DateTime dateTime)
        {
            lock (_historyLocker)
            {
                string history = "";

                try
                {
                    string path;
                    if (!GetUserPath(name, out path))
                        return new Message[0];

                    // Используем имена файлов для выбора предыдущего по дате
                    var topFile = new DirectoryInfo(path).GetFiles("????-??-??.xml")
                        .OrderByDescending(p => DateTime.Parse(Path.GetFileNameWithoutExtension(p.Name)))
                        .FirstOrDefault(p => DateTime.Parse(Path.GetFileNameWithoutExtension(p.Name)) < dateTime.Date.AddDays(1));
                    if (topFile == null)
                        return new Message[0];

                    history = File.ReadAllText(topFile.FullName);
                    if (history == "")
                        return new Message[0];
                }
                catch (Exception ex)
                {
                    ex.Process(ErrorHandlingLevels.Tray, "Cannot read messages from history");
                    return new Message[0];
                }

                try
                {
                    // Обернем сообщения в тег массива и десериализуем
                    history = string.Format("<ArrayOfMessage>{0}</ArrayOfMessage>", history);
                    var messages = Helpers.DeserializeFromXml<Message[]>(history);

                    // Дополнительная обработка для обратной совместимости
                    foreach (var message in messages)
                    {
                        if (message.From == name && message.To != Settings.Instance.Name)
                            message.To = Settings.Instance.Name;
                        else if (message.To == name && message.From != Settings.Instance.Name)
                            message.From = Settings.Instance.Name;
                    }

                    return messages;
                }
                catch (Exception ex)
                {
                    ex.Process(ErrorHandlingLevels.Tray, "Cannot parse messages from history");
                    return null;
                }
            }
        }

        public static void ClearHistory(string name)
        {
            lock (_historyLocker)
            {
                try
                {
                    string path;
                    if (!GetUserPath(name, out path))
                        return;

                    new DirectoryInfo(path).Delete(true);
                }
                catch (Exception ex)
                {
                    ex.Process(ErrorHandlingLevels.Tray, "Cannot clear history");
                }
            }
        }
    }
}
