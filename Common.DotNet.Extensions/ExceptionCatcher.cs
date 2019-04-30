using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Common.DotNet.Extensions
{
    public class ExceptionCatcher
    {
        public static Dictionary<string, Func<Exception,Exception>> ExceptionPreProcesssors = new Dictionary<string, Func<Exception, Exception>>();

        public delegate void Report(string message);

        public static Report Reporter = null;
        public static IWin32Window DialogParent = null;

        public static List<Func<string>> ExtraInfoProviders = new List<Func<string>> { ExceptionContext.InfoProvider };

        // Specify parent to allow Inventor to be alt-tabbable while this dialog is shown
        public static void Do(Action action, string errorMessage = "Error: Unhandled exception", Action finallyAction = null, Action exceptionAction = null, bool withoutNewExceptionContext = false)
        {
            if (withoutNewExceptionContext)
                Do_(action, errorMessage, finallyAction, exceptionAction);
            else
                ExceptionContext.InNew(() => Do_(action, errorMessage, finallyAction, exceptionAction));
        }

        private static void Do_(Action action, string errorMessage, Action finallyAction, Action exceptionAction)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                HandleException(ex, errorMessage);
                if (exceptionAction != null)
                {
                    Do(exceptionAction);
                }
            }
            if (finallyAction != null)
            {
                Do(finallyAction);
            }
        }

        public static void HandleException(Exception ex, string errorMessage = "Error: Unhandled exception")
        {
            var processedEx = ex;
            foreach (var exceptionPreProcesssor in ExceptionPreProcesssors.Values)
                processedEx = exceptionPreProcesssor(processedEx);

            if (Reporter != null)
                Reporter(processedEx.ToString());
            else
            {
                var message = errorMessage + "\r\n" + processedEx;
                foreach (var extraInfoProvider in ExtraInfoProviders)
                {
                    var extra = extraInfoProvider();
                    message = extra == null ? message : extra + "\r\n" + message;
                }
                var exceptionForm = new ExceptionForm(message, errorMessage);
                if (DialogParent != null)
                    exceptionForm.ShowDialog(DialogParent);
                else
                    exceptionForm.ShowDialog();
            }
        }
    }
}
