namespace VaultEagle.Encrypter
{
    partial class EncryptionForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.DecryptedField = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.QuitButton = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.EncryptedField = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.InputField = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // DecryptedField
            // 
            this.DecryptedField.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.DecryptedField.Location = new System.Drawing.Point(113, 73);
            this.DecryptedField.Margin = new System.Windows.Forms.Padding(4);
            this.DecryptedField.Name = "DecryptedField";
            this.DecryptedField.ReadOnly = true;
            this.DecryptedField.Size = new System.Drawing.Size(247, 22);
            this.DecryptedField.TabIndex = 15;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(14, 78);
            this.label4.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(77, 17);
            this.label4.TabIndex = 14;
            this.label4.Text = "Decrypted:";
            // 
            // QuitButton
            // 
            this.QuitButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.QuitButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.QuitButton.Location = new System.Drawing.Point(260, 276);
            this.QuitButton.Margin = new System.Windows.Forms.Padding(4);
            this.QuitButton.Name = "QuitButton";
            this.QuitButton.Size = new System.Drawing.Size(100, 28);
            this.QuitButton.TabIndex = 10;
            this.QuitButton.Text = "Quit";
            this.QuitButton.UseVisualStyleBackColor = true;
            this.QuitButton.Click += new System.EventHandler(this.QuitButton_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(15, 46);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(76, 17);
            this.label3.TabIndex = 13;
            this.label3.Text = "Encrypted:";
            // 
            // EncryptedField
            // 
            this.EncryptedField.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.EncryptedField.Location = new System.Drawing.Point(113, 43);
            this.EncryptedField.Margin = new System.Windows.Forms.Padding(4);
            this.EncryptedField.Name = "EncryptedField";
            this.EncryptedField.ReadOnly = true;
            this.EncryptedField.Size = new System.Drawing.Size(247, 22);
            this.EncryptedField.TabIndex = 12;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(14, 18);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(39, 17);
            this.label2.TabIndex = 11;
            this.label2.Text = "Text:";
            // 
            // InputField
            // 
            this.InputField.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.InputField.Location = new System.Drawing.Point(113, 13);
            this.InputField.Margin = new System.Windows.Forms.Padding(4);
            this.InputField.Name = "InputField";
            this.InputField.Size = new System.Drawing.Size(247, 22);
            this.InputField.TabIndex = 9;
            this.InputField.TextChanged += new System.EventHandler(this.InputField_TextChanged);
            // 
            // EncryptionForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(373, 317);
            this.Controls.Add(this.DecryptedField);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.QuitButton);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.EncryptedField);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.InputField);
            this.Name = "EncryptionForm";
            this.Text = "Encrypter";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox DecryptedField;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Button QuitButton;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox EncryptedField;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox InputField;
    }
}