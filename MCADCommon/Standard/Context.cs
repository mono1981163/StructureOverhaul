using Common.DotNet.Extensions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace MCADCommon.Standard
{
    public abstract class Context
    {
        public Factories<GUI.Control> GUIControlFactories { get; private set; }
        public Factories<GUI.Location> GUILocationFactories { get; private set; }
        public Factories<Rules.Rule> RuleFactories { get; private set; }
        public List<ExtensionBase> LoadedExtensions { get; set; }

        ///////////////////////////////////////////////////////////////////////////////////////////
        protected Context()
        {
            GUIControlFactories = new Factories<GUI.Control>();
            GUILocationFactories = new Factories<GUI.Location>();
            RuleFactories = new Factories<Rules.Rule>();
            RuleFactories.Add(new Rules.BuiltInRuleFactory());

            LoadedExtensions = new List<ExtensionBase>();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////
        public void CopyExtensionsFrom(Context other)
        {
            foreach (ExtensionBase extension in other.LoadedExtensions)
            {
                LoadedExtensions.Add(extension);
                extension.AddToContext(this);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////
        public abstract Action<Context> GetCommand(string id, string argument);

        public virtual Bitmap GetBitmap(string name)
        {
            object bitm = Properties.Resources.ResourceManager.GetObject(name);
            if (bitm != null && bitm is Bitmap)
                return (Bitmap)bitm;

            throw new ErrorMessageException("Unknown command icon: '" + name + "'.");
        }
        ///////////////////////////////////////////////////////////////////////////////////////////
        public virtual Icon GetIcon(string name)
        {
            object icon = Properties.Resources.ResourceManager.GetObject(name);
            if (icon != null && icon is System.Drawing.Icon)
                return (System.Drawing.Icon)icon;

            throw new ErrorMessageException("Unknown command icon: '" + name + "'.");
        }

        ///////////////////////////////////////////////////////////////////////////////////////////
        public virtual Image GetImage(string name)
        {
            object image = Properties.Resources.ResourceManager.GetObject(name);
            if (image != null && image is System.Drawing.Image)
                return (System.Drawing.Image)image;

            throw new ErrorMessageException("Unknown image: '" + name + "'.");
        }
    }
}
