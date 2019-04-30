using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

//using Windows.UI.Notifications;
//using Microsoft.Toolkit.Uwp.Notifications; // Notifications library
//using Microsoft.QueryStringDotNET; // QueryString.NET
namespace VaultEagleLib
{
    class ToastUtils
    {
        //public static ToastVisual CreateToastVisual(string title, string message)
        //{
        //    return new ToastVisual()
        //    {
        //        BindingGeneric = new ToastBindingGeneric()
        //        {
        //            Children =
        //            {
        //                new AdaptiveText(){
        //                    Text = title
        //                },
        //                new AdaptiveText(){
        //                    Text = message
        //                }
        //            },

        //            AppLogoOverride = new ToastGenericAppLogo()
        //            {
        //                Source = "",
        //                HintCrop = ToastGenericAppLogoCrop.Circle
        //            }
        //        }
        //    };
        //}

      /*  public static ToastNotification CreateToastNotification(ToastVisual visual)
        {
            ToastContent content = new ToastContent(){
                Visual = visual
            };
            Windows.Data.Xml.Dom.XmlDocument doc = new Windows.Data.Xml.Dom.XmlDocument();
            string stringContent = content.GetContent();
            doc.LoadXml(content.GetContent());
            return new ToastNotification(doc);
        }*/

    }
}
