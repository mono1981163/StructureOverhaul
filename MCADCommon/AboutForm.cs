using Common.DotNet.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MCADCommon
{
    public partial class AboutForm : Form
    {
        public AboutForm(string title, string description, string version, Option<Image> image = new Option<Image>(), Option<Icon> icon = new Option<Icon>())
        {
            InitializeComponent();

            TitleLabel.Text = title;
            DescriptionField.Text = description;
            VersionLabel.Text = version;

            if (image.IsSome)
                ImageBox.Image = image.Get();
            if (icon.IsSome)
                Icon = icon.Get();
        }

        private void CloseButton_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
