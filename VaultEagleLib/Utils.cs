using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Common.DotNet.Extensions;

namespace VaultEagle
{
    //public class Utils
    //{
    //    public static void MoveFile(string source, string targetPath)
    //    {
    //        var creationDateUtc = File.GetCreationTimeUtc(source);
    //        DeleteFile(targetPath);
    //        System.IO.File.Move(source, targetPath); // sequence atomic on NTFS, apparently
    //        var targetInfo = new FileInfo(targetPath);

    //        var attributes = targetInfo.Attributes;
    //        targetInfo.Attributes = FileAttributes.Normal; // disable read only temporarily
    //        targetInfo.CreationTimeUtc = creationDateUtc; // preserve creation date. (note file system "tunneling")
    //        targetInfo.Attributes = attributes;
    //    }

    //    public static void DeleteFile(string targetPath)
    //    {
    //        var fileInfo = new System.IO.FileInfo(targetPath);
    //        if (!fileInfo.Exists)
    //            return;

    //        if (fileInfo.Attributes != FileAttributes.Normal)
    //            fileInfo.Attributes = FileAttributes.Normal;

    //        fileInfo.Delete();
    //    }
    //}
}
