using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Drawing.Text;
using System.Linq;
using System.Runtime.CompilerServices;

namespace RoboChat.ViewModels
{
    class FontsViewModel : INotifyPropertyChanged
    {
        int _fontSize;
        string _fontFamily;

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #region Binding

        public ICommand OK { get; private set; }
        public ICommand Cancel { get; private set; }

        public Settings SettingsInstance
        {
            get { return Settings.Instance; }
        }

        private string[] _fontFamilies;
        public string[] FontFamilies
        {
            get
            {
                if (_fontFamilies == null)
                {
                    var fontfamilies = from fontFamily in new InstalledFontCollection().Families
                                       select fontFamily.Name;

                    _fontFamilies = fontfamilies.ToArray();
                }

                return _fontFamilies;
            }
        }

        #endregion

        public FontsViewModel()
        {
            _fontSize = SettingsInstance.FontSize;
            _fontFamily = SettingsInstance.FontFamily;

            OK = new DelegateCommand(p => DoOK(p));
            Cancel = new DelegateCommand(p => DoCancel(p));
        }

        private void DoOK(object window)
        {
            ((Window)window).Close();
        }

        private void DoCancel(object window)
        {
            SettingsInstance.FontSize = _fontSize;
            SettingsInstance.FontFamily = _fontFamily;

            ((Window)window).Close();
        }
    }
}