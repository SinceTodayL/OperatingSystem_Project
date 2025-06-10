using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using FileManagerSystem.Models;

namespace FileManagerSystem.Views.Dialogs
{
    public partial class PropertiesDialog : Window, INotifyPropertyChanged
    {
        private FCB _fcb;

        public string FileName => _fcb?.FileName ?? "";
        public string FileType => _fcb?.IsDirectory == true ? "文件夹" : "文件";
        public string TypeIcon => _fcb?.IsDirectory == true ? "📁" : "📄";
        public string Location => _fcb?.ParentPath ?? "";
        public string Size => _fcb?.IsDirectory == true ? "" : FormatFileSize(_fcb?.Size ?? 0);
        public string CreatedTime => _fcb?.CreatedTime.ToString("yyyy年MM月dd日 HH:mm:ss") ?? "";
        public string ModifiedTime => _fcb?.ModifiedTime.ToString("yyyy年MM月dd日 HH:mm:ss") ?? "";
        public string AccessedTime => _fcb?.AccessedTime.ToString("yyyy年MM月dd日 HH:mm:ss") ?? "";
        public string StartBlock => _fcb?.StartBlock.ToString() ?? "";
        public string BlockCount => _fcb?.AllocatedBlocks?.Count.ToString() ?? "";
        public string AllocatedBlocks => _fcb?.AllocatedBlocks != null ? 
            string.Join(", ", _fcb.AllocatedBlocks.Take(10)) + 
            (_fcb.AllocatedBlocks.Count > 10 ? "..." : "") : "";

        public PropertiesDialog(FCB fcb)
        {
            InitializeComponent();
            DataContext = this;
            _fcb = fcb;
            
            // 触发所有属性更新
            OnPropertyChanged("");
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

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 