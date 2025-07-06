using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Microsoft.Win32;

public class FontManagerForm : Form
{
    private TreeView fileTree;
    private Button refreshButton;
    private Button expandToggleButton;
    private Button configButton;

    private string fontRoot;
    private const string DefaultFontRootRaw = @"%LOCALAPPDATA%\Microsoft\Windows\Fonts";
    private const string ConfigFileName = "config.ini";

    private const string RegistryBase = "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Fonts";
    private bool expandedAll = false;

    // 正则表达式匹配字体文件
    private static readonly Regex fontFileRegex = new Regex(@"\.(ttf|otf|ttc)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public FontManagerForm()
    {
        this.Text = "Font Registry Manager";
        this.Width = 800;
        this.Height = 600;

        fontRoot = GetFontRootFromConfig();

        FlowLayoutPanel panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 35,
            AutoSize = true
        };

        refreshButton = new Button { Text = "刷新注册表状态", AutoSize = true };
        refreshButton.Click += (s, e) =>
        {
            fileTree.Nodes.Clear();
            LoadFontTree();
            ExpandToBeforeLeafFolders(fileTree.Nodes);
            expandedAll = false;
            expandToggleButton.Text = "展开全部";
        };

        expandToggleButton = new Button { Text = "展开全部", AutoSize = true };
        expandToggleButton.Click += (s, e) =>
        {
            if (expandedAll)
            {
                fileTree.CollapseAll();
                ExpandToBeforeLeafFolders(fileTree.Nodes);
                expandToggleButton.Text = "展开全部";
                expandedAll = false;
            }
            else
            {
                fileTree.ExpandAll();
                expandToggleButton.Text = "折叠到目录层";
                expandedAll = true;
            }
        };

        configButton = new Button { Text = "设置字体目录", AutoSize = true };
        configButton.Click += (s, e) =>
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "请选择字体目录";
                dialog.SelectedPath = fontRoot;

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    fontRoot = dialog.SelectedPath;
                    SaveFontRootToConfig(fontRoot);
                    MessageBox.Show("字体目录已保存，目录树已刷新。", "设置成功", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // 立即刷新目录树
                    fileTree.Nodes.Clear();
                    LoadFontTree();
                    ExpandToBeforeLeafFolders(fileTree.Nodes);
                    expandedAll = false;
                    expandToggleButton.Text = "展开全部";
                }
            }
        };

        panel.Controls.Add(refreshButton);
        panel.Controls.Add(expandToggleButton);
        panel.Controls.Add(configButton);

        fileTree = new TreeView { Dock = DockStyle.Fill, CheckBoxes = true };
        fileTree.AfterCheck += FileTree_AfterCheck;

        Controls.Add(fileTree);
        Controls.Add(panel);

        LoadFontTree();
        ExpandToBeforeLeafFolders(fileTree.Nodes);
    }

    private string GetFontRootFromConfig()
    {
        string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigFileName);
        string defaultExpanded = Environment.ExpandEnvironmentVariables(DefaultFontRootRaw);

        if (!File.Exists(configPath))
        {
            try
            {
                File.WriteAllText(configPath, "[Settings]\nFontRoot=" + DefaultFontRootRaw);
            }
            catch { }
            return defaultExpanded;
        }

        try
        {
            foreach (var line in File.ReadAllLines(configPath))
            {
                if (line.StartsWith("FontRoot=", StringComparison.OrdinalIgnoreCase))
                {
                    string raw = line.Substring("FontRoot=".Length).Trim();
                    return Environment.ExpandEnvironmentVariables(raw);
                }
            }
        }
        catch { }

        return defaultExpanded;
    }

    private void SaveFontRootToConfig(string path)
    {
        string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigFileName);
        try
        {
            File.WriteAllText(configPath, "[Settings]\nFontRoot=" + path);
        }
        catch { }
    }

    private void LoadFontTree()
    {
        var regFontPaths = LoadRegistryFontPaths();
        DirectoryInfo rootDir = new DirectoryInfo(fontRoot);
        if (!rootDir.Exists)
        {
            MessageBox.Show($"字体目录不存在：{fontRoot}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }
        TreeNode rootNode = CreateDirectoryNode(rootDir, regFontPaths);
        fileTree.Nodes.Add(rootNode);
    }

    private TreeNode CreateDirectoryNode(DirectoryInfo directory, Dictionary<string, string> regFontPaths)
    {
        TreeNode dirNode = new TreeNode(directory.Name) { Tag = directory.FullName };

        bool allChecked = true;
        bool hasChild = false;

        var fontFiles = directory.GetFiles()
            .Where(f => fontFileRegex.IsMatch(f.Name));

        foreach (var file in fontFiles)
        {
            string fullPath = file.FullName;
            TreeNode fileNode = new TreeNode(file.Name) { Tag = fullPath };

            bool isChecked = regFontPaths.ContainsValue(fullPath);
            fileNode.Checked = isChecked;
            if (!isChecked) allChecked = false;

            hasChild = true;
            dirNode.Nodes.Add(fileNode);
        }

        foreach (var subDir in directory.GetDirectories())
        {
            TreeNode childNode = CreateDirectoryNode(subDir, regFontPaths);
            dirNode.Nodes.Add(childNode);
            if (!childNode.Checked) allChecked = false;
            hasChild = true;
        }

        dirNode.Checked = hasChild && allChecked;
        return dirNode;
    }

    private Dictionary<string, string> LoadRegistryFontPaths()
    {
        var result = new Dictionary<string, string>();
        void ReadKey(string keyPath)
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(keyPath))
            {
                if (key == null) return;
                foreach (var valueName in key.GetValueNames())
                {
                    var value = key.GetValue(valueName) as string;
                    if (!string.IsNullOrEmpty(value))
                        result[valueName] = Environment.ExpandEnvironmentVariables(value);
                }
                foreach (var subKey in key.GetSubKeyNames())
                    ReadKey(keyPath + "\\" + subKey);
            }
        }

        ReadKey(RegistryBase);
        return result;
    }

    private void FileTree_AfterCheck(object sender, TreeViewEventArgs e)
    {
        if (e.Action != TreeViewAction.ByMouse) return;

        fileTree.AfterCheck -= FileTree_AfterCheck;

        string path = e.Node.Tag as string;
        bool isChecked = e.Node.Checked;

        if (Directory.Exists(path))
        {
            SetChildNodesChecked(e.Node, isChecked);
            ApplyRegistryForNodeRecursive(e.Node, isChecked);
        }
        else if (File.Exists(path) && IsFontFile(path))
        {
            UpdateRegistryForFont(path, isChecked);
            var parent = e.Node.Parent;
            if (!isChecked && parent != null)
            {
                CleanRegistryAncestorsIfEmpty(parent);
            }
        }

        UpdateParentCheckedState(e.Node.Parent);
        fileTree.AfterCheck += FileTree_AfterCheck;
    }

    private void SetChildNodesChecked(TreeNode node, bool isChecked)
    {
        foreach (TreeNode child in node.Nodes)
        {
            child.Checked = isChecked;
            SetChildNodesChecked(child, isChecked);
        }
    }

    private void ApplyRegistryForNodeRecursive(TreeNode node, bool isChecked)
    {
        string path = node.Tag as string;
        if (path == null) return;

        if (File.Exists(path) && IsFontFile(path))
        {
            UpdateRegistryForFont(path, isChecked);
        }

        foreach (TreeNode child in node.Nodes)
        {
            ApplyRegistryForNodeRecursive(child, isChecked);
        }

        if (!isChecked)
        {
            CleanRegistryAncestorsIfEmpty(node);
        }
    }

    private void CleanRegistryAncestorsIfEmpty(TreeNode node)
    {
        while (node != null)
        {
            if (HasAnyChildChecked(node)) return;

            DeleteRegistryKeyForDirectory(node);

            string path = node.Tag as string;
            if (path != null)
            {
                string relative = GetRelativePath(fontRoot, path);
                if (string.IsNullOrEmpty(relative) || relative.StartsWith(".."))
                    return;
            }

            node = node.Parent;
        }
    }

    private bool HasAnyChildChecked(TreeNode node)
    {
        foreach (TreeNode child in node.Nodes)
        {
            if (child.Checked || HasAnyChildChecked(child))
                return true;
        }
        return false;
    }

    private void DeleteRegistryKeyForDirectory(TreeNode node)
    {
        // 已不再使用子键，无需删除目录项
    }

    private void UpdateRegistryForFont(string fontPath, bool add)
    {
        string fontName = Path.GetFileNameWithoutExtension(fontPath) + " (custom)";
        using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RegistryBase))
        {
            if (add)
                key.SetValue(fontName, fontPath, RegistryValueKind.String);
            else
                key.DeleteValue(fontName, false);
        }
    }

    private bool IsFontFile(string path)
    {
        return fontFileRegex.IsMatch(path);
    }

    private void UpdateParentCheckedState(TreeNode? parent)
    {
        while (parent != null)
        {
            bool allChecked = true;
            foreach (TreeNode child in parent.Nodes)
            {
                if (!child.Checked)
                {
                    allChecked = false;
                    break;
                }
            }

            parent.Checked = allChecked;
            parent = parent.Parent;
        }
    }

    private void ExpandToBeforeLeafFolders(TreeNodeCollection nodes)
    {
        foreach (TreeNode node in nodes)
        {
            if (node.Nodes.Count == 0) continue;

            bool hasSubFolder = node.Nodes.Cast<TreeNode>().Any(child =>
            {
                string path = child.Tag as string;
                return path != null && Directory.Exists(path);
            });

            if (hasSubFolder)
            {
                node.Expand();
                ExpandToBeforeLeafFolders(node.Nodes);
            }
            else
            {
                node.Collapse(); // 不展开字体文件节点
            }
        }
    }

    private string GetRelativePath(string basePath, string fullPath)
    {
        Uri baseUri = new Uri(basePath.EndsWith("\\") ? basePath : basePath + "\\");
        Uri fullUri = new Uri(fullPath);
        return Uri.UnescapeDataString(baseUri.MakeRelativeUri(fullUri).ToString()).Replace('/', '\\');
    }

    [STAThread]
    public static void Main()
    {
        Application.EnableVisualStyles();
        Application.Run(new FontManagerForm());
    }
}
