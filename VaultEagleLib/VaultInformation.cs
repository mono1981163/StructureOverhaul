using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace VaultEagleLib
{
    public class VaultSettings
    {
        public string Server { get; private set; }
        public string Vault { get; private set; }
        public string UserName { get; private set; }
        public string Password { get; private set; }

        public static VaultSettings Read(XmlElement element)
        {
            string server = MCAD.XmlCommon.XmlTools.ReadString(element, "server");
            string vault = MCAD.XmlCommon.XmlTools.ReadString(element, "vault");
            string userName = MCAD.XmlCommon.XmlTools.ReadString(element, "user_name");
            string password = MCAD.XmlCommon.XmlTools.ReadString(element, "password");

            return new VaultSettings
            {
                Server = server,
                Vault = vault,
                UserName = userName,
                Password = password
            };
        }
    }
}
