using Common.DotNet.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VDF = Autodesk.DataManagement.Client.Framework.Vault;

namespace MCADCommon.VaultCommon
{
    public class ServerConnectionHandler : IDisposable
    {
        private Option<VDF.Currency.Connections.Connection> _Connection = Option.None;
        public VDF.Currency.Connections.Connection Connection { get { return _Connection.Get(); } }

        /*****************************************************************************************/
        public ServerConnectionHandler(string serverName, string vaultName, string userName, string password)
        {
            VDF.Results.LogInResult logInResult = VDF.Library.ConnectionManager.LogIn(serverName, vaultName, userName, password, VDF.Currency.Connections.AuthenticationFlags.Standard, null);
            if (!logInResult.Success)
                throw new ErrorMessageException("Failed to log in to vault: '" + vaultName + "@" + serverName + "' as user: '" + userName + "'." + Environment.NewLine + string.Join(Environment.NewLine, logInResult.ErrorMessages.Select(lr => lr.Key.ToString() + " - " + lr.Value).ToArray()));
            _Connection = logInResult.Connection.AsOption();
        }

        /*****************************************************************************************/
        public void Dispose()
        {
            if (_Connection.IsSome)
                VDF.Library.ConnectionManager.LogOut(_Connection.Get());
        }
    }
}
