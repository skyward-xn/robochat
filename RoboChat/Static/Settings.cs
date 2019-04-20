using RoboChat.DataStructures;
using RoboChat.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;

namespace RoboChat
{
    public class Settings : INotifyPropertyChanged
    {
        const string PROFILE_FILE_PATH = "config\\profile.cfg";
        const string WINDOWS_FILE_PATH = "config\\windows.cfg";
        const string SETTINGS_FILE_PATH = "config\\settings.cfg";
        const string BASIC_SETTINGS_FILE_PATH = "RoboChat.exe.config";
        const string SMILEYS_PATH = "resources\\kolobok";
        const string DICT_FILE_NAME = "dict.cfg";
        const string SEND_KEY_INDEX = "SEND_KEY";
        const string ROBO_PATH_INDEX = "ROBO_PATH";
        const string ROBO_CACHE_NAME_INDEX = "ROBO_CACHE_NAME";
        const string ROBO_CACHE_PASS_INDEX = "ROBO_CACHE_PASS";
        const string FONT_FAMILY_INDEX = "FONT_FAMILY";
        const string FONT_SIZE_INDEX = "FONT_SIZE";
        const string FILE_BLOCK_SIZE_INDEX = "FILE_BLOCK_SIZE";
        const string SHOW_OFFLINE_INDEX = "SHOW_OFFLINE";
        const string SHOW_GROUPS_INDEX = "SHOW_GROUPS";
        const string SHOW_SYSTEM_POPUPS_INDEX = "SHOW_SYSTEM_POPUPS";
        const string UPDATE_SOURCE_INDEX = "UPDATE_SOURCE";
        const string IGNORE_BASIC_SETTINGS_INDEX = "IGNORE_BASIC_SETTINGS";
        const string PRIVATE_KEY_ENCRYPTED_INDEX = "PRIVATE_KEY_ENCRYPTED";
        const string PUBLIC_KEY_INDEX = "PUBLIC_KEY";
        const string FILE_SIZE_LIMIT_INDEX = "FILE_SIZE_LIMIT";
        const string PENDING_INTERVAL_INDEX = "PENDING";
        const string STATUS_INTERVAL_INDEX = "STATUS";
        const string LIFETIME_INTERVAL_INDEX = "LIFETIME";
        const string SEND_OFFLINE_MESSAGES_INDEX = "SEND_OFFLINE_MESSAGES";
        const string REQUIRE_ENCRYPTION_INDEX = "REQUIRE_ENCRYPTION";
        const string ENABLE_SMILEYS_INDEX = "ENABLE_SMILEYS";

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private Settings()
        {
        }

        private static Settings _instance;
        public static Settings Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new Settings();

                return _instance;
            }
        }

        #region Smileys

        private static Dictionary<string, string> _smileysReplacer;
        public static Dictionary<string, string> SmileysReplacer
        {
            get
            {
                if (_smileysReplacer == null)
                {
                    _smileysReplacer = new Dictionary<string, string>();
                    var lines = File.ReadAllLines(Path.Combine(SmileysLocation, DICT_FILE_NAME));
                    var files = new DirectoryInfo(SmileysLocation).GetFiles("*.gif");
                    for (int i = 0; i < lines.Length; i++)
                    {
                        if (files.Length < i + 1)
                            break;

                        var codes = lines[i].Split(',');
                        foreach (var code in codes)
                        {
                            if (!_smileysReplacer.ContainsKey(code))
                                _smileysReplacer.Add(code, files[i].Name);
                        }
                    }
                }

                return _smileysReplacer;
            }
        }

        private static Dictionary<string, string> _smileysSelector;
        public static Dictionary<string, string> SmileysSelector
        {
            get
            {
                if (_smileysSelector == null)
                {
                    _smileysSelector = new Dictionary<string, string>();
                    var lines = File.ReadAllLines(Path.Combine(SmileysLocation, DICT_FILE_NAME));
                    var files = new DirectoryInfo(SmileysLocation).GetFiles("*.gif");
                    for (int i = 0; i < lines.Length; i++)
                    {
                        if (files.Length < i + 1)
                            break;

                        var codes = lines[i].Split(',');
                        if (codes.Length > 0 && !_smileysSelector.ContainsKey(files[i].Name))
                        {
                            _smileysSelector.Add(files[i].Name, codes.First());
                        }
                    }
                }

                return _smileysSelector;
            }
        }

        private static string _smileysLocation;
        public static string SmileysLocation
        {
            get
            {
                if (string.IsNullOrEmpty(_smileysLocation))
                {
                    _smileysLocation = Path.Combine(Directory, SMILEYS_PATH);
                }

                return _smileysLocation;
            }
        }

        #endregion

        #region Initialization

        private static Configuration _config;
        private static Configuration Config
        {
            get
            {
                if (_config == null)
                {
                    var configMap = new ExeConfigurationFileMap();
                    configMap.ExeConfigFilename = Path.Combine(Directory, SETTINGS_FILE_PATH);
                    _config = ConfigurationManager.OpenMappedExeConfiguration(configMap, ConfigurationUserLevel.None);
                }

                return _config;
            }
        }

        private static Configuration _basicConfig;
        private static Configuration BasicConfig
        {
            get
            {
                if (_basicConfig == null)
                {
                    var configMap = new ExeConfigurationFileMap();
                    configMap.ExeConfigFilename = Path.Combine(Directory, BASIC_SETTINGS_FILE_PATH);
                    _basicConfig = ConfigurationManager.OpenMappedExeConfiguration(configMap, ConfigurationUserLevel.None);
                }

                return _basicConfig;
            }
        }

        private static string _directory;
        public static string Directory
        {
            get
            {
                if (string.IsNullOrEmpty(_directory))
                {
                    string location = Assembly.GetEntryAssembly().Location;
                    _directory = Path.GetDirectoryName(location);
                }

                return _directory;
            }
        }

        private static string _version;
        public static string Version
        {
            get
            {
                if (string.IsNullOrEmpty(_version))
                {
                    _version = Assembly.GetEntryAssembly().GetName().Version.ToString();
                }

                return _version;
            }
        }

        private static bool? _isDesign;
        public static bool IsDesign
        {
            get
            {
                if (!_isDesign.HasValue)
                {
                    _isDesign = (bool)(DesignerProperties.IsInDesignModeProperty.GetMetadata(typeof(DependencyObject)).DefaultValue);
                }

                return _isDesign.Value;
            }
        }

        private static string _title;
        public static string Title
        {
            get
            {
                if (string.IsNullOrEmpty(_title))
                {
                    _title = Assembly.GetEntryAssembly().GetName().Name;
                }

                return _title;
            }
        }

        private static string _titleUpdate;
        public static string TitleUpdate
        {
            get
            {
                if (string.IsNullOrEmpty(_titleUpdate))
                {
                    _titleUpdate = Title + " Update Available";
                }

                return _titleUpdate;
            }
        }

        private static string _pluginDirectory;
        public static string PluginDirectory
        {
            get
            {
                if (string.IsNullOrEmpty(_pluginDirectory))
                {
                    _pluginDirectory = Path.Combine(Directory, "plugins");
                }

                return _pluginDirectory;
            }
        }

        private static Dictionary<string, Rect> _windowSettings;
        public static Dictionary<string, Rect> WindowSettings
        {
            get
            {
                if (_windowSettings == null)
                {
                    _windowSettings = new Dictionary<string, Rect>();

                    string path = Path.Combine(Directory, WINDOWS_FILE_PATH);
                    if (File.Exists(path))
                    {
                        string xml = File.ReadAllText(path);
                        _windowSettings = Helpers.DeserializeFromXml<Dictionary<string, Rect>>(xml);
                    }
                }

                return _windowSettings;
            }
        }

        #endregion

        #region Settings

        private string _name;
        public string Name
        {
            get
            {
                if (string.IsNullOrEmpty(_name))
                {
                    string profilePath = Path.Combine(Directory, PROFILE_FILE_PATH);
                    if (File.Exists(profilePath))
                        _name = File.ReadAllText(profilePath);

                    if (string.IsNullOrEmpty(_name))
                        _name = "newuser";
                }

                return _name;
            }
            set
            {
                _name = value;
                string profilePath = Path.Combine(Directory, PROFILE_FILE_PATH);
                File.WriteAllText(profilePath, _name);
            }
        }

        private uint? _pendingInterval;
        public uint PendingInterval
        {
            get
            {
                return GetValue(ref _pendingInterval, PENDING_INTERVAL_INDEX, 20000u, BasicConfig);
            }
        }

        /// <summary>
        /// Желательно значение не больше 1/3 LIFETIME
        /// </summary>
        private uint? _statusInterval;
        public uint StatusInterval
        {
            get
            {
                return GetValue(ref _statusInterval, STATUS_INTERVAL_INDEX, 20000u, BasicConfig);
            }
        }

        private uint? _lifetimeInterval;
        public uint LifetimeInterval
        {
            get
            {
                return GetValue(ref _lifetimeInterval, LIFETIME_INTERVAL_INDEX, 60000u, BasicConfig);
            }
        }

        public bool BasicSettingsOk
        {
            get
            {
                return IgnoreBasicSettings ||
                    !string.IsNullOrEmpty(CacheName) &&
                    !string.IsNullOrEmpty(CachePass) &&
                    !string.IsNullOrEmpty(UpdateSource);
            }
        }

        private bool? _ignoreBasicSettings;
        public bool IgnoreBasicSettings
        {
            get
            {
                return GetValue(ref _ignoreBasicSettings, IGNORE_BASIC_SETTINGS_INDEX, false, BasicConfig);
            }
            set
            {
                SetValue(ref _ignoreBasicSettings, IGNORE_BASIC_SETTINGS_INDEX, value, BasicConfig);
                NotifyPropertyChanged();
                NotifyPropertyChanged("BasicSettingsOk");
            }
        }

        private string _updateSource;
        public string UpdateSource
        {
            get
            {
                return GetValue(ref _updateSource, UPDATE_SOURCE_INDEX, "", BasicConfig);
            }
            set
            {
                SetValue(ref _updateSource, UPDATE_SOURCE_INDEX, value, BasicConfig);
                NotifyPropertyChanged();
                NotifyPropertyChanged("BasicSettingsOk");
            }
        }

        public string RobotnetPath
        {
            get
            {
                return "robotnet\\";
            }
        }

        private string _cacheName;
        public string CacheName
        {
            get
            {
                return GetValue(ref _cacheName, ROBO_CACHE_NAME_INDEX, "", BasicConfig);
            }
            set
            {
                SetValue(ref _cacheName, ROBO_CACHE_NAME_INDEX, value, BasicConfig);
                NotifyPropertyChanged();
                NotifyPropertyChanged("BasicSettingsOk");
            }
        }

        private string _cachePass;
        public string CachePass
        {
            get
            {
                return GetValue(ref _cachePass, ROBO_CACHE_PASS_INDEX, "", BasicConfig);
            }
            set
            {
                SetValue(ref _cachePass, ROBO_CACHE_PASS_INDEX, value, BasicConfig);
                NotifyPropertyChanged();
                NotifyPropertyChanged("BasicSettingsOk");
            }
        }

        private long? _fileSizeLimit;
        public long FileSizeLimit
        {
            get
            {
                return GetValue(ref _fileSizeLimit, FILE_SIZE_LIMIT_INDEX, 0, BasicConfig);
            }
            set
            {
                SetValue(ref _fileSizeLimit, FILE_SIZE_LIMIT_INDEX, value, BasicConfig);
                NotifyPropertyChanged();
            }
        }

        private int? _fileBlockSize;
        public int FileBlockSize
        {
            get
            {
                return GetValue(ref _fileBlockSize, FILE_BLOCK_SIZE_INDEX, 256, BasicConfig);
            }
            set
            {
                SetValue(ref _fileBlockSize, FILE_BLOCK_SIZE_INDEX, value, BasicConfig);
                NotifyPropertyChanged();
            }
        }

        private string Password
        {
            get
            {
                return "Password";
            }
        }

        private byte[] _salt;
        private byte[] Salt
        {
            get
            {
                if (_salt == null)
                    _salt = new byte[] { 33, 227, 90, 84, 74, 137, 112, 141 };

                return _salt;
            }
        }

        private string _privateKeyEncrypted;
        public string PrivateKeyEncrypted
        {
            get
            {
                return GetValue(ref _privateKeyEncrypted, PRIVATE_KEY_ENCRYPTED_INDEX, "", Config);
            }
            set
            {
                SetValue(ref _privateKeyEncrypted, PRIVATE_KEY_ENCRYPTED_INDEX, value, Config);
                NotifyPropertyChanged();
            }
        }

        private string _privateKey;
        public string PrivateKey
        {
            get
            {
                if (_privateKey == null)
                {
                    byte[] privateKeyEncryptedBytes = Convert.FromBase64String(PrivateKeyEncrypted);
                    byte[] privateKeyBytes = Helpers.Decrypt(Password, Salt, privateKeyEncryptedBytes);
                    _privateKey = Encoding.UTF8.GetString(privateKeyBytes);
                }

                return _privateKey;
            }
            set
            {
                _privateKey = value;

                byte[] privateKeyBytes = Encoding.UTF8.GetBytes(_privateKey);
                byte[] privateKeyEncryptedBytes = Helpers.Encrypt(Password, Salt, privateKeyBytes);
                PrivateKeyEncrypted = Convert.ToBase64String(privateKeyEncryptedBytes);
            }
        }

        private string _publicKey;
        public string PublicKey
        {
            get
            {
                return GetValue(ref _publicKey, PUBLIC_KEY_INDEX, "", Config);
            }
            set
            {
                SetValue(ref _publicKey, PUBLIC_KEY_INDEX, value, Config);
                NotifyPropertyChanged();
            }
        }

        private bool? _requireEncryption;
        public bool RequireEncryption
        {
            get
            {
                return GetValue(ref _requireEncryption, REQUIRE_ENCRYPTION_INDEX, false);
            }
            set
            {
                SetValue(ref _requireEncryption, REQUIRE_ENCRYPTION_INDEX, value);
                NotifyPropertyChanged();
            }
        }

        private SendKeyTypes? _sendKey;
        public bool UseCtrlInSendKey
        {
            get
            {
                return GetValue(ref _sendKey, SEND_KEY_INDEX, SendKeyTypes.CtrlEnter) == SendKeyTypes.CtrlEnter;
            }
            set
            {
                SetValue(ref _sendKey, SEND_KEY_INDEX, value ? SendKeyTypes.CtrlEnter : SendKeyTypes.Enter);
                NotifyPropertyChanged();
            }
        }

        private string _fontFamily;
        public string FontFamily
        {
            get
            {
                return GetValue(ref _fontFamily, FONT_FAMILY_INDEX, "Courier New");
            }
            set
            {
                SetValue(ref _fontFamily, FONT_FAMILY_INDEX, value);
                NotifyPropertyChanged();
            }
        }

        private int? _fontSize;
        public int FontSize
        {
            get
            {
                return GetValue(ref _fontSize, FONT_SIZE_INDEX, 12);
            }
            set
            {
                SetValue(ref _fontSize, FONT_SIZE_INDEX, value);
                NotifyPropertyChanged();
            }
        }

        private bool? _showOffline;
        public bool ShowOffline
        {
            get
            {
                return GetValue(ref _showOffline, SHOW_OFFLINE_INDEX, true);
            }
            set
            {
                SetValue(ref _showOffline, SHOW_OFFLINE_INDEX, value);
                NotifyPropertyChanged();
            }
        }

        private bool? _showGroups;
        public bool ShowGroups
        {
            get
            {
                return GetValue(ref _showGroups, SHOW_GROUPS_INDEX, true);
            }
            set
            {
                SetValue(ref _showGroups, SHOW_GROUPS_INDEX, value);
                NotifyPropertyChanged();
            }
        }

        private bool? _enableSmileys;
        public bool EnableSmileys
        {
            get
            {
                return GetValue(ref _enableSmileys, ENABLE_SMILEYS_INDEX, true);
            }
            set
            {
                SetValue(ref _enableSmileys, ENABLE_SMILEYS_INDEX, value);
                NotifyPropertyChanged();
            }
        }

        private bool? _enableAnimation;
        public bool EnableAnimation
        {
            get
            {
                return GetValue(ref _enableAnimation, SHOW_OFFLINE_INDEX, true);
            }
            set
            {
                SetValue(ref _enableAnimation, SHOW_OFFLINE_INDEX, value);
                NotifyPropertyChanged();
            }
        }

        private bool? _showSystemPopups;
        public bool ShowSystemPopups
        {
            get
            {
                return GetValue(ref _showSystemPopups, SHOW_SYSTEM_POPUPS_INDEX, false);
            }
            set
            {
                SetValue(ref _showSystemPopups, SHOW_SYSTEM_POPUPS_INDEX, value);
                NotifyPropertyChanged();
            }
        }

        private bool? _sendOfflineMessages;
        public bool SendOfflineMessages
        {
            get
            {
                return GetValue(ref _sendOfflineMessages, SEND_OFFLINE_MESSAGES_INDEX, false);
            }
            set
            {
                SetValue(ref _sendOfflineMessages, SEND_OFFLINE_MESSAGES_INDEX, value);
                NotifyPropertyChanged();
            }
        }

        #endregion

        #region Helpers

        public static void SaveWindowSizeAndPosition(Window window)
        {
            WindowSettings[window.GetType().ToString()] = new Rect(window.Left, window.Top, window.Width, window.Height);

            lock (WindowSettings)
            {
                string path = Path.Combine(Directory, WINDOWS_FILE_PATH);
                string xml = Helpers.SerializeToXml(WindowSettings);
                File.WriteAllText(path, xml);
            }
        }

        public static void LoadWindowSizeAndPosition(Window window)
        {
            if (!WindowSettings.ContainsKey(window.GetType().ToString()))
                return;

            var settings = WindowSettings[window.GetType().ToString()];
            if (
                settings.Left < SystemParameters.PrimaryScreenWidth &&
                settings.Top < SystemParameters.PrimaryScreenHeight
            )
            {
                window.Left = settings.Left;
                window.Top = settings.Top;
                window.Width = settings.Width;
                window.Height = settings.Height;
            }
        }

        private static T GetValue<T>(ref T? innerValue, string index, T defaultValue, Configuration config = null) where T : struct
        {
            if (config == null)
                config = Config;

            if (!innerValue.HasValue)
            {
                if (!config.AppSettings.Settings.AllKeys.Contains(index))
                    innerValue = defaultValue;
                else if (typeof(T).IsEnum)
                    innerValue = (T)Enum.Parse(typeof(T), config.AppSettings.Settings[index].Value);
                else
                    innerValue = (T)Convert.ChangeType(config.AppSettings.Settings[index].Value, typeof(T));
            }

            return innerValue.Value;
        }

        private static T GetValue<T>(ref T innerValue, string index, T defaultValue, Configuration config = null) where T : class
        {
            if (config == null)
                config = Config;

            if (innerValue == null)
            {
                if (!config.AppSettings.Settings.AllKeys.Contains(index))
                    innerValue = defaultValue;
                else
                    innerValue = (T)Convert.ChangeType(config.AppSettings.Settings[index].Value, typeof(T));
            }

            return innerValue;
        }

        private static void SetValue<T>(ref T innerValue, string index, T newValue, Configuration config = null)
        {
            if (config == null)
                config = Config;

            innerValue = newValue;

            if (!config.AppSettings.Settings.AllKeys.Contains(index))
                config.AppSettings.Settings.Add(index, innerValue.ToString());
            else
                config.AppSettings.Settings[index].Value = innerValue.ToString();

            config.Save();
        }

        #endregion
    }
}