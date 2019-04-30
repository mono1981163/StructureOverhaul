namespace VaultEagle
{
    partial class ShowSubscriptionsForm
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
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.TreeNode treeNode8 = new System.Windows.Forms.TreeNode("designs.ipj");
            System.Windows.Forms.TreeNode treeNode9 = new System.Windows.Forms.TreeNode("c");
            System.Windows.Forms.TreeNode treeNode10 = new System.Windows.Forms.TreeNode("bigfolder", new System.Windows.Forms.TreeNode[] {
            treeNode9});
            System.Windows.Forms.TreeNode treeNode11 = new System.Windows.Forms.TreeNode("file1_unzipped_by_vaulteagle.zip");
            System.Windows.Forms.TreeNode treeNode12 = new System.Windows.Forms.TreeNode("testfiles", new System.Windows.Forms.TreeNode[] {
            treeNode11});
            System.Windows.Forms.TreeNode treeNode13 = new System.Windows.Forms.TreeNode("designs", new System.Windows.Forms.TreeNode[] {
            treeNode12});
            System.Windows.Forms.TreeNode treeNode14 = new System.Windows.Forms.TreeNode("$", new System.Windows.Forms.TreeNode[] {
            treeNode8,
            treeNode10,
            treeNode13});
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ShowSubscriptionsForm));
            this.SubscriptionTree = new System.Windows.Forms.TreeView();
            this.SubscriptionImages = new System.Windows.Forms.ImageList(this.components);
            this.OKButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.treeContextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.inheritedToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.includeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.includeAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.includeFilesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.includeDirectSubfoldersToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.includeFoldersToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.onlyThisFolderToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.excludeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.removeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.syncNowButton = new System.Windows.Forms.Button();
            this.showLogButton = new System.Windows.Forms.Button();
            this.repairButton = new System.Windows.Forms.Button();
            this.treeContextMenuStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // SubscriptionTree
            // 
            this.SubscriptionTree.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.SubscriptionTree.ImageIndex = 0;
            this.SubscriptionTree.ImageList = this.SubscriptionImages;
            this.SubscriptionTree.Location = new System.Drawing.Point(12, 12);
            this.SubscriptionTree.Name = "SubscriptionTree";
            treeNode8.ImageIndex = 1;
            treeNode8.Name = "Node2";
            treeNode8.Text = "designs.ipj";
            treeNode9.ImageIndex = 3;
            treeNode9.Name = "Node4";
            treeNode9.Text = "c";
            treeNode10.ImageIndex = 4;
            treeNode10.Name = "Node3";
            treeNode10.Text = "bigfolder";
            treeNode11.ImageIndex = 1;
            treeNode11.Name = "Node7";
            treeNode11.Text = "file1_unzipped_by_vaulteagle.zip";
            treeNode12.ImageIndex = 6;
            treeNode12.Name = "Node6";
            treeNode12.Text = "testfiles";
            treeNode13.ImageIndex = 4;
            treeNode13.Name = "Node5";
            treeNode13.Text = "designs";
            treeNode14.ImageIndex = 4;
            treeNode14.Name = "Node0";
            treeNode14.Text = "$";
            this.SubscriptionTree.Nodes.AddRange(new System.Windows.Forms.TreeNode[] {
            treeNode14});
            this.SubscriptionTree.SelectedImageIndex = 0;
            this.SubscriptionTree.Size = new System.Drawing.Size(464, 440);
            this.SubscriptionTree.TabIndex = 0;
            this.SubscriptionTree.KeyDown += new System.Windows.Forms.KeyEventHandler(this.SubscriptionTree_KeyDown);
            this.SubscriptionTree.MouseUp += new System.Windows.Forms.MouseEventHandler(this.SubscriptionTree_MouseUp);
            // 
            // SubscriptionImages
            // 
            this.SubscriptionImages.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("SubscriptionImages.ImageStream")));
            this.SubscriptionImages.TransparentColor = System.Drawing.Color.Magenta;
            this.SubscriptionImages.Images.SetKeyName(0, "_page_white_red_cross.png");
            this.SubscriptionImages.Images.SetKeyName(1, "_page_white_add.png");
            this.SubscriptionImages.Images.SetKeyName(2, "_folder_gray_red_cross.png");
            this.SubscriptionImages.Images.SetKeyName(3, "folder_add.png");
            this.SubscriptionImages.Images.SetKeyName(4, "_folder_gray.png");
            this.SubscriptionImages.Images.SetKeyName(5, "folder.png");
            this.SubscriptionImages.Images.SetKeyName(6, "_folder_wrench.png");
            this.SubscriptionImages.Images.SetKeyName(7, "page_white_error.png");
            this.SubscriptionImages.Images.SetKeyName(8, "folder_error.png");
            // 
            // OKButton
            // 
            this.OKButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.OKButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.OKButton.Location = new System.Drawing.Point(320, 458);
            this.OKButton.Name = "OKButton";
            this.OKButton.Size = new System.Drawing.Size(75, 23);
            this.OKButton.TabIndex = 1;
            this.OKButton.Text = "Save";
            this.OKButton.UseVisualStyleBackColor = true;
            this.OKButton.Click += new System.EventHandler(this.OKButton_Click);
            // 
            // cancelButton
            // 
            this.cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.Location = new System.Drawing.Point(401, 458);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(75, 23);
            this.cancelButton.TabIndex = 2;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
            // 
            // treeContextMenuStrip
            // 
            this.treeContextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.inheritedToolStripMenuItem,
            this.includeToolStripMenuItem,
            this.excludeToolStripMenuItem,
            this.toolStripSeparator1,
            this.removeToolStripMenuItem});
            this.treeContextMenuStrip.Name = "contextMenuStrip1";
            this.treeContextMenuStrip.Size = new System.Drawing.Size(153, 120);
            // 
            // inheritedToolStripMenuItem
            // 
            this.inheritedToolStripMenuItem.Name = "inheritedToolStripMenuItem";
            this.inheritedToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.inheritedToolStripMenuItem.Text = "(like parent)";
            this.inheritedToolStripMenuItem.Click += new System.EventHandler(this.toolStripMenuItem_Click);
            // 
            // includeToolStripMenuItem
            // 
            this.includeToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.includeAllToolStripMenuItem,
            this.includeFilesToolStripMenuItem,
            this.includeDirectSubfoldersToolStripMenuItem,
            this.includeFoldersToolStripMenuItem,
            this.onlyThisFolderToolStripMenuItem});
            this.includeToolStripMenuItem.Name = "includeToolStripMenuItem";
            this.includeToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.includeToolStripMenuItem.Text = "Include";
            this.includeToolStripMenuItem.Click += new System.EventHandler(this.toolStripMenuItem_Click);
            // 
            // includeAllToolStripMenuItem
            // 
            this.includeAllToolStripMenuItem.Name = "includeAllToolStripMenuItem";
            this.includeAllToolStripMenuItem.Size = new System.Drawing.Size(218, 22);
            this.includeAllToolStripMenuItem.Text = "Include files and subfolders";
            this.includeAllToolStripMenuItem.Click += new System.EventHandler(this.toolStripMenuItem_Click);
            // 
            // includeFilesToolStripMenuItem
            // 
            this.includeFilesToolStripMenuItem.Name = "includeFilesToolStripMenuItem";
            this.includeFilesToolStripMenuItem.Size = new System.Drawing.Size(218, 22);
            this.includeFilesToolStripMenuItem.Text = "Include files only";
            this.includeFilesToolStripMenuItem.Click += new System.EventHandler(this.toolStripMenuItem_Click);
            // 
            // includeDirectSubfoldersToolStripMenuItem
            // 
            this.includeDirectSubfoldersToolStripMenuItem.Name = "includeDirectSubfoldersToolStripMenuItem";
            this.includeDirectSubfoldersToolStripMenuItem.Size = new System.Drawing.Size(218, 22);
            this.includeDirectSubfoldersToolStripMenuItem.Text = "Include direct subfolders";
            this.includeDirectSubfoldersToolStripMenuItem.Click += new System.EventHandler(this.toolStripMenuItem_Click);
            // 
            // includeFoldersToolStripMenuItem
            // 
            this.includeFoldersToolStripMenuItem.Name = "includeFoldersToolStripMenuItem";
            this.includeFoldersToolStripMenuItem.Size = new System.Drawing.Size(218, 22);
            this.includeFoldersToolStripMenuItem.Text = "Include folder structure";
            this.includeFoldersToolStripMenuItem.Click += new System.EventHandler(this.toolStripMenuItem_Click);
            // 
            // onlyThisFolderToolStripMenuItem
            // 
            this.onlyThisFolderToolStripMenuItem.Name = "onlyThisFolderToolStripMenuItem";
            this.onlyThisFolderToolStripMenuItem.Size = new System.Drawing.Size(218, 22);
            this.onlyThisFolderToolStripMenuItem.Text = "Only this folder";
            this.onlyThisFolderToolStripMenuItem.Click += new System.EventHandler(this.toolStripMenuItem_Click);
            // 
            // excludeToolStripMenuItem
            // 
            this.excludeToolStripMenuItem.Name = "excludeToolStripMenuItem";
            this.excludeToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.excludeToolStripMenuItem.Text = "Exclude";
            this.excludeToolStripMenuItem.Click += new System.EventHandler(this.toolStripMenuItem_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(149, 6);
            // 
            // removeToolStripMenuItem
            // 
            this.removeToolStripMenuItem.Name = "removeToolStripMenuItem";
            this.removeToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.removeToolStripMenuItem.Text = "Clear node";
            this.removeToolStripMenuItem.Click += new System.EventHandler(this.removeToolStripMenuItem_Click);
            // 
            // syncNowButton
            // 
            this.syncNowButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.syncNowButton.Location = new System.Drawing.Point(12, 458);
            this.syncNowButton.Name = "syncNowButton";
            this.syncNowButton.Size = new System.Drawing.Size(75, 23);
            this.syncNowButton.TabIndex = 3;
            this.syncNowButton.Text = "Sync now";
            this.syncNowButton.UseVisualStyleBackColor = true;
            this.syncNowButton.Click += new System.EventHandler(this.syncNowButton_Click);
            // 
            // showLogButton
            // 
            this.showLogButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.showLogButton.Location = new System.Drawing.Point(93, 458);
            this.showLogButton.Name = "showLogButton";
            this.showLogButton.Size = new System.Drawing.Size(78, 23);
            this.showLogButton.TabIndex = 4;
            this.showLogButton.Text = "Show log";
            this.showLogButton.UseVisualStyleBackColor = true;
            this.showLogButton.Click += new System.EventHandler(this.showLogButton_Click);
            // 
            // repairButton
            // 
            this.repairButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.repairButton.Location = new System.Drawing.Point(177, 458);
            this.repairButton.Name = "repairButton";
            this.repairButton.Size = new System.Drawing.Size(55, 23);
            this.repairButton.TabIndex = 5;
            this.repairButton.Text = "Repair";
            this.repairButton.UseVisualStyleBackColor = true;
            this.repairButton.Click += new System.EventHandler(this.repairButton_Click);
            // 
            // ShowSubscriptionsForm
            // 
            this.AcceptButton = this.OKButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.cancelButton;
            this.ClientSize = new System.Drawing.Size(488, 493);
            this.Controls.Add(this.repairButton);
            this.Controls.Add(this.syncNowButton);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.OKButton);
            this.Controls.Add(this.showLogButton);
            this.Controls.Add(this.SubscriptionTree);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimumSize = new System.Drawing.Size(430, 300);
            this.Name = "ShowSubscriptionsForm";
            this.Text = "Current Subscriptions";
            this.treeContextMenuStrip.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TreeView SubscriptionTree;
        private System.Windows.Forms.Button OKButton;
        private System.Windows.Forms.ImageList SubscriptionImages;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.ContextMenuStrip treeContextMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem excludeToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem removeToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem includeToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem inheritedToolStripMenuItem;
        private System.Windows.Forms.Button syncNowButton;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.Button showLogButton;
        private System.Windows.Forms.ToolStripMenuItem includeAllToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem includeFilesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem includeDirectSubfoldersToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem onlyThisFolderToolStripMenuItem;
        private System.Windows.Forms.Button repairButton;
        private System.Windows.Forms.ToolStripMenuItem includeFoldersToolStripMenuItem;
    }
}