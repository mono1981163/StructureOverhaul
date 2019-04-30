using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Common.DotNet.Extensions
{
    namespace Winforms
    {
        public static class WrapperExtensions
        {
            public static ITreeNode Wrap(this TreeNode source)
            {
                if (source == null)
                    return null;
                return new TreeNodeWrapper(source);
            }

            public static ITreeNode Wrap(this TreeView source)
            {
                if (source == null)
                    return null;
                return new TreeViewAsTreeNodeWrapper(source);
            }
        }

        public interface ITreeNode
        {
            /// <summary>
            /// Gets the parent tree node of the current tree node.
            /// </summary>
            /// <returns>
            /// An <see cref="T:Common.DotNet.Extensions.Winforms.ITreeNode"/> that represents the parent of the current tree node.
            /// </returns>
            /// <filterpriority>1</filterpriority>
            ITreeNode Parent { get; }

            /// <summary>
            /// Gets or sets the background color of the tree node.
            /// </summary>
            /// <returns>
            /// The background <see cref="T:System.Drawing.Color"/> of the tree node. The default is <see cref="F:System.Drawing.Color.Empty"/>.
            /// </returns>
            /// <filterpriority>1</filterpriority><PermissionSet><IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/></PermissionSet>
            Color BackColor { get; set; }

            /// <summary>
            /// Gets the bounds of the tree node.
            /// </summary>
            /// <returns>
            /// The <see cref="T:System.Drawing.Rectangle"/> that represents the bounds of the tree node.
            /// </returns>
            /// <filterpriority>1</filterpriority><PermissionSet><IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/><IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/><IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence"/><IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/></PermissionSet>
            Rectangle Bounds { get; }

            /// <summary>
            /// Gets the shortcut menu associated with this tree node.
            /// </summary>
            /// <returns>
            /// The <see cref="T:System.Windows.Forms.ContextMenu"/> associated with the tree node.
            /// </returns>
            /// <filterpriority>2</filterpriority>
            ContextMenu ContextMenu { get; set; }

            /// <summary>
            /// Gets or sets the shortcut menu associated with this tree node.
            /// </summary>
            /// <returns>
            /// The <see cref="T:System.Windows.Forms.ContextMenuStrip"/> associated with the tree node.
            /// </returns>
            ContextMenuStrip ContextMenuStrip { get; set; }

            /// <summary>
            /// Gets or sets the foreground color of the tree node.
            /// </summary>
            /// <returns>
            /// The foreground <see cref="T:System.Drawing.Color"/> of the tree node.
            /// </returns>
            /// <filterpriority>1</filterpriority><PermissionSet><IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/></PermissionSet>
            Color ForeColor { get; set; }

            /// <summary>
            /// Gets the handle of the tree node.
            /// </summary>
            /// <returns>
            /// The tree node handle.
            /// </returns>
            /// <filterpriority>2</filterpriority><PermissionSet><IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/><IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/><IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence"/><IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/></PermissionSet>
            IntPtr Handle { get; }

            /// <summary>
            /// Gets or sets the image list index value of the image displayed when the tree node is in the unselected state.
            /// </summary>
            /// <returns>
            /// A zero-based index value that represents the image position in the assigned <see cref="T:System.Windows.Forms.ImageList"/>.
            /// </returns>
            /// <filterpriority>2</filterpriority><PermissionSet><IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/><IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/><IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence"/><IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/></PermissionSet>
            int ImageIndex { get; set; }

            /// <summary>
            /// Gets or sets the key for the image associated with this tree node when the node is in an unselected state.
            /// </summary>
            /// <returns>
            /// The key for the image associated with this tree node when the node is in an unselected state.
            /// </returns>
            /// <filterpriority>2</filterpriority><PermissionSet><IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/><IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/><IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence"/><IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/></PermissionSet>
            string ImageKey { get; set; }

            /// <summary>
            /// Gets or sets the name of the tree node.
            /// </summary>
            /// <returns>
            /// A <see cref="T:System.String"/> that represents the name of the tree node.
            /// </returns>
            /// <filterpriority>1</filterpriority>
            string Name { get; set; }

            /// <summary>
            /// Gets the collection of <see cref="T:System.Windows.Forms.TreeNode"/> objects assigned to the current tree node.
            /// </summary>
            /// <returns>
            /// A <see cref="T:System.Windows.Forms.TreeNodeCollection"/> that represents the tree nodes assigned to the current tree node.
            /// </returns>
            /// <filterpriority>1</filterpriority>
            TreeNodeCollection Nodes { get; }

            /// <summary>
            /// Gets or sets the image list index value of the image that is displayed when the tree node is in the selected state.
            /// </summary>
            /// <returns>
            /// A zero-based index value that represents the image position in an <see cref="T:System.Windows.Forms.ImageList"/>.
            /// </returns>
            /// <filterpriority>1</filterpriority><PermissionSet><IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/><IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/><IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence"/><IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/></PermissionSet>
            int SelectedImageIndex { get; set; }

            /// <summary>
            /// Gets or sets the key of the image displayed in the tree node when it is in a selected state.
            /// </summary>
            /// <returns>
            /// The key of the image displayed when the tree node is in a selected state.
            /// </returns>
            /// <filterpriority>1</filterpriority><PermissionSet><IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/><IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/><IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence"/><IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/></PermissionSet>
            string SelectedImageKey { get; set; }

            /// <summary>
            /// Gets or sets the object that contains data about the tree node.
            /// </summary>
            /// <returns>
            /// An <see cref="T:System.Object"/> that contains data about the tree node. The default is null.
            /// </returns>
            /// <filterpriority>1</filterpriority>
            object Tag { get; set; }

            /// <summary>
            /// Gets or sets the text displayed in the label of the tree node.
            /// </summary>
            /// <returns>
            /// The text displayed in the label of the tree node.
            /// </returns>
            /// <filterpriority>1</filterpriority><PermissionSet><IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/><IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/><IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence"/><IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/></PermissionSet>
            string Text { get; set; }

            /// <summary>
            /// Gets the parent tree view that the tree node is assigned to.
            /// </summary>
            /// <returns>
            /// A <see cref="T:System.Windows.Forms.TreeView"/> that represents the parent tree view that the tree node is assigned to, or null if the node has not been assigned to a tree view.
            /// </returns>
            /// <filterpriority>1</filterpriority>
            TreeView TreeView { get; }

            /// <summary>
            /// Gets the path from the root tree node to the current tree node.
            /// </summary>
            /// <returns>
            /// The path from the root tree node to the current tree node.
            /// </returns>
            /// <exception cref="T:System.InvalidOperationException">The node is not contained in a <see cref="T:System.Windows.Forms.TreeView"/>.</exception><filterpriority>1</filterpriority>
            string FullPath { get; }

            /// <summary>
            /// Gets a value indicating whether the tree node is in the expanded state.
            /// </summary>
            /// <returns>
            /// true if the tree node is in the expanded state; otherwise, false.
            /// </returns>
            /// <filterpriority>1</filterpriority><PermissionSet><IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/><IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/><IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence"/><IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/></PermissionSet>
            bool IsExpanded { get; }

            /// <summary>
            /// Gets the zero-based depth of the tree node in the <see cref="T:System.Windows.Forms.TreeView"/> control.
            /// </summary>
            /// <returns>
            /// The zero-based depth of the tree node in the <see cref="T:System.Windows.Forms.TreeView"/> control.
            /// </returns>
            /// <filterpriority>1</filterpriority>
            int Level { get; }

            /// <summary>
            /// Gets the next sibling tree node.
            /// </summary>
            /// <returns>
            /// A <see cref="T:System.Windows.Forms.TreeNode"/> that represents the next sibling tree node.
            /// </returns>
            /// <filterpriority>1</filterpriority>
            TreeNode NextNode { get; }

            /// <summary>
            /// Gets the next visible tree node.
            /// </summary>
            /// <returns>
            /// A <see cref="T:System.Windows.Forms.TreeNode"/> that represents the next visible tree node.
            /// </returns>
            /// <filterpriority>1</filterpriority><PermissionSet><IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/><IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/><IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence"/><IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/></PermissionSet>
            TreeNode NextVisibleNode { get; }

            /// <summary>
            /// Gets the previous sibling tree node.
            /// </summary>
            /// <returns>
            /// A <see cref="T:System.Windows.Forms.TreeNode"/> that represents the previous sibling tree node.
            /// </returns>
            /// <filterpriority>1</filterpriority>
            TreeNode PrevNode { get; }

            /// <summary>
            /// Gets the previous visible tree node.
            /// </summary>
            /// <returns>
            /// A <see cref="T:System.Windows.Forms.TreeNode"/> that represents the previous visible tree node.
            /// </returns>
            /// <filterpriority>1</filterpriority><PermissionSet><IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/><IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/><IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence"/><IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/></PermissionSet>
            TreeNode PrevVisibleNode { get; }

            /// <summary>
            /// Gets a value indicating whether the tree node is visible or partially visible.
            /// </summary>
            /// <returns>
            /// true if the tree node is visible or partially visible; otherwise, false.
            /// </returns>
            /// <filterpriority>1</filterpriority><PermissionSet><IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/><IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/><IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence"/><IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/></PermissionSet>
            bool IsVisible { get; }

            /// <summary>
            /// Gets a value indicating whether the tree node is in the selected state.
            /// </summary>
            /// <returns>
            /// true if the tree node is in the selected state; otherwise, false.
            /// </returns>
            /// <filterpriority>1</filterpriority><PermissionSet><IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/><IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/><IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence"/><IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/></PermissionSet>
            bool IsSelected { get; }

            /// <summary>
            /// Expands all the child tree nodes.
            /// </summary>
            /// <filterpriority>1</filterpriority><PermissionSet><IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/><IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/><IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence"/><IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/></PermissionSet>
            void ExpandAll();

            /// <summary>
            /// Returns the number of child tree nodes.
            /// </summary>
            /// <returns>
            /// The number of child tree nodes assigned to the <see cref="P:System.Windows.Forms.TreeNode.Nodes"/> collection.
            /// </returns>
            /// <param name="includeSubTrees">true if the resulting count includes all tree nodes indirectly rooted at this tree node; otherwise, false. </param><filterpriority>1</filterpriority>
            int GetNodeCount(bool includeSubTrees);

            /// <summary>
            /// Expands the tree node.
            /// </summary>
            /// <filterpriority>1</filterpriority><PermissionSet><IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/><IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/><IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence"/><IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/></PermissionSet>
            void Expand();

            /// <summary>
            /// Ensures that the tree node is visible, expanding tree nodes and scrolling the tree view control as necessary.
            /// </summary>
            /// <filterpriority>1</filterpriority><PermissionSet><IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/><IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/><IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence"/><IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/></PermissionSet>
            void EnsureVisible();

            /// <summary>
            /// Collapses the tree node.
            /// </summary>
            /// <filterpriority>1</filterpriority><PermissionSet><IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/><IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/><IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence"/><IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/></PermissionSet>
            void Collapse();

            /// <summary>
            /// Toggles the tree node to either the expanded or collapsed state.
            /// </summary>
            /// <filterpriority>1</filterpriority><PermissionSet><IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/><IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/><IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence"/><IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/></PermissionSet>
            void Toggle();

            /// <summary>
            /// Collapses the <see cref="T:System.Windows.Forms.TreeNode"/> and optionally collapses its children.
            /// </summary>
            /// <param name="ignoreChildren">true to leave the child nodes in their current state; false to collapse the child nodes.</param>
            void Collapse(bool ignoreChildren);
        }

        public class TreeViewAsTreeNodeWrapper : ITreeNode
        {
            private readonly TreeView _source;

            public TreeViewAsTreeNodeWrapper(TreeView source)
            {
                _source = source;
            }

            public Color BackColor
            {
                get { return _source.BackColor; }
                set { _source.BackColor = value; }
            }

            public Rectangle Bounds
            {
                get { return _source.Bounds; }
                set { _source.Bounds = value; }
            }

            public ContextMenu ContextMenu
            {
                get { return _source.ContextMenu; }
                set { _source.ContextMenu = value; }
            }

            public ContextMenuStrip ContextMenuStrip
            {
                get { return _source.ContextMenuStrip; }
                set { _source.ContextMenuStrip = value; }
            }

            public TreeView TreeView
            {
                get { return _source; }
            }

            public string FullPath
            {
                get { return ""; }
            }

            public bool IsExpanded
            {
                get { return true; }
            }

            public int Level
            {
                get { return -1; }
            }

            public TreeNode NextNode
            {
                get { return null; }
            }

            public TreeNode NextVisibleNode
            {
                get { return null; }
            }

            public TreeNode PrevNode
            {
                get { return null; }
            }

            public TreeNode PrevVisibleNode
            {
                get { return null; }
            }

            public bool IsVisible
            {
                get { return _source.VisibleCount > 0; }
            }

            public bool IsSelected
            {
                get { return false; }
            }

            public void ExpandAll()
            {
                _source.ExpandAll();
            }

            public Color ForeColor
            {
                get { return _source.ForeColor; }
                set { _source.ForeColor = value; }
            }

            public int GetNodeCount(bool includeSubTrees)
            {
                return _source.GetNodeCount(includeSubTrees);
            }

            public void Expand()
            {
            }

            public void EnsureVisible()
            {
            }

            public void Collapse()
            {
            }

            public void Toggle()
            {
            }

            public void Collapse(bool ignoreChildren)
            {
                foreach (var node in _source.Nodes.AsEnumerable())
                    node.Collapse(ignoreChildren);
            }

            public IntPtr Handle
            {
                get { return _source.Handle; }
            }

            public int ImageIndex
            {
                get { return _source.ImageIndex; }
                set { _source.ImageIndex = value; }
            }

            public string ImageKey
            {
                get { return _source.ImageKey; }
                set { _source.ImageKey = value; }
            }

            public string Name
            {
                get { return _source.Name; }
                set { _source.Name = value; }
            }

            public TreeNodeCollection Nodes
            {
                get { return _source.Nodes; }
            }

            public ITreeNode Parent
            {
                get { return null; }
            }

            public int SelectedImageIndex
            {
                get { return _source.SelectedImageIndex; }
                set { _source.SelectedImageIndex = value; }
            }

            public string SelectedImageKey
            {
                get { return _source.SelectedImageKey; }
                set { _source.SelectedImageKey = value; }
            }

            public object Tag
            {
                get { return _source.Tag; }
                set { _source.Tag = value; }
            }

            public string Text
            {
                get { return _source.Text; }
                set { _source.Text = value; }
            }
        }

        public class TreeNodeWrapper : ITreeNode
        {
            private readonly TreeNode _source;

            public bool IsExpanded
            {
                get { return _source.IsExpanded; }
            }

            public int Level
            {
                get { return _source.Level; }
            }

            public void Expand()
            {
                _source.Expand();
            }

            public void EnsureVisible()
            {
                _source.EnsureVisible();
            }

            public void Collapse()
            {
                _source.Collapse();
            }

            public TreeNode NextNode
            {
                get { return _source.NextNode; }
            }

            public TreeNode NextVisibleNode
            {
                get { return _source.NextVisibleNode; }
            }

            public TreeNode PrevNode
            {
                get { return _source.PrevNode; }
            }

            public TreeNode PrevVisibleNode
            {
                get { return _source.PrevVisibleNode; }
            }

            public bool IsVisible
            {
                get { return _source.IsVisible; }
            }

            public bool IsSelected
            {
                get { return _source.IsSelected; }
            }

            public void Toggle()
            {
                _source.Toggle();
            }

            public void Collapse(bool ignoreChildren)
            {
                _source.Collapse(ignoreChildren);
            }

            public string FullPath
            {
                get { return _source.FullPath; }
            }

            public TreeNodeWrapper(TreeNode source)
            {
                _source = source;
            }

            public Color BackColor
            {
                get { return _source.BackColor; }
                set { _source.BackColor = value; }
            }

            public Rectangle Bounds
            {
                get { return _source.Bounds; }
            }

            public ContextMenu ContextMenu
            {
                get { return _source.ContextMenu; }
                set { _source.ContextMenu = value; }
            }

            public ContextMenuStrip ContextMenuStrip
            {
                get { return _source.ContextMenuStrip; }
                set { _source.ContextMenuStrip = value; }
            }

            public TreeView TreeView
            {
                get { return _source.TreeView; }
            }

            public void ExpandAll()
            {
                _source.ExpandAll();
            }

            public Color ForeColor
            {
                get { return _source.ForeColor; }
                set { _source.ForeColor = value; }
            }

            public int GetNodeCount(bool includeSubTrees)
            {
                return _source.GetNodeCount(includeSubTrees);
            }

            public IntPtr Handle
            {
                get { return _source.Handle; }
            }

            public int ImageIndex
            {
                get { return _source.ImageIndex; }
                set { _source.ImageIndex = value; }
            }

            public string ImageKey
            {
                get { return _source.ImageKey; }
                set { _source.ImageKey = value; }
            }

            public string Name
            {
                get { return _source.Name; }
                set { _source.Name = value; }
            }

            public TreeNodeCollection Nodes
            {
                get { return _source.Nodes; }
            }

            public ITreeNode Parent
            {
                get { return _source.Parent.Wrap() ?? _source.TreeView.Wrap(); }
            }

            public int SelectedImageIndex
            {
                get { return _source.SelectedImageIndex; }
                set { _source.SelectedImageIndex = value; }
            }

            public string SelectedImageKey
            {
                get { return _source.SelectedImageKey; }
                set { _source.SelectedImageKey = value; }
            }

            public object Tag
            {
                get { return _source.Tag; }
                set { _source.Tag = value; }
            }

            public string Text
            {
                get { return _source.Text; }
                set { _source.Text = value; }
            }
        }
    }
}
