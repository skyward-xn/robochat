using RoboChat;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Web;

[Serializable]
public class Plugin : MarshalByRefObject, IPlugin
{
    public void Run(Uri uri)
    {
        string location = Assembly.GetExecutingAssembly().Location;
        string path = Path.GetDirectoryName(location);
        var directoryInfo = new DirectoryInfo(path);

        string uriString = HttpUtility.UrlDecode(uri.AbsoluteUri);
        uriString = uriString.Replace("robochat://", "file:///" + directoryInfo.Parent.FullName.Replace('\\', '/'));

        Process.Start(new ProcessStartInfo(uriString));
    }
}