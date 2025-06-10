using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using FileManagerSystem.Models;
using FileManagerSystem.Services;

namespace FileManagerSystem.Views.Dialogs
{
    public partial class TextEditorDialog : Window, INotifyPropertyChanged
    {
        private readonly FileSystemService _fileSystemService;
        private readonly FCB _fcb;
        private string _content;
        private string _windowTitle;
        private bool _isModified;

        public string Content
        {
            get => _content;
            set
            {
                _content = value;
                OnPropertyChanged();
                UpdateStatus();
            }
        }

        public string WindowTitle
        {
            get => _windowTitle;
            set
            {
                _windowTitle = value;
                OnPropertyChanged();
            }
        }

        public bool IsModified
        {
            get => _isModified;
            set
            {
                _isModified = value;
                UpdateTitle();
            }
        }

        public TextEditorDialog(FCB fcb, FileSystemService fileSystemService, string initialContent = "")
        {
            InitializeComponent();
            DataContext = this;
            
            _fcb = fcb;
            _fileSystemService = fileSystemService;
            _content = initialContent;
            _isModified = false;
            
            ContentTextBox.Text = initialContent;
            
            UpdateTitle();
            UpdateStatus();
            
            Loaded += (s, e) => ContentTextBox.Focus();
        }

        private void UpdateTitle()
        {
            var modifiedIndicator = _isModified ? "*" : "";
            WindowTitle = $"文本编辑器 - {_fcb.FileName}{modifiedIndicator}";
        }

        private void UpdateStatus()
        {
            var currentText = ContentTextBox?.Text ?? _content ?? "";
            CharCountTextBlock.Text = $"字符数: {currentText.Length}";
            
            if (ContentTextBox != null)
            {
                var caretIndex = ContentTextBox.CaretIndex;
                var textBeforeCaret = currentText.Substring(0, Math.Min(caretIndex, currentText.Length));
                var lines = textBeforeCaret.Split('\n');
                var line = lines.Length;
                var column = lines[lines.Length - 1].Length + 1;
                
                LineColumnTextBlock.Text = $"行: {line}, 列: {column}";
            }
        }

        private void ContentTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _content = ContentTextBox.Text;
            IsModified = true;
            UpdateStatus();
        }

        private void New_Click(object sender, RoutedEventArgs e)
        {
            if (ConfirmUnsavedChanges())
            {
                Content = "";
                IsModified = false;
                StatusTextBlock.Text = "新建文档";
            }
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            StatusTextBlock.Text = "打开功能暂未实现";
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            SaveFile();
        }

        private void Cut_Click(object sender, RoutedEventArgs e)
        {
            ContentTextBox.Cut();
            StatusTextBlock.Text = "已剪切";
        }

        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            ContentTextBox.Copy();
            StatusTextBlock.Text = "已复制";
        }

        private void Paste_Click(object sender, RoutedEventArgs e)
        {
            ContentTextBox.Paste();
            StatusTextBlock.Text = "已粘贴";
        }

        private void Undo_Click(object sender, RoutedEventArgs e)
        {
            if (ContentTextBox.CanUndo)
            {
                ContentTextBox.Undo();
                StatusTextBlock.Text = "已撤销";
            }
        }

        private void Redo_Click(object sender, RoutedEventArgs e)
        {
            if (ContentTextBox.CanRedo)
            {
                ContentTextBox.Redo();
                StatusTextBlock.Text = "已重做";
            }
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            if (SaveFile())
            {
                DialogResult = true;
                Close();
            }
        }

        private bool SaveFile()
        {
            try
            {
                var currentContent = ContentTextBox.Text;
                if (_fileSystemService.UpdateFileContent(_fcb.FullPath, currentContent))
                {
                    _content = currentContent;
                    IsModified = false;
                    StatusTextBlock.Text = "文件已保存";
                    return true;
                }
                else
                {
                    MessageBox.Show("保存失败：磁盘空间不足", "错误", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存文件时出错: {ex.Message}", "错误", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private bool ConfirmUnsavedChanges()
        {
            if (!IsModified) return true;

            var result = MessageBox.Show(
                "当前文档已修改，是否保存更改？",
                "确认",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                return SaveFile();
            }
            else if (result == MessageBoxResult.No)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (!ConfirmUnsavedChanges())
            {
                e.Cancel = true;
            }
            base.OnClosing(e);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 