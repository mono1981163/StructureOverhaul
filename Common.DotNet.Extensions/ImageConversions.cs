using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace Common.DotNet.Extensions
{
    public class ImageConversions
    {
        private class IconToolsAxHost : System.Windows.Forms.AxHost
        {
            private IconToolsAxHost() : base(string.Empty) { }

            public static stdole.IPictureDisp GetIPictureDispFromImage(System.Drawing.Image image)
            {
                return GetIPictureDispFromPicture(image).CastTo<stdole.IPictureDisp>();
            }

            // Note: use GetImageFromIPicture since GetPictureFromIPictureDisp is apparently broken (see doc)
            public static System.Drawing.Image GetImageFromIPicture(object iPicture)
            {
                return GetPictureFromIPicture(iPicture);
            }

            public static stdole.IPicture GetIPictureFromImage(Image image)
            {
                return (stdole.IPicture)GetIPictureFromPicture(image);
            }
        }

        public static stdole.IPictureDisp GetIPictureDisp(System.Drawing.Image image)
        {
            return IconToolsAxHost.GetIPictureDispFromImage(image);
        }

        public static stdole.IPicture GetIPictureFromImage(Image image)
        {
            return IconToolsAxHost.GetIPictureFromImage(image);
        }

        private static object Convert(object source, short type, Func<int> handleFunc, Func<int> hPalFunc)
        {
            const short PICTYPE_BITMAP = 1;
            const short PICTYPE_ICON = 3;
            const short PICTYPE_ENHMETAFILE = 4;
            switch (type)
            {
                case PICTYPE_BITMAP:
                    return Image.FromHbitmap((IntPtr)handleFunc(), (IntPtr)hPalFunc());
                case PICTYPE_ICON:
                    return Icon.FromHandle((IntPtr)handleFunc());
                case PICTYPE_ENHMETAFILE: // not tested
                    return System.Drawing.Imaging.Metafile.GetMetafileHeader((IntPtr)handleFunc());
            }
            return IconToolsAxHost.GetImageFromIPicture(source);
        }

        public static object Convert(stdole.IPicture pic)
        {
            return Convert(pic, pic.Type, () => pic.Handle, () => pic.hPal);
        }

        public static object Convert(stdole.IPictureDisp pic)
        {
            return Convert(pic, pic.Type, () => pic.Handle, () => pic.hPal);
        }

        public static Image GetImage(stdole.IPicture iPicture)
        {
            return ConvertIconToImage(Convert(iPicture));
        }

        public static Image GetImage(stdole.IPictureDisp iPictureDisp)
        {
            return ConvertIconToImage(Convert(iPictureDisp));
        }

        private static Image ConvertIconToImage(object converted)
        {
            var icon = converted as Icon;
            if (icon != null)
                return icon.ToBitmap();
            return converted.CastTo<Image>();
        }
    }
}
