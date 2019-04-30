using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Common.DotNet.Extensions.Winforms;

namespace Common.DotNet.Extensions
{
    public partial class ExceptionForm : Form
    {
        private Bitmap Screenshot;
        private readonly string NamespaceStartsWith;
        private string FileNameTimeStamp; // E.g. "2012-12-22 13.50.50"
        private string TimeStamp; // E.g. "2012-12-22 13:50:50 +02:00"
        private string FileNameBase;
        private string Message;


        public ExceptionForm(string message, string title = "", string namespaceStartsWith = "Common")
        {
            Bitmap screenshot = null;
            try
            {
                screenshot = CaptureFullScreen();
            }
            catch { }

            Screenshot = screenshot;
            NamespaceStartsWith = namespaceStartsWith;

            TimeStamp = Utils.CurrentTimeString;
            FileNameTimeStamp = TimeStamp.Replace(':', '.');
            if(Utils.IsCurrentTimeSameAsSwedishTime())
                FileNameTimeStamp = string.Join(" ", FileNameTimeStamp.Split(" ").SkipLast(1)); // "2012-12-22 13.50.50 +02.00" => "2012-12-22 13.50.50"

            InitializeComponent();
            this.SetUseCompatibleTextRendering(false);

            var oldWidth = logFolderLinkLabel.Width;
            var config = CommonDotNetExtensionsConfig.LoadFromFile();
            logFolderLinkLabel.Text = config.ExceptionLogPath;
            if (logFolderLinkLabel.Width > oldWidth)
            {
                var delta = logFolderLinkLabel.Width - oldWidth;
                Width += delta;
                label1.MaximumSize = new Size(label1.MaximumSize.Width+delta, label1.MaximumSize.Height);
            }

            Text = title;
            Message = message;
            label1.Text = TimeStamp + "\r\n" + MessageText;
            MinimumSize = new Size(Width, Height);
            Height = (Height - panel1.Height + 6) + label1.PreferredSize.Height;
            MaximumSize = new Size(Width, Screen.FromControl(this).Bounds.Height*8/10);

            okButton.Focus();

            try
            {
                FileNameBase = GetFileNameBase(message, NamespaceStartsWith);

                var dir = GetDirectoryInfo();
                var logFilePath = GetTimeStampPath(dir, "txt");
                if(logFilePath != null)
                    File.WriteAllText(logFilePath.FullName, TextForLogFile, Encoding.UTF8);
            }
            catch { }
        }

        private string GetFileNameBase(string message, string namespaceStartsWith)
        {
            // at Matrix3Designer.FindEquipLite.<>c__DisplayClass1.<R3ClassTreeViewOnDrawNode>b__0() in C:\Users\sefangej\Documents\Visual Studio 2010\Projects\MatrixTools3DesignerLibrary\3DesignerInventor\FindEquipLite.cs:line 83

            // at Matrix.Program.iPipe.Commands.ConvenientMoveCopy.Complete(iLength offset) in C:\Users\sefangej\Documents\Visual Studio 2010\Projects\iPipe\iPipe\Commands\ConvenientMoveCopy.cs:line 350

            // at Matrix.Program.iPipe.Commands.CreateIPipe.UpdateBendRadius(EvaluatedBendMachineRule bendMachineRule, iLineValidationData validationData) in C:\Users\sefangej\Documents\Visual Studio 2010\Projects\iPipe\iPipe\Commands\CreateIPipe.cs:line 203 

            // at Matrix3Designer.FindEquipLite.<clearAllButton_Click>b__3d() in C:\Users\sefangej\Documents\Visual Studio 2010\Projects\MatrixTools3DesignerLibrary\3DesignerInventor\FindEquipLite.cs:line 655 

            string methodPart = "";
            string linePart = "";
            try
            {
                FindFirstInterestingLine(message, namespaceStartsWith)
                .IfSomeDo(interestingLine =>
                {
                    interestingLine = FixStackTraceLine(interestingLine);
                    var firstPart = GetClassAndMethod(interestingLine);
                    var lastTwo = firstPart.Split(".").TakeLast(2).ToArray();
                    if (lastTwo.Length < 2)
                        return;
                    methodPart = "." + string.Join(".", lastTwo).Trim('.');

                    var splitByLineNumber = interestingLine.Split(".cs:line ");
                    if (splitByLineNumber.Length == 2) // "...FindEquipLite.cs:line 655" => "...\3DesignerInventor\FindEquipLite", "655"
                    {
                        var lineNumber = splitByLineNumber.Last();
                        linePart = "_L" + lineNumber;
                        var splitByPathSep = splitByLineNumber.First().Split(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
                        if (splitByPathSep.Length > 1) // [..., "3DesignerInventor", "FindEquipLite"]
                        {
                            var fileNameWithoutExt = splitByPathSep.Last();
                            if (methodPart.StartsWith(fileNameWithoutExt + ".", StringComparison.OrdinalIgnoreCase))
                                linePart = "_" + fileNameWithoutExt + ".cs" + linePart;
                        }
                    }
                });
            }
            catch { }

            return MakeValidFileName(FileNameTimeStamp + "." + Environment.UserName + methodPart + linePart + ".txt", maxLength: 120).TrimStringAtEnd(".txt");
        }

        private static Option<string> FindFirstInterestingLine(string message, string namespaceStartsWith)
        {
            return message.Lines().Where(x => x.Contains("at " + namespaceStartsWith)).OptionFirst();
        }

        private static string GetClassAndMethod(string interestingLine)
        {
            var trimmed = interestingLine.Trim().TrimStringAtStart("at ");
            var firstPart = new string(trimmed.TakeWhile(c => char.IsLetterOrDigit(c) || c == '.' || c == '_').ToArray());
            return firstPart;
        }

        private static string FixStackTraceLine(string interestingLine)
        {
            interestingLine = System.Text.RegularExpressions.Regex.Replace(interestingLine, @"<>c__DisplayClas[a-z0-9]+\.", "");
            interestingLine = System.Text.RegularExpressions.Regex.Replace(interestingLine, @"<([^>]+)>b__[a-z0-9]+\(", "$1(");
            return interestingLine;
        }

        private void ExceptionForm_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Control && e.KeyCode == Keys.C)
                Clipboard.SetText(TextForClipboard);
        }

        private string TextForClipboard
        {
            get
            {
                return TimeStamp + "\r\n" + TextForLogFile;
            }
        }

        private string MessageText
        {
            get
            {
                var versionString = "";
                try
                {
                    var stackFrames = new StackTrace().GetFrames();
                    if (stackFrames != null)
                    {
                        var assembly = stackFrames.Select(f => f.GetMethod().DeclaringType).Where(t => t != null && t.FullName != null && t.FullName.StartsWith(NamespaceStartsWith)).Select(t => t.Assembly).OptionFirst();
                        assembly.IfSomeDo(assy =>
                        {
                            versionString = GetAssembyVersionAndDate(assy);
                        });
                    }

                }
                catch { } 
                try // try harder!
                {
                    if(string.IsNullOrEmpty(versionString))
                    {
                        var assemblyOption = FindFirstInterestingLine(Message, NamespaceStartsWith)
                            .Transform(FixStackTraceLine)
                            .Transform(GetClassAndMethod)
                            .Transform(classAndMethod => string.Join(".", classAndMethod.Split(".").SkipLast(1).ToArray()))
                            .Bind(@class => AppDomain.CurrentDomain.GetAssemblies().Where(a => a.GetType(@class, false) != null).OptionFirst()); // very inefficient!

                        assemblyOption.IfSomeDo(assy =>
                        {
                            versionString = GetAssembyVersionAndDate(assy);
                        });
                    }
                }
                catch { } 

                return versionString + Message;
            }
        }

        private static string GetAssembyVersionAndDate(Assembly assy)
        {
            var name = assy.GetName();
            var lastWriteTimeUtc = (DateTimeOffset)File.GetLastWriteTimeUtc(assy.Location);
            var formattedSwedishTime = Utils.ConvertToSwedishTime(lastWriteTimeUtc).ToString(Utils.ReadableIso8601TimeFormatString);
            return string.Format("Version of {0}.dll: {1}\r\nDate of {0}.dll: {2}\r\n", name.Name,
                                 name.Version.ToString(), formattedSwedishTime);
        }

        private string TextForLogFile
        {
            get
            {
                return "---------------------------\r\n"
                    + Text 
                    + "\r\n---------------------------\r\n"
                    + MessageText
                    + "\r\n---------------------------\r\n"
                    + "Screenshot  OK" 
                    + "\r\n---------------------------\r\n";
            }
        }

        private void logFolderLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var dir = GetDirectoryInfo();
            if(dir != null)
                System.Diagnostics.Process.Start(dir.FullName);
        }

        private DirectoryInfo GetDirectoryInfo()
        {
            try
            {
                var dir = new System.IO.DirectoryInfo(logFolderLinkLabel.Text);
                if (!dir.Exists)
                    dir.Create();
                return dir;
            } catch { }
            return null;
        }


        public static Bitmap CaptureFullScreen()
        {
            var allBounds = Screen.AllScreens.Select(s => s.Bounds).ToArray();
            Rectangle bounds = Rectangle.FromLTRB(allBounds.Min(b => b.Left), allBounds.Min(b => b.Top), allBounds.Max(b => b.Right), allBounds.Max(b => b.Bottom));

            var bitmap = ScreenCapturePInvoke.CaptureScreen(bounds, captureMouse: true);
            return bitmap;
        }

        public static Bitmap CapturePrimaryScreen()
        {
            Rectangle bounds = Screen.PrimaryScreen.Bounds;

            var bitmap = ScreenCapturePInvoke.CaptureScreen(bounds, captureMouse: true);
            return bitmap;
        }

        private void screenshotButton_Click(object sender, EventArgs e)
        {
            bool didSomething = false;
            var dir = GetDirectoryInfo();
            if(Screenshot != null)
            {
                var screenshotPath = GetTimeStampPath(dir, "png");
                if (screenshotPath != null)
                {
                    Screenshot.Save(screenshotPath.FullName, ImageFormat.Png);
                    didSomething = true;
                }
                Screenshot.DisposeUnlessNull(ref Screenshot);
            }

            using(Bitmap screenshot = CaptureFullScreen())
            {
                var screenshotPath = GetTimeStampPath(dir, "png");
                if (screenshotPath != null)
                {
                    screenshot.Save(screenshotPath.FullName, ImageFormat.Png);
                    didSomething = true;
                }
            }
            if (didSomething)
                screenshotButton.Text = screenshotButton.Text.Replace('☐', '☑');
        }

        private FileInfo GetTimeStampPath(DirectoryInfo dir, string filetype)
        {
            if (dir == null || FileNameBase == null)
                return null;

            var firstScreenshotPath = new FileInfo(System.IO.Path.Combine(dir.FullName, FileNameBase + "." + filetype));
            if (!firstScreenshotPath.Exists)
                return firstScreenshotPath;
            for (int i = 1; i < 100; i++)
            {
                var screenshotPath = new FileInfo(System.IO.Path.Combine(dir.FullName, FileNameBase + "_" + i + "." + filetype));
                if (!screenshotPath.Exists)
                    return screenshotPath;
            }
            return null;
        }

        // from: http://stackoverflow.com/questions/309485/c-sharp-sanitize-file-name
        private static string MakeValidFileName(string name, int? maxLength = null)
        {
            string invalidChars = System.Text.RegularExpressions.Regex.Escape(new string(System.IO.Path.GetInvalidFileNameChars()));
            string invalidReStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);
            string validFileName = System.Text.RegularExpressions.Regex.Replace(name, invalidReStr, "_");

            if (maxLength.HasValue)
            {
                var ext = Path.GetExtension(validFileName) ?? "";
                validFileName = validFileName.Substring(0, Math.Min(validFileName.Length - ext.Length, Math.Max(0, maxLength.Value - ext.Length))) + ext;
            }
                
            return validFileName;
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            Close();
        }
    }

    #region ugly stuff

    // from http://stackoverflow.com/questions/918990/c-sharp-capturing-the-mouse-cursor-image
    public class ScreenCapturePInvoke
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct CURSORINFO
        {
            public Int32 cbSize;
            public Int32 flags;
            public IntPtr hCursor;
            public POINTAPI ptScreenPos;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINTAPI
        {
            public int x;
            public int y;
        }

        [DllImport("user32.dll")]
        private static extern bool GetCursorInfo(out CURSORINFO pci);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool DrawIconEx(IntPtr hdc, int xLeft, int yTop, IntPtr hIcon, int cxWidth, int cyHeight, int istepIfAniCur, IntPtr hbrFlickerFreeDraw, int diFlags);

        private const Int32 CURSOR_SHOWING = 0x0001;
        private const Int32 DI_NORMAL = 0x0003;

        public static Bitmap CaptureFullScreen(bool captureMouse)
        {
            var allBounds = Screen.AllScreens.Select(s => s.Bounds).ToArray();
            Rectangle bounds = Rectangle.FromLTRB(allBounds.Min(b => b.Left), allBounds.Min(b => b.Top), allBounds.Max(b => b.Right), allBounds.Max(b => b.Bottom));

            var bitmap = CaptureScreen(bounds, captureMouse);
            return bitmap;
        }

        public static Bitmap CapturePrimaryScreen(bool captureMouse)
        {
            Rectangle bounds = Screen.PrimaryScreen.Bounds;

            var bitmap = CaptureScreen(bounds, captureMouse);
            return bitmap;
        }

        public static Bitmap CaptureScreen(Rectangle bounds, bool captureMouse)
        {
            Bitmap result = new Bitmap(bounds.Width, bounds.Height);

            try
            {
                using (Graphics g = Graphics.FromImage(result))
                {
                    g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);

                    if (captureMouse)
                    {
                        CURSORINFO pci;
                        pci.cbSize = Marshal.SizeOf(typeof (CURSORINFO));

                        if (GetCursorInfo(out pci))
                        {
                            if (pci.flags == CURSOR_SHOWING)
                            {
                                var hdc = g.GetHdc();
                                DrawIconEx(hdc, pci.ptScreenPos.x-bounds.X, pci.ptScreenPos.y-bounds.Y, pci.hCursor, 0, 0, 0, IntPtr.Zero, DI_NORMAL);
                                g.ReleaseHdc();
                            }
                        }
                    }
                }
            }
            catch
            {
                result = null;
            }

            return result;
        }
    }

    #endregion
}
