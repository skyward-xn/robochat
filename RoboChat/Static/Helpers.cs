using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;

namespace RoboChat
{
    static class Helpers
    {
        private const string ERROR_FILE_NAME = "err.log";

        private static object _locker = new object();

        public static MemoryStream SerializeToStream(object o)
        {
            MemoryStream stream = new MemoryStream();
            IFormatter formatter = new BinaryFormatter();
            formatter.Serialize(stream, o);
            return stream;
        }

        public static object DeserializeFromStream(MemoryStream stream)
        {
            IFormatter formatter = new BinaryFormatter();
            stream.Seek(0, SeekOrigin.Begin);
            object o = formatter.Deserialize(stream);
            return o;
        }

        public static string SerializeToXml<T>(T obj)
        {
            using (var sw = new StringWriter())
            {
                var xws = new XmlWriterSettings()
                {
                    OmitXmlDeclaration = true,
                    Indent = true
                };
                using (var xmlWriter = XmlWriter.Create(sw, xws))
                {
                    var xmlSerializer = new XmlSerializer(typeof(T));
                    xmlSerializer.Serialize(xmlWriter, obj);
                    return sw.ToString();
                }
            }
        }

        public static T DeserializeFromXml<T>(string xml)
        {
            using (var sr = new StringReader(xml))
            {
                using (var xmlReader = XmlReader.Create(sr))
                {
                    var xmlSerializer = new XmlSerializer(typeof(T));
                    return (T)xmlSerializer.Deserialize(xmlReader);
                }
            }
        }

        public static string ComputeMD5(string input)
        {
            using (var md5Hash = MD5.Create())
            {
                byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

                var sBuilder = new StringBuilder();
                for (int i = 0; i < data.Length; i++)
                    sBuilder.Append(data[i].ToString("x2"));

                return sBuilder.ToString();
            }
        }

        public static byte[] Encrypt(string password, byte[] salt, byte[] bytesToEncrypt)
        {
            if (bytesToEncrypt == null || bytesToEncrypt.Length == 0)
                return new byte[0];

            using (var symmetric = new RijndaelManaged()
            {
                Mode = CipherMode.CBC
            })
            using (var pdb = new Rfc2898DeriveBytes(password, salt))
            {
                symmetric.Key = pdb.GetBytes(16);
                symmetric.GenerateIV();

                using (var cTransform = symmetric.CreateEncryptor())
                {
                    byte[] encryptedBytes = cTransform.TransformFinalBlock(bytesToEncrypt, 0, bytesToEncrypt.Length);
                    return symmetric.IV.Concat(encryptedBytes).ToArray();
                }
            }
        }

        public static byte[] Decrypt(string password, byte[] salt, byte[] bytesToDecrypt)
        {
            if (bytesToDecrypt == null || bytesToDecrypt.Length == 0)
                return new byte[0];

            using (var symmetric = new RijndaelManaged()
            {
                Mode = CipherMode.CBC
            })
            using (var pdb = new Rfc2898DeriveBytes(password, salt))
            {
                symmetric.Key = pdb.GetBytes(16);
                symmetric.IV = bytesToDecrypt.Take(16).ToArray();

                using (var cTransform = symmetric.CreateDecryptor())
                {
                    byte[] encryptedBytes = bytesToDecrypt.Skip(16).ToArray();
                    return cTransform.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
                }
            }
        }

        public static void GenerateKeyPair(out string privateKey, out string publicKey)
        {
            using (var csp = new RSACryptoServiceProvider()
            {
                PersistKeyInCsp = false
            })
            {
                privateKey = csp.ToXmlString(true);
                publicKey = csp.ToXmlString(false);
            }
        }

        public static byte[] EncryptAsymmetric(byte[] input, string publicKey)
        {
            using (var csp = new RSACryptoServiceProvider()
            {
                PersistKeyInCsp = false
            })
            {
                csp.FromXmlString(publicKey);

                int maxLength = csp.KeySize / 8 - 42;
                int iterations = (int)Math.Ceiling((double)input.Length / maxLength);

                var result = new List<byte>();
                for (int i = 0; i < iterations; i++)
                {
                    int length = Math.Min(maxLength, input.Length - maxLength * i);
                    byte[] bytesPart = input.Skip(maxLength * i).Take(length).ToArray();
                    byte[] encryptedBytes = csp.Encrypt(bytesPart, true);
                    result.AddRange(encryptedBytes);
                }
                return result.ToArray();
            }
        }

        public static byte[] DecryptAssymetric(byte[] input, string privateKey)
        {
            using (var csp = new RSACryptoServiceProvider()
            {
                PersistKeyInCsp = false
            })
            {
                csp.FromXmlString(privateKey);

                int maxLength = csp.KeySize / 8;
                int iterations = input.Length / maxLength;

                var result = new List<byte>();
                for (int i = 0; i < iterations; i++)
                {
                    byte[] bytesPart = input.Skip(maxLength * i).Take(maxLength).ToArray();
                    byte[] decryptedBytes = csp.Decrypt(bytesPart, true);
                    result.AddRange(decryptedBytes);
                }
                return result.ToArray();
            }
        }

        public static void Process(this Exception ex, ErrorHandlingLevels level, string extraMessage = null)
        {
            string innerExceptionMessage = "";
            if (ex.InnerException != null)
                innerExceptionMessage = "INNER EXCEPTION" + Environment.NewLine + ex.InnerException.Message;

            switch (level)
            {
                case ErrorHandlingLevels.Tray:
                    string trayMessage = string.IsNullOrEmpty(extraMessage) ? ex.Message : extraMessage;
                    AppTray.Message(ToolTipIcon.Error, trayMessage);
                    break;

                case ErrorHandlingLevels.Modal:
                    string modalMessage = "";

                    if (!string.IsNullOrEmpty(extraMessage))
                        modalMessage += extraMessage + Environment.NewLine + Environment.NewLine;

                    modalMessage += ex.Message;

                    if (!string.IsNullOrEmpty(innerExceptionMessage))
                        modalMessage += Environment.NewLine + Environment.NewLine + innerExceptionMessage;

                    System.Windows.MessageBox.Show(modalMessage, Settings.Title, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    break;
            }

            lock (_locker)
            {
                string path = Path.Combine(Settings.Directory, ERROR_FILE_NAME);
                string logMessage = DateTime.Now.ToString() + ": ";

                if (!string.IsNullOrEmpty(extraMessage))
                    logMessage += extraMessage + Environment.NewLine + Environment.NewLine;

                logMessage += ex.Message + Environment.NewLine + Environment.NewLine + ex.StackTrace;

                if (!string.IsNullOrEmpty(innerExceptionMessage))
                    logMessage += Environment.NewLine + Environment.NewLine + innerExceptionMessage;

                logMessage += Environment.NewLine + Environment.NewLine;

                File.AppendAllText(path, logMessage);
            }
        }

        public static void Restart()
        {
            System.Diagnostics.Process.Start(App.ResourceAssembly.Location, "--nocheck");
            App.Current.Shutdown();
        }

        public static T GetLogicalChildOfType<T>(this DependencyObject parent)
            where T : DependencyObject
        {
            foreach (var child in LogicalTreeHelper.GetChildren(parent).OfType<DependencyObject>())
            {
                if (child is T)
                    return (T)child;

                var result = child.GetLogicalChildOfType<T>();
                if (result != null)
                    return result;
            }

            return null;
        }
    }
}