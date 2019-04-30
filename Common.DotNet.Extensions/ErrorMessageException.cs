using System;
using System.Runtime.InteropServices;
using Microsoft.Win32;	

namespace Common.DotNet.Extensions
{
    [Serializable]
    public class ErrorMessageException : Exception
    {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //
        public readonly Option<string>  Title;
        public readonly bool DontShowAgainMessageBox = false;          
        public readonly string DontShowAgainMessageBoxId = string.Empty;                
        public static string MessageNotinaModuleId = "{A894E1AF-076B-4119-9D65-20D8269EBE2F}";
        public static string MessageNotintheFolderId = "{B9A4CE2A-717D-4F0A-AFDC-5BF8A9841329}";
        public static string MessageOutsidetheModuleId = "{26DC969D-04E8-4057-8B01-9D03A1EA6F9F}";

        public ErrorMessageException()
        {
        }

        public ErrorMessageException(string message)
            : base(message)
        {
        }

        public ErrorMessageException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected ErrorMessageException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
        }

        public ErrorMessageException(string message, string dontShowAgainMessageBoxId, bool dontShowAgainMessageBox)
            : base(message)
        {
            this.DontShowAgainMessageBoxId = dontShowAgainMessageBoxId;
            this.DontShowAgainMessageBox = dontShowAgainMessageBox;
        }

        public ErrorMessageException(string message, string title) : base(message)
        {
            Title = Option.GetSome(title);
        }

        public ErrorMessageException(string message, string title, Exception exception)
            : base(message, exception)
        {
            Title = Option.GetSome(title);
        }

        public static void Do(Action action, string errorMessage = "Error: Unhandled exception", Action finallyAction = null, Action exceptionAction = null, string errorMessageBoxDefaultTitle = "Error", string debugErrorMessagePrefix = "ERROR: ", bool swallowOperationCancelled = false)
        {
            ExceptionCatcher.Do(() =>
            {
                try
                {
                    try
                    {
                        action();
                    }
                    catch (OperationCanceledException)
                    {
                        if(!swallowOperationCancelled)
                            throw;
                    }
                }
                catch (ErrorMessageException ex)
                {
                    HandleException(ex, errorMessage, errorMessageBoxDefaultTitle, debugErrorMessagePrefix);
                }
            }, errorMessage, finallyAction, exceptionAction);
        }

        public static void HandleException(Exception ex, string errorMessage = "Error: Unhandled exception", string errorMessageBoxDefaultTitle = "Error", string debugErrorMessagePrefix = "ERROR: ")
        {
            var eEx = ex as ErrorMessageException;
            if(eEx != null)
            {
                System.Diagnostics.Debug.WriteLine(debugErrorMessagePrefix + eEx.Message);

                if (eEx.DontShowAgainMessageBox)
                    SHMessageBoxCheck(
                        IntPtr.Zero,
                        "Error: " + eEx.Message,
                        eEx.Title.Else(errorMessageBoxDefaultTitle),
                        MessageBoxCheckFlags.MB_OK | MessageBoxCheckFlags.MB_ICONEXCLAMATION,
                        0,
                        eEx.DontShowAgainMessageBoxId
                        );
                else
                    System.Windows.Forms.MessageBox.Show("Error: " + eEx.Message, eEx.Title.Else(errorMessageBoxDefaultTitle));
            }
            else
            {
                ExceptionCatcher.HandleException(ex, errorMessage);
            }
        }

        public static void ClearDontShowAgainMessageBoxFromRegister(string dontShowAgainMessageBoxId)
        {
            RegistryKey regKey = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\DontShowMeThisDialogAgain");

            regKey.DeleteValue(dontShowAgainMessageBoxId, false);
        }

        public static void ClearDontShowAgainMessageBoxsFromRegister()
        {
            ClearDontShowAgainMessageBoxFromRegister(MessageNotinaModuleId);
            ClearDontShowAgainMessageBoxFromRegister(MessageNotintheFolderId);
            ClearDontShowAgainMessageBoxFromRegister(MessageOutsidetheModuleId);
        }

        public enum MessageBoxCheckFlags : uint
        {
            MB_OK = 0x00000000,
            MB_OKCANCEL = 0x00000001,
            MB_YESNO = 0x00000004,
            MB_ICONHAND = 0x00000010,
            MB_ICONQUESTION = 0x00000020,
            MB_ICONEXCLAMATION = 0x00000030,
            MB_ICONINFORMATION = 0x00000040
        }

        [DllImport("shlwapi.dll", EntryPoint = "#185", ExactSpelling = true, PreserveSig = false)]
        public static extern int SHMessageBoxCheck(
            [In] IntPtr hwnd,
            [In] String pszText,
            [In] String pszTitle,
            [In] MessageBoxCheckFlags uType,
            [In] int iDefault,
            [In] string pszRegVal
            );
    }
}