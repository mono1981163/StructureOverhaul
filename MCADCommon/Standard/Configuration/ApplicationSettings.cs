using Common.DotNet.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace MCADCommon.Standard.Configuration
{
    public class ApplicationSettings
    {
        /*****************************************************************************************/
        /*****************************************************************************************/
        public class Command
        {
            public GUI.Control Control { get; private set; }
            public List<GUI.Location> Locations { get; private set; }

            /*************************************************************************************/
            public static Command Read(Context context, XmlElement element)
            {
                XmlElement controlElement = MCAD.XmlCommon.XmlTools.GetElement(element, "control");
                GUI.Control control = context.GUIControlFactories.Read(context, controlElement.Attributes["type"].Value, controlElement);

                List<GUI.Location> locations = new List<GUI.Location>();
                foreach (XmlElement locationElement in MCAD.XmlCommon.XmlTools.GetElements(MCAD.XmlCommon.XmlTools.GetElement(element, "locations"), "location"))
                    locations.Add(context.GUILocationFactories.Read(context, locationElement.Attributes["type"].Value, locationElement));

                return new Command
                {
                    Control = control,
                    Locations = locations
                };
            }

            /*************************************************************************************/
            public void Write(XmlDocument document, XmlElement element)
            {
                XmlElement controlElement = document.CreateElement("control");
                controlElement.Attributes.Append(MCAD.XmlCommon.XmlTools.WriteStringAttribute(document, "type", Control.Type));
                Control.Write(document, controlElement);
                element.AppendChild(controlElement);

                XmlElement locationsElement = document.CreateElement("locations");
                foreach (GUI.Location location in Locations)
                {
                    XmlElement locationElement = document.CreateElement("location");
                    locationElement.Attributes.Append(MCAD.XmlCommon.XmlTools.WriteStringAttribute(document, "type", location.Type));
                    location.Write(document, locationElement);
                    locationsElement.AppendChild(locationElement);
                }
                element.AppendChild(locationsElement);
            }
        }

        public List<Command> Commands { get; private set; }

        /*****************************************************************************************/
        protected ApplicationSettings()
        {
            Commands = new List<Command>();
        }

        /*****************************************************************************************/
        protected void Read(Context context, XmlElement element)
        {
            Option<XmlElement> commandsElement = MCAD.XmlCommon.XmlTools.SafeGetElement(element, "commands");
            if (commandsElement.IsSome)
            {
                foreach (XmlElement commandElement in MCAD.XmlCommon.XmlTools.GetElements(commandsElement.Get(), "command"))
                    Commands.Add(Command.Read(context, commandElement));
            }
        }

        /*****************************************************************************************/
        protected void Write(Context context, XmlDocument document, XmlElement element)
        {
            XmlElement commandsElement = document.CreateElement("commands");
            foreach (Command command in Commands)
            {
                XmlElement commandElement = document.CreateElement("command");
                command.Write(document, commandElement);
                commandsElement.AppendChild(commandElement);
            }
            element.AppendChild(commandsElement);
        }
    }
}
