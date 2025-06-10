using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FileManagerSystem.Models;
using FileManagerSystem.Services;
using FileManagerSystem.Views.Dialogs;

namespace FileManagerSystem.Views
{
    public partial class MainWindow : Window
    {
        private FileSystemService _fileSystemService;
        private string _currentPath;
        private List<string> _pathHistory;
        private int _currentHistoryIndex;

        public MainWindow()
        {
            InitializeComponent();
            _fileSystemService = new FileSystemService();
            _pathHistory = new List<string>();
            _currentHistoryIndex = -1;
            
            // 初始化到根目录
            NavigateTo("\\");
        }

        private void NavigateTo(string path)
        {
            _currentPath = path;
            
            // 更新历史记录
            if (_currentHistoryIndex < _pathHistory.Count - 1)
            {
                _pathHistory.RemoveRange(_currentHistoryIndex + 1, _pathHistory.Count - _currentHistoryIndex - 1);
            }
            
            if (_pathHistory.Count == 0 || _pathHistory.Last() != path)
            {
                _pathHistory.Add(path);
                _currentHistoryIndex = _pathHistory.Count - 1;
            }
            
            // 更新UI
            PathTextBox.Text = path;
            LoadDirectoryContents();
            UpdateUI();
        }

        private void LoadDirectoryContents()
        {
            var contents = _fileSystemService.GetDirectoryContents(_currentPath);
            FileListView.Items.Clear();
            
            foreach (var fcb in contents.OrderBy(f => !f.IsDirectory).ThenBy(f => f.FileName))
            {
                var item = new FileListItem
                {
                    FCB = fcb,
                    DisplayName = (fcb.IsDirectory ? "📁 " : "📄 ") + fcb.FileName,
                    ModifiedTime = fcb.ModifiedTime.ToString("yyyy/MM/dd HH:mm"),
                    SizeText = fcb.IsDirectory ? "" : FormatFileSize(fcb.Size)
                };
                FileListView.Items.Add(item);
            }
        }

        private void UpdateUI()
        {
            ItemCountTextBlock.Text = $"项目: {FileListView.Items.Count}";
            
            var (total, used, free, percentage) = _fileSystemService.GetDiskUsage();
            DiskUsageTextBlock.Text = $"磁盘使用: {used}/{total} 块 ({percentage:F1}%)";
        }

        private string FormatFileSize(long bytes)
        {
            if (bytes == 0) return "0 字节";
            
            string[] suffixes = { "字节", "KB", "MB", "GB", "TB" };
            int suffixIndex = 0;
            double size = bytes;
            
            while (size >= 1024 && suffixIndex < suffixes.Length - 1)
            {
                size /= 1024;
                suffixIndex++;
            }
            
            return $"{size:F1} {suffixes[suffixIndex]}";
        }

        // 事件处理程序
        private void FileListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (FileListView.SelectedItem is FileListItem item)
            {
                if (item.FCB.IsDirectory)
                {
                    NavigateTo(item.FCB.FullPath);
                }
                else if (item.FCB.IsTextEditable())
                {
                    var content = _fileSystemService.GetFileContent(item.FCB.FullPath);
                    var editor = new TextEditorDialog(item.FCB, _fileSystemService, content);
                    if (editor.ShowDialog() == true)
                    {
                        LoadDirectoryContents();
                        UpdateUI();
                    }
                }
            }
        }

        private void GoBack_Click(object sender, RoutedEventArgs e)
        {
            if (_currentHistoryIndex > 0)
            {
                _currentHistoryIndex--;
                var path = _pathHistory[_currentHistoryIndex];
                _currentPath = path;
                PathTextBox.Text = path;
                LoadDirectoryContents();
                UpdateUI();
            }
        }

        private void GoForward_Click(object sender, RoutedEventArgs e)
        {
            if (_currentHistoryIndex < _pathHistory.Count - 1)
            {
                _currentHistoryIndex++;
                var path = _pathHistory[_currentHistoryIndex];
                _currentPath = path;
                PathTextBox.Text = path;
                LoadDirectoryContents();
                UpdateUI();
            }
        }

        private void Up_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPath != "\\")
            {
                var parentPath = System.IO.Path.GetDirectoryName(_currentPath);
                if (string.IsNullOrEmpty(parentPath))
                    parentPath = "\\";
                NavigateTo(parentPath);
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LoadDirectoryContents();
            UpdateUI();
        }

        private void NewTextFile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new InputDialog("新建文本文件", "请输入文件名:");
            if (dialog.ShowDialog() == true)
            {
                var fileName = dialog.Result;
                if (!fileName.EndsWith(".txt"))
                    fileName += ".txt";
                
                if (_fileSystemService.CreateFile(fileName, _currentPath))
                {
                    LoadDirectoryContents();
                    UpdateUI();
                }
                else
                {
                    MessageBox.Show("创建文件失败！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void NewFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new InputDialog("新建文件夹", "请输入文件夹名:");
            if (dialog.ShowDialog() == true)
            {
                if (_fileSystemService.CreateDirectory(dialog.Result, _currentPath))
                {
                    LoadDirectoryContents();
                    UpdateUI();
                }
                else
                {
                    MessageBox.Show("创建文件夹失败！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (FileListView.SelectedItem is FileListItem item)
            {
                var result = MessageBox.Show(
                    $"确定要删除 '{item.FCB.FileName}' 吗？",
                    "确认删除",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    if (_fileSystemService.Delete(item.FCB.FullPath))
                    {
                        LoadDirectoryContents();
                        UpdateUI();
                    }
                    else
                    {
                        MessageBox.Show("删除失败！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void Rename_Click(object sender, RoutedEventArgs e)
        {
            if (FileListView.SelectedItem is FileListItem item)
            {
                var dialog = new InputDialog("重命名", "请输入新名称:", item.FCB.FileName);
                if (dialog.ShowDialog() == true)
                {
                    if (_fileSystemService.Rename(item.FCB.FullPath, dialog.Result))
                    {
                        LoadDirectoryContents();
                        UpdateUI();
                    }
                    else
                    {
                        MessageBox.Show("重命名失败！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            if (FileListView.SelectedItem is FileListItem item)
            {
                if (item.FCB.IsDirectory)
                {
                    NavigateTo(item.FCB.FullPath);
                }
                else if (item.FCB.IsTextEditable())
                {
                    var content = _fileSystemService.GetFileContent(item.FCB.FullPath);
                    var editor = new TextEditorDialog(item.FCB, _fileSystemService, content);
                    if (editor.ShowDialog() == true)
                    {
                        LoadDirectoryContents();
                        UpdateUI();
                    }
                }
                else
                {
                    MessageBox.Show($"无法打开此类型的文件：{item.FCB.FileName}\n" +
                                  "当前只支持文本文件的编辑。", 
                                  "文件类型不支持", 
                                  MessageBoxButton.OK, 
                                  MessageBoxImage.Information);
                }
            }
        }

        private void Properties_Click(object sender, RoutedEventArgs e)
        {
            if (FileListView.SelectedItem is FileListItem item)
            {
                var dialog = new PropertiesDialog(item.FCB);
                dialog.ShowDialog();
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    // 辅助类用于ListView绑定
    public class FileListItem
    {
        public FCB FCB { get; set; }
        public string DisplayName { get; set; }
        public string ModifiedTime { get; set; }
        public string SizeText { get; set; }
    }
} 