using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Forms;

namespace RoboChat.Model
{
    public class RosterModel : IRosterModel
    {
        #region Const

        const string CONTACTS_FILE_PATH = "config\\contacts.cfg";
        private const string TEMP_FILE_DIRECTORY = "temp";

        #endregion

        #region Fields

        private ConnectionSend _connectionSend;
        private ConnectionReceive _connectionReceive;

        #endregion

        #region Properties

        private ObservableCollection<Contact> _contacts;
        public ObservableCollection<Contact> Contacts
        {
            get
            {
                if (_contacts == null)
                {
                    string contactsPath = Path.Combine(Settings.Directory, CONTACTS_FILE_PATH);
                    if (File.Exists(contactsPath))
                    {
                        string xml = File.ReadAllText(contactsPath);

                        Contact[] contacts;
                        try
                        {
                            contacts = Helpers.DeserializeFromXml<Contact[]>(xml);
                        }
                        catch (Exception ex)
                        {
                            ex.Process(ErrorHandlingLevels.Tray, "Cannot load contact list");
                            contacts = new Contact[0];
                        }

                        foreach (var contact in contacts)
                        {
                            contact.PropertyChanged += Contact_PropertyChanged;
                            contact.IsConnectionLost = false;
                        }

                        _contacts = new ObservableCollection<Contact>(contacts);
                    }
                    else
                    {
                        _contacts = new ObservableCollection<Contact>();
                    }

                    _contacts.CollectionChanged += _contactsStorage_CollectionChanged;
                }

                return _contacts;
            }
        }

        #endregion

        #region Events

        public event EventHandler<MessageEventArgs> MessageReceived;
        public event EventHandler<StatusEventArgs> StatusChanged;
        public event EventHandler<StatisticsEventArgs> StatisticsUpdated;

        #endregion

        #region Interface Implementation

        /// <summary>
        /// Create threads for sending and receiving messages
        /// </summary>
        public void Start()
        {
            _connectionSend = new ConnectionSend();

            _connectionReceive = new ConnectionReceive();
            _connectionReceive.MessageReceived += _connectionReceive_MessageReceived;
            _connectionReceive.StatusChanged += _connectionReceive_StatusChanged;
            _connectionReceive.ContactsUpdated += _connectionReceive_ContactsUpdated;
            _connectionReceive.StatisticsUpdated += _connectionReceive_StatisticsUpdated;
        }

        /// <summary>
        /// Dispose of threads for sending and receiving messages
        /// </summary>
        public void Dispose()
        {
            SaveContacts();

            Interlocked.Exchange(ref ConnectionThread.FlagShouldClose, 1);

            if (_connectionSend != null)
                _connectionSend.Dispose();

            if (_connectionReceive != null)
            {
                _connectionReceive.MessageReceived -= _connectionReceive_MessageReceived;
                _connectionReceive.StatusChanged -= _connectionReceive_StatusChanged;
                _connectionReceive.ContactsUpdated -= _connectionReceive_ContactsUpdated;
                _connectionReceive.StatisticsUpdated -= _connectionReceive_StatisticsUpdated;
                _connectionReceive.Dispose();
            }
        }

        /// <summary>
        /// The main gateway for outgoing messages
        /// </summary>
        /// <param name="toAddress">Where to send</param>
        /// <param name="text">What to send</param>
        /// <returns>Copy of the sent message</returns>
        public Message Send(string toAddress, string text)
        {
            var resultMessage = QueueMessage(new Message
            {
                // Convert newlines to tags of newlines
                Text = text.Replace(Environment.NewLine, GetTag(Tags.Newline)),
                From = Settings.Instance.PublicKey,
                To = toAddress,
                Sent = DateTime.Now,
                Received = DateTime.MinValue
            });

            var contactForHistory = Contacts.FirstOrDefault(p => p.ID == resultMessage.Address);
            var contactForAuthor = Contacts.FirstOrDefault(p => p.ID == resultMessage.From);
            if (contactForHistory != null && contactForAuthor != null)
                History.AddToHistory(contactForHistory.Name, contactForAuthor.Name, resultMessage);

            // Provide a copy for external use
            return resultMessage;
        }

        /// <summary>
        /// The main gateway for outgoing files
        /// </summary>
        public void SendFile(Message message)
        {
            if (Settings.Instance.FileSizeLimit != 0 && message.FileLength > Settings.Instance.FileSizeLimit * 1024)
                throw new Exception("Error. File size limit exceeded.");

            Task.Factory.StartNew(() =>
           {
               string fileName = Path.GetFileName(message.FilePath);

               try
               {
                    // Finished sending file
                    if (message.FileOffset == message.FileLength)
                   {
                       SystemMessageOut(string.Format("[img]pack://application:,,,/Resources;component/images/file_sent.png[/img] \"{0}\"", fileName),
                           message.To, message.Sent, true);

                       return;
                   }
                    // Started sending file
                    else if (message.FileOffset == 0)
                   {
                       SystemMessageOut(string.Format("[img]pack://application:,,,/Resources;component/images/degrees0.png[/img] [0.00%] \"{0}\"", fileName),
                           message.To, message.Sent, true);
                   }
                   else
                   {
                       double part = (double)message.FileOffset / message.FileLength;
                       int degrees = 30 * (int)(12 * part);
                       SystemMessageOut(string.Format("[img]pack://application:,,,/Resources;component/images/degrees{2}.png[/img] [{1:0.00%}] \"{0}\"",
                           fileName, part, degrees), message.To, message.Sent, false);
                   }

                   using (var stream = File.OpenRead(message.FilePath))
                   {
                       stream.Seek(message.FileOffset, SeekOrigin.Begin);

                       byte[] buffer = new byte[Settings.Instance.FileBlockSize * 1024];
                       int count = stream.Read(buffer, 0, Settings.Instance.FileBlockSize * 1024);
                       string text = Convert.ToBase64String(buffer.Take(count).ToArray());

                       QueueMessage(new Message
                       {
                           Text = text,
                           From = Settings.Instance.PublicKey,
                           To = message.To,
                           Sent = message.Sent,
                           Received = DateTime.MinValue,
                           IsFile = true,
                           FilePath = message.FilePath,
                           FileOffset = message.FileOffset,
                           FileLength = message.FileLength
                       });
                   }
               }
               catch (Exception ex)
               {
                   ex.Process(ErrorHandlingLevels.Tray, "Error while sending file");

                   try
                   {
                       SystemMessageOut(string.Format("Error while sending file \"{0}\".", fileName),
                           message.To, message.Sent, true);
                   }
                   catch
                   {
                        // silence
                    }
               }
           });
        }

        /// <summary>
        /// Request the history to return messages for a chat with a certain user on a certain date
        /// </summary>
        /// <param name="address">User to request from history</param>
        /// <param name="dateTime">Date to request from history</param>
        /// <returns>Array of messages for a chat with a certain user on a certain date</returns>
        public Message[] GetFromHistory(string address, DateTime dateTime)
        {
            return History.GetFromHistory(address, dateTime);
        }

        public void ClearHistory(string address)
        {
            History.ClearHistory(address);
        }

        /// <summary>
        /// Open the folder where the history for a certain user is kept
        /// </summary>
        /// <param name="name">User to request from history</param>
        public void OpenHistory(string name)
        {
            try
            {
                string path;
                if (!History.GetUserPath(name, out path))
                    throw new Exception("Cannot get full path to user folder");

                Process.Start(new ProcessStartInfo(path));
            }
            catch (Exception ex)
            {
                ex.Process(ErrorHandlingLevels.Tray, "Cannot open history folder");
            }
        }

        private Message QueueMessage(Message message)
        {
            Message sendingMessage = new Message(message)
            {
                Raw = ""
            };

            Message resultMessage = new Message(message);

            var recipient = Contacts.FirstOrDefault(p => p.ID == sendingMessage.Recipient);
            if (recipient == null)
                return resultMessage;

            // Подтверждение о прочтении шифруем только если оно на шифрованное сообщение
            // Новое исходящее сообщение шифруем, если для контакта включили шифрование или
            // если контакт затребовал шифрование входящих сообщений
            sendingMessage.IsEncrypted = sendingMessage.IsNotification && sendingMessage.IsEncrypted ||
                !sendingMessage.IsNotification && (recipient.RequireEncryption || recipient.SendEncrypted);

            if (sendingMessage.IsEncrypted)
            {
                byte[] bytesDecrypted = Encoding.UTF8.GetBytes(sendingMessage.Text);
                byte[] bytesEncrypted = Helpers.EncryptAsymmetric(bytesDecrypted, sendingMessage.Recipient);
                sendingMessage.Text = Convert.ToBase64String(bytesEncrypted);
            }

            _connectionSend.QueueMessage(sendingMessage);

            resultMessage.IsEncrypted = sendingMessage.IsEncrypted;
            resultMessage.Raw = sendingMessage.Text;

            return resultMessage;
        }

        /// <summary>
        /// Request the parser to fill the span with all blocks found in messageText 
        /// and also store all found images for the future disposal
        /// </summary>
        /// <param name="span">Span to fill with blocks</param>
        /// <param name="messageText">Message text to parse for blocks</param>
        /// <param name="images">Where to store found images</param>
        public void ProcessMessage(Span span, string messageText, ref List<Image> images)
        {
            // Replacing slashes is needed as a fix for a side effect of parsing
            Parser.Parse(span, messageText.Replace("\\", "\\\\"), ref images);
        }

        /// <summary>
        /// Request the parser for the string representation of a tag
        /// </summary>
        /// <param name="tag">A tag to get a string representation for</param>
        /// <param name="param">Additional text to insert into tag if possible</param>
        /// <param name="attr">Additional attribute to insert into tag if possible</param>
        /// <returns>String representation of a tag</returns>
        public string GetTag(Tags tag, string param = "", string attr = "")
        {
            return Parser.GetTag(tag, param, attr);
        }

        /// <summary>
        /// Request the parser for the string representation of a tag by its name
        /// </summary>
        /// <param name="tagName">A tag name to get a string representation for</param>
        /// <param name="param">Additional text to insert into tag if possible</param>
        /// <param name="attr">Additional attribute to insert into tag if possible</param>
        /// <returns>String representation of a tag</returns>
        public string GetTag(string tagName, string param = "", string attr = "")
        {
            return Parser.GetTag(tagName, param, attr);
        }

        #endregion

        #region Event Handlers

        private void Contact_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != "IsFavorite" &&
                e.PropertyName != "RequireEncryption" &&
                e.PropertyName != "SendEncrypted")
            {
                return;
            }

            SaveContacts();
        }

        private void _contactsStorage_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            SaveContacts();
        }

        /// <summary>
        /// Modify contact profiles according to the new data
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _connectionReceive_ContactsUpdated(object sender, ContactsUpdatedEventArgs e)
        {
            App.Current.Dispatcher.Invoke((Action)(() =>
        {
            var offlineContacts = Contacts.Where(p => !e.Contacts.Any(q => q.ID == p.ID));
            foreach (var contact in offlineContacts)
            {
                contact.IsOnline = false;
            }

            foreach (var contactReceived in e.Contacts)
            {
                contactReceived.IsOnline = true;

                var contact = Contacts.FirstOrDefault(p => p.ID == contactReceived.ID);
                if (contact != null)
                {
                    contact.Name = contactReceived.Name;
                    contact.Version = contactReceived.Version;
                    contact.IsOnline = contactReceived.IsOnline;
                    contact.LastOnline = contactReceived.LastOnline;
                    contact.RequireEncryption = contactReceived.RequireEncryption;
                }
                else
                {
                        // Если контакт пришел впервые, то посмотрим, есть ли он в старом файле со списком избранных
                        string favoritesPath = Path.Combine(Settings.Directory, "config\\favorites.cfg");
                    if (File.Exists(favoritesPath))
                        contactReceived.IsFavorite = File.ReadAllLines(favoritesPath).Contains(contactReceived.Name);

                    contactReceived.PropertyChanged += Contact_PropertyChanged;
                    Contacts.Add(contactReceived);
                }
            }
        }));
        }

        private void _connectionReceive_StatisticsUpdated(object sender, StatisticsEventArgs e)
        {
            App.Current.Dispatcher.Invoke((Action)(() =>
        {
            StatisticsUpdated?.Invoke(sender, e);
        }));
        }

        /// <summary>
        /// Handle receving of a message
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _connectionReceive_MessageReceived(object sender, MessageEventArgs e)
        {
            e.Message.Raw = e.Message.Text;
            if (e.Message.IsEncrypted)
            {
                byte[] bytesEncrypted = Convert.FromBase64String(e.Message.Text);
                byte[] bytesDecrypted = Helpers.DecryptAssymetric(bytesEncrypted, Settings.Instance.PrivateKey);
                e.Message.Text = Encoding.UTF8.GetString(bytesDecrypted);
            }

            // A new message was received
            if (!e.Message.IsNotification)
                e.Message.Received = DateTime.Now;

            if (e.Message.IsFile)
            {
                // The message is a notification that a sent chunk of a file has been received
                if (e.Message.IsNotification)
                {
                    SendFile(e.Message);
                }
                // The message is a new chunk from a file that we are receiving
                else
                {
                    int bytesLength = ReceiveFile(e.Message);

                    if (bytesLength > 0)
                    {
                        QueueMessage(new Message(e.Message)
                        {
                            Text = "",
                            Raw = "",
                            FileOffset = e.Message.FileOffset + bytesLength,
                            IsNotification = true
                        });
                    }
                }
            }
            else
            {
                // Отправляем оповещение о прочтении
                if (!e.Message.IsNotification)
                {
                    QueueMessage(new Message(e.Message)
                    {
                        Raw = "",
                        IsNotification = true
                    });
                }

                ReceiveMessage(e.Message, e.Activate);
            }
        }

        /// <summary>
        /// Handle change of connection status
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _connectionReceive_StatusChanged(object sender, StatusEventArgs e)
        {
            if (e.IsOnline)
            {
                if (Settings.Instance.ShowSystemPopups)
                    AppTray.Message(ToolTipIcon.Warning, "Connection established.");

                foreach (var contact in Contacts)
                {
                    contact.IsConnectionLost = false;
                }
            }
            else
            {
                if (Settings.Instance.ShowSystemPopups)
                    AppTray.Message(ToolTipIcon.Warning, "Connection lost.");

                foreach (var contact in Contacts)
                {
                    contact.IsOnline = false;
                    contact.IsConnectionLost = true;
                }
            }

            StatusChanged?.Invoke(sender, e);
        }

        #endregion

        #region Helpers

        private void SaveContacts()
        {
            string contactsPath = Path.Combine(Settings.Directory, CONTACTS_FILE_PATH);
            var xml = Helpers.SerializeToXml(Contacts);
            File.WriteAllText(contactsPath, xml);
        }

        private void SystemMessageOut(string text, string to, DateTime sent, bool activate)
        {
            var recipient = Contacts.FirstOrDefault(p => p.ID == to);
            if (recipient == null)
                return;

            var message = new Message
            {
                Text = text,
                From = Settings.Instance.PublicKey,
                To = to,
                Sent = sent,
                Received = DateTime.Now,
                IsEncrypted = recipient.RequireEncryption || recipient.SendEncrypted
            };

            ReceiveMessage(message, activate);
        }

        private void SystemMessageIn(string text, string from, DateTime sent, bool isEncrypted, bool activate)
        {
            var message = new Message
            {
                Text = text,
                From = from,
                To = Settings.Instance.PublicKey,
                Sent = sent,
                Received = DateTime.Now,
                IsEncrypted = isEncrypted
            };

            ReceiveMessage(message, activate);
        }

        /// <summary>
        /// Receiving a message
        /// </summary>
        /// <param name="displayMessage">The message to be displayed</param>
        /// <param name="archiveMessage">The message to be put to history</param>
        private void ReceiveMessage(Message message, bool activate)
        {
            MessageReceived?.Invoke(this, new MessageEventArgs(message, activate));

            // It should be last not to pop up a message in a newly opened window
            var contactForHistory = Contacts.FirstOrDefault(p => p.ID == message.Address);
            var contactForAuthor = Contacts.FirstOrDefault(p => p.ID == message.From);
            if (contactForHistory != null && contactForAuthor != null)
                History.AddToHistory(contactForHistory.Name, contactForAuthor.Name, message);
        }

        /// <summary>
        /// Receiving a file
        /// </summary>
        /// <param name="message">The message to be parsed as a file part</param>
        /// <returns></returns>
        private int ReceiveFile(Message message)
        {
            try
            {
                var contact = Contacts.FirstOrDefault(p => p.ID == message.From);
                if (contact == null)
                    return 0;

                // Start to receive the first block
                if (message.FileOffset == 0)
                {
                    SystemMessageIn(string.Format("[img]pack://application:,,,/Resources;component/images/degrees0.png[/img] [0.00%] \"{0}\"",
                        message.FileName),
                        message.From, message.Sent, message.IsEncrypted, true);
                }

                string path = Path.Combine(Settings.Directory, TEMP_FILE_DIRECTORY);
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                path = Path.Combine(path, contact.Name);
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                path = Path.Combine(path, message.FileName);
                byte[] bytes = Convert.FromBase64String(message.Text);

                // If such a file exists - rewrite
                if (File.Exists(path) && message.FileOffset == 0)
                    File.Delete(path);

                using (var stream = File.Open(path, FileMode.Append))
                    stream.Write(bytes, 0, bytes.Length);

                // Finished receiving the last block
                if (message.FileOffset + bytes.Length == message.FileLength)
                {
                    // Куда в итоге сохранен файл
                    string movePath = History.StoreFile(contact.Name, path);

                    // Путь до файла относительно папки приложения
                    string relativePath = movePath.Replace(Settings.Directory, "");

                    // Let's make a link to a downloaded file
                    string url = GetRelativeLink(relativePath);
                    string urlDirectory = GetRelativeLink(Path.GetDirectoryName(relativePath));

                    string messageText;
                    if (new[] { ".jpg", ".jpeg", ".png", ".gif" }.Contains(Path.GetExtension(movePath).ToLower()))
                    {
                        messageText = string.Format("[link={0}][img]{0}[/img][/link]", url);
                    }
                    else
                    {
                        messageText = string.Format("[img]pack://application:,,,/Resources;component/images/file_received.png[/img] " +
                            "\"[link={1}]{0}[/link]\" [link={2}]Open folder[/link]", message.FileName, url, urlDirectory);
                    }

                    SystemMessageIn(messageText, message.From, message.Sent, message.IsEncrypted, true);
                }
                else
                {
                    double part = (double)(message.FileOffset + bytes.Length) / message.FileLength;
                    int degrees = 30 * (int)(12 * part);
                    SystemMessageIn(string.Format("[img]pack://application:,,,/Resources;component/images/degrees{2}.png[/img] [{1:0.00%}] \"{0}\"",
                        message.FileName, part, degrees), message.From, message.Sent, message.IsEncrypted, false);
                }

                return bytes.Length;
            }
            catch (Exception ex)
            {
                ex.Process(ErrorHandlingLevels.Tray, "Error while receiving file");

                try
                {
                    SystemMessageIn(string.Format("Error while receiving file \"{0}\".", message.FileName),
                            message.From, message.Sent, message.IsEncrypted, true);
                }
                catch
                {
                    // silence
                }

                return 0;
            }
        }

        private string GetRelativeLink(string path)
        {
            var parts = from part in path.Split('\\')
                        select HttpUtility.UrlEncode(part);

            return "robochat://" + string.Join("/", parts);
        }

        #endregion
    }
}