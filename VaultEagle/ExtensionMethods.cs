using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

using Common.DotNet.Extensions;

namespace VaultEagle
{
    public static class WinformsExtensionMethods
    {
        public static IEnumerable<TreeNode> AsEnumerable(this TreeNodeCollection nodes)
        {
            return nodes.Cast<TreeNode>();
        }

        public static void DoThreadSafeSynchronous(this Control control, Action action)
        {
            if (control.InvokeRequired)
                control.Invoke(action);
            else
                action();
        }

        public static TResult DoThreadSafeSynchronous<TResult>(this Control control, Func<TResult> func)
        {
            if (control.InvokeRequired)
                return (TResult) control.Invoke(func);
            else
                return func();
        }

        public static void DoThreadSafeAsync(this Control control, Action action)
        {
            if (control.InvokeRequired)
                control.BeginInvoke(action); // execute async, Control.BeginInvoke executes calls in-order, queuing items on the message-pump
            else
                action();
        }
    }

    public static class SynchronizationTreeExtensionMethods
    {
        public static TreeNode ToTreeNode(this SynchronizationTree tree)
        {
            var set = new HashSet<string>();
            foreach (var path in tree.ExplicitPaths.Keys.ToList())
                set.UnionWith(SynchronizationTree.SubPaths(path));
            set.Remove("$/"); // special
            List<string> allPaths = set.ToList().Sorted(); // sort, with folders last

            Dictionary<string, TreeNode> pathToNodeDict = new Dictionary<string, TreeNode>();
            TreeNode root = CreateNode(tree, "$/", "$");
            pathToNodeDict["$/"] = root;
            foreach (string subPath in allPaths)
            {
                string folder, filename;
                SynchronizationTree.TrySplitPathIntoFolderAndChild(subPath, out folder, out filename);
                filename = filename.ToLowerInvariant();
                TreeNode node = CreateNode(tree, subPath, filename);
                pathToNodeDict[folder].Nodes.Add(node);
                pathToNodeDict[subPath] = node;
            }

            SortFilesBeforeFolders(root);

            return root;
        }

        private static void SortFilesBeforeFolders(TreeNode root)
        {
            foreach (var node in TreeUtils.Flatten(root, (t) => t.Nodes.AsEnumerable()).ToList())
            {
                var nodes = node.Nodes.AsEnumerable().ToList();
                var sorted = nodes.OrderBy(n => (ShowSubscriptionsForm.GetPath(n).EndsWith("/") ? "b " : "a ") + n.Name);
                node.Nodes.Clear();
                node.Nodes.AddRange(sorted.ToArray());
            }
        }

        private static TreeNode CreateNode(SynchronizationTree tree, string path, string name)
        {
            name = name.TrimEnd('/');
            int iconImageIndex = (int)tree.GetIconIndexForPath(path); // order matches that in ImageList

            TreeNode node = new TreeNode(name, iconImageIndex, iconImageIndex);
            node.Tag = path;

            if (tree.IsExcluded(path))
                node.ForeColor = System.Drawing.SystemColors.GrayText;

            return node;
        }

    }
}