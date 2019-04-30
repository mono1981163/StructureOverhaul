using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Common.DotNet.Extensions;

namespace VaultEagle
{
    partial class ShowSubscriptionsForm : Form
    {
        private SynchronizationTree tree;
        private Action<bool> startSyncThread;
        private readonly VaultCommunication vaultCom;

        public ShowSubscriptionsForm(SynchronizationTree tree, Action<bool> startSyncThread, VaultCommunication vaultCom)
        {
            this.tree = tree;
            this.startSyncThread = startSyncThread;
            this.vaultCom = vaultCom;

            InitializeComponent();
            SubscriptionTree.Nodes.Clear();

            removeToolStripMenuItem.ShortcutKeys = Keys.Delete;
            RedrawTree();
            SubscriptionTree.ExpandAll();
        }

        private void SubscriptionTree_MouseUp(object sender, MouseEventArgs e)
        {
            var folderToolStripMenuItems = new[]
            {
                new {state=SyncState.Include, menuItem=includeAllToolStripMenuItem},
                new {state=SyncState.IncludeOnlyFiles, menuItem=includeFilesToolStripMenuItem}, 
                new {state=SyncState.IncludeOnlyDirectChildFolders, menuItem=includeDirectSubfoldersToolStripMenuItem},
                new {state=SyncState.IncludeOnlyFolders, menuItem=includeFoldersToolStripMenuItem}, 
                new {state=SyncState.IncludeSingleFolder, menuItem=onlyThisFolderToolStripMenuItem}
            };
            var toolStripMenuItems = new[]
            {
                new {state=SyncState.Include, menuItem=includeToolStripMenuItem},
                new {state=SyncState.Exclude, menuItem=excludeToolStripMenuItem}, 
            };
            foreach (var x in folderToolStripMenuItems.Concat(toolStripMenuItems))
                x.menuItem.Tag = x.state;

            if (e.Button == MouseButtons.Right)
            {
                SubscriptionTree.SelectedNode = SubscriptionTree.GetNodeAt(e.X, e.Y);

                var n = SubscriptionTree.SelectedNode;
                if (n != null)
                {
                    foreach (var item in toolStripMenuItems
                                        .Concat(folderToolStripMenuItems)
                                        .Select(x => x.menuItem)
                                        .Concat(treeContextMenuStrip.Items.OfType<ToolStripMenuItem>()))
                    {
                        item.Checked = false;
                        item.Enabled = true;
                    }

                    var isFolder = tree.IsFolder(GetPath(n));
                    includeToolStripMenuItem.DropDownItems.Clear();
                    if (isFolder) // show dropdown
                        foreach (var item in folderToolStripMenuItems)
                            includeToolStripMenuItem.DropDownItems.Add(item.menuItem);

                    var state = tree.GetExplicitStateOfPath(GetPath(n));
                    if (state == null)
                        inheritedToolStripMenuItem.Checked = true;
                    else
                    {
                        foreach (var item in toolStripMenuItems.Concat(folderToolStripMenuItems))
                            if (item.menuItem.Tag != null && state == item.menuItem.Tag as SyncState?)
                                item.menuItem.Checked = true;
                        if (folderToolStripMenuItems.Any(x => x.menuItem.Checked))
                            includeToolStripMenuItem.Checked = true;
                    }
                }
                else
                    foreach (var item in treeContextMenuStrip.Items.OfType<ToolStripMenuItem>())
                    {
                        item.Enabled = false;
                        item.Checked = false;
                    }
                treeContextMenuStrip.Show(SubscriptionTree, e.Location);
            }
        }

        private void toolStripMenuItem_Click(object sender, EventArgs e)
        {
            treeContextMenuStrip.Close();
            string path = GetPath(SubscriptionTree.SelectedNode);
            var syncState = ((ToolStripItem)sender).Tag as SyncState?;
            if(!syncState.HasValue)
                tree.Reset(path);
            else
                tree.SetState(path, syncState.Value);
            RedrawTree();
        }

        private void removeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var root = SubscriptionTree.SelectedNode;
            if (root == null)
                return;

            foreach (var node in GetAllNodes(root))
            {
                string path = GetPath(node);
                if(path != null)
                    tree.Reset(path);
            }
            RedrawTree();
        }

        private void RedrawTree()
        {
            var expandedNodes = (from n in GetAllNodes() 
                                 where n.IsExpanded
                                 select GetPath(n)).ToSet();

            SubscriptionTree.Nodes.Clear();
            SubscriptionTree.Nodes.Add(tree.ToTreeNode());
            foreach (var node in GetAllNodes())
                if (expandedNodes.Contains(GetPath(node)))
                    node.Expand();
        }

        public static string GetPath(TreeNode n)
        {
            return n.Tag as string;
        }

        private IEnumerable<TreeNode> GetAllNodes()
        {
            return SubscriptionTree.Nodes.AsEnumerable().SelectMany(root => GetAllNodes(root));
        }

        private IEnumerable<TreeNode> GetAllNodes(TreeNode root)
        {
            return TreeUtils.Flatten(root, n => n.Nodes.Cast<TreeNode>());
        }

        private void OKButton_Click(object sender, EventArgs e)
        {
            tree.WriteTree();
            Close();
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void syncNowButton_Click(object sender, EventArgs e)
        {
            tree.WriteTree();
            startSyncThread(true);
        }

        private void SubscriptionTree_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.Delete)
            {
                removeToolStripMenuItem.PerformClick();
                e.Handled = true;
            }
        }

        private void showLogButton_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(FilesAndFolders.GetLogPath());
        }

        private void repairButton_Click(object sender, EventArgs e)
        {
            var synchronizationTree = tree;
            vaultCom.TryRepairSynchronizationTree(synchronizationTree);

            RedrawTree();

            HashSet<string> missingFolders;
            HashSet<string> missingFiles;
            vaultCom.UpdateLastVaultId(synchronizationTree, out missingFolders, out missingFiles);

            foreach (var node in GetAllNodes())
            {
                string path = GetPath(node);
                if (missingFolders.Contains(path))
                    node.ImageIndex = (int) SynchronizationTree.ImageIndex.FolderError;
                if (missingFiles.Contains(path))
                    node.ImageIndex = (int) SynchronizationTree.ImageIndex.FileError;
            }
        }
    }
}
