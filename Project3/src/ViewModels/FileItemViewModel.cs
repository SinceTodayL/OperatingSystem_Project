using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FileManagerSystem.Models;

namespace FileManagerSystem.ViewModels
{
    /// <summary>
    /// 文件项视图模型
    /// </summary>
    public class FileItemViewModel : INotifyPropertyChanged
    {
        private FCB _fcb;
        private bool _isSelected;

        public FCB FCB 
        { 
            get => _fcb; 
            set 
            { 
                _fcb = value; 
                OnPropertyChanged();
                OnPropertyChanged(nameof(Name));
                OnPropertyChanged(nameof(IsDirectory));
                OnPropertyChanged(nameof(Size));
                OnPropertyChanged(nameof(ModifiedTime));
                OnPropertyChanged(nameof(Icon));
            } 
        }

        public string Name => _fcb?.FileName ?? "";
        public bool IsDirectory => _fcb?.IsDirectory ?? false;
        public long Size => _fcb?.Size ?? 0;
        public string SizeText => IsDirectory ? "" : FormatFileSize(Size);
        public DateTime ModifiedTime => _fcb?.ModifiedTime ?? DateTime.MinValue;
        public string FullPath => _fcb?.FullPath ?? "";
        public string TypeIcon => IsDirectory ? "📁" : "📄";

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }

        public ImageSource Icon
        {
            get
            {
                try
                {
                    if (IsDirectory)
                    {
                        return new BitmapImage(new Uri("pack://application:,,,/Resources/folder.png", UriKind.Absolute));
                    }
                    else
                    {
                        // 根据文件扩展名返回不同图标
                        var extension = System.IO.Path.GetExtension(Name).ToLower();
                        return extension switch
                        {
                            ".txt" => new BitmapImage(new Uri("pack://application:,,,/Resources/text.png", UriKind.Absolute)),
                            ".jpg" or ".jpeg" or ".png" or ".bmp" or ".gif" => new BitmapImage(new Uri("pack://application:,,,/Resources/image.png", UriKind.Absolute)),
                            ".mp3" or ".wav" or ".wma" => new BitmapImage(new Uri("pack://application:,,,/Resources/music.png", UriKind.Absolute)),
                            ".mp4" or ".avi" or ".mkv" => new BitmapImage(new Uri("pack://application:,,,/Resources/video.png", UriKind.Absolute)),
                            _ => new BitmapImage(new Uri("pack://application:,,,/Resources/file.png", UriKind.Absolute))
                        };
                    }
                }
                catch
                {
                    // 如果图标加载失败，返回默认图标或null
                    return null;
                }
            }
        }

        public FileItemViewModel(FCB fcb)
        {
            _fcb = fcb;
        }

        private string FormatFileSize(long bytes)
        {
            if (bytes == 0) return "0 B";
            
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
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