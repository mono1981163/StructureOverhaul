using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Common.DotNet.Extensions;
namespace MCADCommon.MailCommon
{
    public class MailSettings
    {
        public string Sender { get; private set; }
        public string Password { get; private set; }
        public List<string> Receivers { get; private set; }
        public string Host { get; private set; }
        public int Port { get; private set; }
        public bool UseTls { get; private set; }

        public static MailSettings Read(XmlElement element, string passPhrase)
        {
            MailSettings settings = new MailSettings();
            settings.Sender = Environment.ExpandEnvironmentVariables(MCAD.XmlCommon.XmlTools.ReadString(element, "sender"));
            settings.Password = MCADCommon.EncryptionCommon.SimpleEncryption.DecryptString(MCAD.XmlCommon.XmlTools.ReadString(element, "password"), passPhrase);
            settings.Receivers = MCAD.XmlCommon.XmlTools.ReadStringValues(element, "receivers", "receiver");
            settings.Host = MCAD.XmlCommon.XmlTools.ReadString(element, "host");
            settings.Port = Convert.ToInt32(MCAD.XmlCommon.XmlTools.ReadString(element, "port"));
            settings.UseTls = MCAD.XmlCommon.XmlTools.SafeReadString(element, "use_tls").IsSome ? MCAD.XmlCommon.XmlTools.ReadString(element, "use_tls").Equals("True", StringComparison.InvariantCultureIgnoreCase) : false;

            return settings;
        }

        public void SetSender(string overrideSender)
        {
            Sender = overrideSender;
        }

    }
}
