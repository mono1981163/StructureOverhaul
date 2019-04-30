using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VaultEagle.Encrypter
{
    public partial class EncryptionForm : Form
    {
        string passPhrase = "Hades";
        public EncryptionForm()
        {
            InitializeComponent();
        }

        private void InputField_TextChanged(object sender, EventArgs e)
        {
            UpdateTexts();
        }

        private void UpdateTexts()
        {
            EncryptedField.Text = MCADCommon.EncryptionCommon.SimpleEncryption.EncryptString(InputField.Text, passPhrase);
            DecryptedField.Text = MCADCommon.EncryptionCommon.SimpleEncryption.DecryptString(EncryptedField.Text, passPhrase);
        }

        private void QuitButton_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
