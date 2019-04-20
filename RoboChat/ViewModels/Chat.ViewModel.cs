using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using RoboChat.Win32;
using System.Windows.Documents;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media.Imaging;
using System.Windows.Markup;
using System.Runtime.CompilerServices;
using System.Threading;
using RoboChat.Enums;
using RoboChat.Contracts;

namespace RoboChat.ViewModels
{
    class ChatViewModel : INotifyPropertyChanged, IDisposable
    {
        private IRosterModel _model;
        private DateTime _topDate;

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private List<Image> _images = new List<Image>();

        #region Binding

        public Settings SettingsInstance
        {
            get { return Settings.Instance; }
        }

        private Contact _chatContact;
        public Contact ChatContact
        {
            get
            {
                return _chatContact;
            }
            private set
            {
                _chatContact = value;
            }
        }

        private string _textOutgoing;
        public string TextOutgoing
        {
            get
            {
                return _textOutgoing;
            }
            set
            {
                if (value != _textOutgoing)
                {
                    _textOutgoing = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private ObservableCollection<TextElement> _messages;
        public ObservableCollection<TextElement> Messages
        {
            get
            {
                if (_messages == null)
                    _messages = new ObservableCollection<TextElement>();

                return _messages;
            }
            set
            {
                if (value != _messages)
                {
                    _messages = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private bool _smilePopupIsOpen;
        public bool SmilePopupIsOpen
        {
            get
            {
                return _smilePopupIsOpen;
            }
            set
            {
                if (value != _smilePopupIsOpen)
                {
                    _smilePopupIsOpen = value;
                    NotifyPropertyChanged();
                }
            }
        }

        List<Image> _smileList;
        public List<Image> SmileList
        {
            get
            {
                return _smileList;
            }
            set
            {
                if (value != _smileList)
                {
                    _smileList = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private Image _smilePopupSelectedItem;
        public Image SmilePopupSelectedItem
        {
            get
            {
                return _smilePopupSelectedItem;
            }
            set
            {
                if (value != _smilePopupSelectedItem)
                {
                    _smilePopupSelectedItem = value;
                    NotifyPropertyChanged();
                }

                if (_smilePopupSelectedItem != null)
                {
                    AppendText((string)_smilePopupSelectedItem.Tag + " ");
                    SmilePopupIsOpen = false;
                }
            }
        }

        private int _selectionStart;
        public int SelectionStart
        {
            get
            {
                return _selectionStart;
            }
            set
            {
                if (value != _selectionStart)
                {
                    _selectionStart = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private int _selectionLength;
        public int SelectionLength
        {
            get
            {
                return _selectionLength;
            }
            set
            {
                if (value != _selectionLength)
                {
                    _selectionLength = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private bool _shouldFocus;
        public bool ShouldFocus
        {
            get
            {
                return _shouldFocus;
            }
            set
            {
                if (value != _shouldFocus)
                {
                    _shouldFocus = value;
                    NotifyPropertyChanged();
                }

                // Сбросим значение
                if (value)
                {
                    ShouldFocus = false;
                }
            }
        }

        private bool _shouldScroll;
        public bool ShouldScroll
        {
            get
            {
                return _shouldScroll;
            }
            set
            {
                if (value != _shouldScroll)
                {
                    _shouldScroll = value;
                    NotifyPropertyChanged();
                }

                // Сбросим значение
                if (value)
                {
                    ShouldScroll = false;
                }
            }
        }

        private bool _shouldFind;
        public bool ShouldFind
        {
            get
            {
                return _shouldFind;
            }
            set
            {
                if (value != _shouldFind)
                {
                    _shouldFind = value;
                    NotifyPropertyChanged();
                }

                // Сбросим значение
                if (value)
                {
                    ShouldFind = false;
                }
            }
        }

        private bool _shouldPrint;
        public bool ShouldPrint
        {
            get
            {
                return _shouldPrint;
            }
            set
            {
                if (value != _shouldPrint)
                {
                    _shouldPrint = value;
                    NotifyPropertyChanged();
                }

                // Сбросим значение
                if (value)
                {
                    ShouldPrint = false;
                }
            }
        }

        private bool _isScrollTop;
        public bool IsScrollTop
        {
            get
            {
                return _isScrollTop;
            }
            set
            {
                _isScrollTop = value;
                NotifyPropertyChanged();

                if (_isScrollTop)
                {
                    DisplayFromHistory();
                }
            }
        }

        public ICommand Paste { get; private set; }
        public ICommand Encrypt { get; private set; }
        public ICommand Send { get; private set; }
        public ICommand Find { get; private set; }
        public ICommand Print { get; private set; }
        public ICommand NewLine { get; private set; }
        public ICommand Tag { get; private set; }
        public ICommand Quote { get; private set; }
        public ICommand Attach { get; private set; }
        public ICommand Smile { get; private set; }
        public ICommand Close { get; private set; }
        public ICommand History { get; private set; }
        public ICommand Activated { get; private set; }
        public ICommand Deactivated { get; private set; }
        public ICommand StateChanged { get; private set; }
        public ICommand Closed { get; private set; }
        public ICommand Loaded { get; private set; }

        #endregion

        public ChatViewModel(IRosterModel model, Contact contact)
        {
            _model = model;

            ChatContact = contact;

            DisplayFromHistory(true);

            Paste = new DelegateCommand(p => DoPaste());
            Encrypt = new DelegateCommand(p => DoEncrypt());
            Send = new DelegateCommand(p => DoSend());
            Find = new DelegateCommand(p => DoFind());
            Print = new DelegateCommand(p => DoPrint());
            NewLine = new DelegateCommand(p => DoNewLine());
            Tag = new DelegateCommand(p => DoTag(p));
            Smile = new DelegateCommand(p => DoSmile());
            Quote = new DelegateCommand(p => DoQuote(p));
            Attach = new DelegateCommand(p => DoAttach());
            Close = new DelegateCommand(p => DoClose(p));
            History = new DelegateCommand(p => DoHistory());
            Activated = new DelegateCommand(p => DoActivated(p));
            Deactivated = new DelegateCommand(p => DoDeactivated(p));
            StateChanged = new DelegateCommand(p => DoStateChanged(p));
            Closed = new DelegateCommand(p => DoClosed(p));
            Loaded = new DelegateCommand(p => DoLoaded(p));

            _smilePopupIsOpen = false;

            _smileList = new List<Image>();
            if (!Settings.IsDesign)
            {
                foreach (var smiley in Settings.SmileysSelector)
                {
                    Image image = new Image();
                    image.Tag = smiley.Value;
                    XamlAnimatedGif.AnimationBehavior.SetSourceUri(
                        image,
                        new Uri(Path.Combine(Settings.SmileysLocation, smiley.Key)));

                    _smileList.Add(image);
                }
            }

            _textOutgoing = "";
        }

        public void Dispose()
        {
            _images.Clear();
        }

        private void DoPaste()
        {
            try
            {
                if (Clipboard.ContainsImage())
                {
                    string filePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".jpg");
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        var image = Clipboard.GetImage();
                        var encoder = new JpegBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(image));
                        encoder.Save(fileStream);
                    }

                    SendFile(filePath);
                }
                else if (Clipboard.ContainsFileDropList())
                {
                    var files = Clipboard.GetFileDropList();

                    foreach (var file in files)
                    {
                        SendFile(file);
                        // Задержка нужна для корректной работы DateTime.Now,
                        // по которому определяется ID сообщения
                        Thread.Sleep(25);
                    }
                }
                else if (Clipboard.ContainsText())
                {
                    string text = Clipboard.GetText();
                    AppendText(text);
                }
            }
            catch (Exception ex)
            {
                ex.Process(ErrorHandlingLevels.Tray);
            }
        }

        private void DoEncrypt()
        {
            ShouldFocus = true;

            if (ChatContact.RequireEncryption)
                return;

            ChatContact.SendEncrypted = !ChatContact.SendEncrypted;
        }

        private void DoSend()
        {
            if (string.IsNullOrEmpty(TextOutgoing) || !ChatContact.EnableSend)
                return;

            // Отсылаем сообщение с отбрасыванием пробелом по краям
            var message = _model.Send(ChatContact.ID, TextOutgoing.Trim());
            DisplayMessage(message);

            ShouldScroll = true;
            TextOutgoing = "";
            ShouldFocus = true;
        }

        private void DoFind()
        {
            ShouldFind = true;
        }

        private void DoPrint()
        {
            ShouldPrint = true;
        }

        private void DoNewLine()
        {
            AppendText(Environment.NewLine);
        }

        private void DoTag(object obj)
        {
            try
            {
                string tagName = (string)obj;

                int selectionLength = SelectionLength;

                string param;
                if (SelectionLength > 0)
                {
                    // Запомним выделенный текст
                    param = TextOutgoing.Substring(SelectionStart, SelectionLength);

                    // Удалим выделенный текст
                    int selectionStart = SelectionStart;
                    TextOutgoing = TextOutgoing.Remove(SelectionStart, SelectionLength);
                    SelectionStart = selectionStart;
                }
                else
                {
                    param = "";
                }

                // Обернем выделенный текст в тег
                string tag = _model.GetTag(tagName, param);

                // Добавим в текущее место
                AppendText(tag);

                // Вернем выделение
                SelectionStart -= tag.Length - tag.IndexOf("]") - 1;
                SelectionLength = selectionLength;

            }
            catch (Exception ex)
            {
                ex.Process(ErrorHandlingLevels.Tray);
            }
        }

        private void DoSmile()
        {
            SmilePopupIsOpen = true;
        }

        private void DoQuote(object obj)
        {
            var viewer = (FlowDocumentScrollViewer)obj;

            string param = "";
            if (viewer.Selection != null)
                param = viewer.Selection.Text;

            string attr = ChatContact.Name;

            // Обернем выделенный текст в тег
            string tag = _model.GetTag(Tags.Quote, param, attr);

            // Добавим в текущее место
            AppendText(tag);
        }

        private void DoAttach()
        {
            try
            {
                using (var openFileDialog = new System.Windows.Forms.OpenFileDialog())
                {
                    if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        SendFile(openFileDialog.FileName);
                    }
                }
            }
            catch (Exception ex)
            {
                ex.Process(ErrorHandlingLevels.Tray);
            }
        }

        private void SendFile(string filename)
        {
            var fileInfo = new FileInfo(filename);
            _model.SendFile(new Message
            {
                To = ChatContact.ID,
                Sent = DateTime.Now,
                FilePath = fileInfo.FullName,
                FileOffset = 0,
                FileLength = fileInfo.Length
            });
        }

        private void DoClose(object window)
        {
            ((Window)window).Close();
        }

        private void DoHistory()
        {
            _model.OpenHistory(ChatContact.Name);
        }

        private void DoActivated(object window)
        {
            if (Settings.Instance.EnableAnimation)
            {
                _images.ForEach(p => XamlAnimatedGif.AnimationBehavior.GetAnimator(p)?.Play());
            }

            ((Window)window).StopFlashingWindow();

            // При открытии окна поместим сразу фокус в поле для набора текста
            ShouldFocus = true;
        }

        private void DoDeactivated(object window)
        {
            if (Settings.Instance.EnableAnimation)
            {
                _images.ForEach(p => XamlAnimatedGif.AnimationBehavior.GetAnimator(p)?.Pause());
            }
        }

        private void DoStateChanged(object window)
        {
            if (((Window)window).WindowState == WindowState.Minimized)
                return;

            // При открытии окна поместим сразу фокус в поле для набора текста
            ShouldFocus = true;
        }

        private void DoClosed(object window)
        {
            Settings.SaveWindowSizeAndPosition((Window)window);
        }

        private void DoLoaded(object window)
        {
            Settings.LoadWindowSizeAndPosition((Window)window);

            // Прокрутим чат внизу
            ShouldScroll = true;

            // При открытии окна поместим сразу фокус в поле для набора текста
            ShouldFocus = true;
        }

        public void DisplayHeader(DateTime dateTime, int insertIndex = -1)
        {
            var blockXaml = RoboChat.Properties.Resources.Header
                .Replace("[TIME]", dateTime.ToShortDateString());
            var row = (TableRow)XamlReader.Parse(blockXaml);
            row.Tag = dateTime;

            if (insertIndex != -1)
                Messages.Insert(insertIndex, row);
            else
                Messages.Add(row);
        }

        public int DisplayMessage(Message message, int insertIndex = -1)
        {
            // Распарсим сообщение
            var span = new Span();
            _model.ProcessMessage(span, message.Text, ref _images);

            // Заполним шаблон сообщения
            string template;
            if (message.Sent.Date == DateTime.Today)
            {
                if (message.Direction == Directions.In)
                    template = RoboChat.Properties.Resources.ReceivedMessageToday;
                else if (message.Received == DateTime.MinValue)
                    template = RoboChat.Properties.Resources.SendingMessageToday;
                else
                    template = RoboChat.Properties.Resources.SentMessageToday;
            }
            else
            {
                if (message.Direction == Directions.In)
                    template = RoboChat.Properties.Resources.ReceivedMessage;
                else if (message.Received == DateTime.MinValue)
                    template = RoboChat.Properties.Resources.SendingMessage;
                else
                    template = RoboChat.Properties.Resources.SentMessage;
            }

            string blockXaml = template
                .Replace("[TIME]", message.Sent.ToLongTimeString())
                .Replace("[DATE]", message.Sent.ToShortDateString())
                .Replace("[TOOLTIP]", message.TooltipText)
                .Replace("[LETTER]", ChatContact.Name[0].ToString())
                .Replace("[LOCK]", message.IsEncrypted ? "Visible" : "Hidden");

            // Сформируем строчку по шаблону
            var row = (TableRow)XamlReader.Parse(blockXaml);
            row.Tag = message;

            var container = (Span)row.GetLogicalChildOfType<Bold>().Parent;
            container.Inlines.Clear();
            container.Inlines.Add(span);

            var oldRow = Messages.OfType<TableRow>()
                .FirstOrDefault(p => p.Tag is Message && ((Message)p.Tag).ID == message.ID);

            // Если оригинала для оповещения о прочтении нет в окне, то ничего и не делаем
            int resultIndex = insertIndex;
            if (oldRow != null)
            {
                // Заменим строчку, чтобы вначале сработало событие удаления, а потом вставки
                int index = Messages.IndexOf(oldRow);
                Messages.Remove(oldRow);
                Messages.Insert(index, row);
            }
            else
            {
                if (insertIndex != -1)
                {
                    Messages.Insert(insertIndex, row);
                }
                else
                {
                    Messages.Add(row);
                    ShouldScroll = true;
                }

                resultIndex++;
            }

            return resultIndex;
        }

        public void Drop(System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
                foreach (var file in files)
                {
                    SendFile(file);
                    // Задержка нужна для корректной работы DateTime.Now,
                    // по которому определяется ID сообщения
                    Thread.Sleep(25);
                }
            }
        }

        private void DisplayFromHistory(bool isFirst = false)
        {
            if (isFirst)
                _topDate = DateTime.Today;

            DateTime oldTopDate = _topDate;

            var messages = _model.GetFromHistory(ChatContact.Name, isFirst ? _topDate : _topDate.AddDays(-1));
            if (messages.Length > 0)
            {
                int index = 0;
                foreach (var message in messages)
                {
                    index = DisplayMessage(message, index);
                }

                _topDate = messages[0].Sent.Date;
                DisplayHeader(_topDate, 0);
            }

            // Заголовок сегодняшнего дня должен присутствовать обязательно, даже если за сегодня ничего нет
            if (isFirst && (oldTopDate != _topDate || messages == null || messages.Length == 0))
            {
                DisplayHeader(oldTopDate);
            }

            if (isFirst)
                ShouldScroll = true;
        }

        private void AppendText(string text)
        {
            int selectionStart = SelectionStart;
            TextOutgoing = TextOutgoing.Insert(selectionStart, text);
            SelectionStart = selectionStart + text.Length;
            ShouldFocus = true;
        }
    }
}