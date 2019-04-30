using Common.DotNet.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using VaultEagle;

namespace VaultEagleConsole
{
    public static class ServerUtils
    {
        // return - true : one of server is valid or ServerList is empty, false : no server is valid
        public static bool CheckServers(List<string> ServerList)
        {
            if (ServerList.Count == 0)
                return true;

            List<string> serverList = ServerList;
            List<Task<PingReply>> pingTasks = new List<Task<PingReply>>();
            foreach (var server in serverList)
            {
                pingTasks.Add(PingAsync(server));
            }

            Task.WaitAll(pingTasks.ToArray());

            foreach (var pingTask in pingTasks)
            {
                if (pingTask.Result != null)
                {
                    return true;
                }

            }

            return false;
        }

        // return - response from url
        public static Task<PingReply> PingAsync(string address)
        {
            var tcs = new TaskCompletionSource<PingReply>();
            Ping ping = new Ping();
            ping.PingCompleted += (obj, sender) =>
            {
                tcs.SetResult(sender.Reply);
            };
            ping.SendAsync(address, new object());
            return tcs.Task;
        }

        public static void ParseVaultUrl(string vaultUrl, ref string user, ref string pass, ref string server, ref string vault, ref string path)
        {
            try
            {

                if (!string.IsNullOrEmpty(vaultUrl))
                {
                    Uri sourceUri;
                    if (vaultUrl.Contains("://"))
                        sourceUri = new Uri(vaultUrl);
                    else
                        sourceUri = new Uri("http://" + vaultUrl);

                    if (!string.IsNullOrEmpty(sourceUri.UserInfo))
                    {
                        string userInfo = sourceUri.UserInfo;
                        if (userInfo.Contains(":"))
                        {
                            var split = userInfo.Split(new[] { ':' }, 2, StringSplitOptions.None);
                            SetIfNotNullOrEmpty(ref user, split[0]);
                            pass = split[1]; // can be String.Empty
                        }
                        else
                            SetIfNotNullOrEmpty(ref user, userInfo);
                    }

                    SetIfNotNullOrEmpty(ref server, sourceUri.Host);

                    var absolutePath = sourceUri.AbsolutePath;
                    // "/Vault2/$/Designs" => {"", "Vault2", "$/Designs"}
                    var split2 = absolutePath.Split(new[] { '/' }, 3, StringSplitOptions.None);
                    if (split2.Length > 1)
                        SetIfNotNullOrEmpty(ref vault, split2[1]);
                    if (split2.Length > 2)
                        SetIfNotNullOrEmpty(ref path, split2[2]);
                }
            }
            catch (UriFormatException)
            {
                //Print("Couldn't parse: " + vaultUrl, "Error");
                //PrintUsageAndExit();
                throw new Exception("Couldn't parse: " + vaultUrl);
            }
        }

        private static void SetIfNotNullOrEmpty(ref string target, string s)
        {
            if (!string.IsNullOrEmpty(s))
                target = Uri.UnescapeDataString(s);
        }

        public class DoNotRetry { }

        public static void DoWithRetry(IProgressWindow logger, Action action, int numberOfRetries, TimeSpan? retryDelay)
        {
            var retryDelayInSecondsSchedule =
                retryDelay.HasValue
                    ? new[] { retryDelay.Value }.Cycle()
                    : new[]
                        {
                            10,
                            30,
                            60,
                            5*60,
                            10*60,
                            15*60,
                            60*60,
                            2*60*60,
                        }.Concat(new[] { 2 * 60 * 60 }.Cycle())
                         .Select(x => TimeSpan.FromSeconds(x));

            foreach (var delay in retryDelayInSecondsSchedule.Take(numberOfRetries))
            {
                try
                {
                    action();
                    return;
                }
                catch (SimpleException<DoNotRetry> ex)
                {
                    if (ex.InnerException != null)
                        throw ex.InnerException;
                    throw new ErrorMessageException(ex.Message);
                }
                catch (ErrorMessageException ex)
                {
                    logger.Log("Error: " + ex.Message);
                    logger.Log("Waiting to retry... (" + delay.ToPrettyFormat() + ")");
                    System.Threading.Thread.Sleep(delay);
                }
                catch (Exception ex)
                {
                    logger.Log(VaultServerException.WrapException(ex).ToString());
                    logger.Log("Waiting to retry... (" + delay.ToPrettyFormat() + ")");
                    System.Threading.Thread.Sleep(delay);
                }
            }
            action();
        }
    }
    
}