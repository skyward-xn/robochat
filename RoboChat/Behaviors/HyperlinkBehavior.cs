using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web;
using System.Windows.Documents;
using System.Windows.Interactivity;

namespace RoboChat.Behaviors
{
    public class HyperlinkBehavior : Behavior<Hyperlink>
    {
        protected override void OnAttached()
        {
            base.OnAttached();

            this.AssociatedObject.RequestNavigate += Hyperlink_RequestNavigate;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();

            this.AssociatedObject.RequestNavigate -= Hyperlink_RequestNavigate;
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            try
            {
                if (Directory.Exists(Settings.PluginDirectory))
                {
                    var pluginAssembly = Directory.GetFiles(Settings.PluginDirectory,
                        string.Format("HyperlinkBehavior_{0}.dll", e.Uri.Scheme))
                        .FirstOrDefault();

                    if (pluginAssembly != null)
                    {
                        var domain = AppDomain.CreateDomain("NewDomainName");
                        var pluginClass = domain.CreateInstanceFromAndUnwrap(pluginAssembly, "Plugin") as IPlugin;
                        pluginClass.Run(e.Uri);

                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                ex.Process(ErrorHandlingLevels.Tray, "Error while creating hyperlink plugin");
            }

            try
            {
                Process.Start(new ProcessStartInfo(HttpUtility.UrlDecode(e.Uri.AbsoluteUri)));
            }
            catch (Exception ex)
            {
                ex.Process(ErrorHandlingLevels.Tray, "Error while opening hyperlink");
            }

            e.Handled = true;
        }
    }
}