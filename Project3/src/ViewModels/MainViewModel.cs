using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using FileManagerSystem.Models;
using FileManagerSystem.Services;
using FileManagerSystem.Commands;

namespace FileManagerSystem.ViewModels
{
    /// <summary>
    /// 主视图模型
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly FileSystemService _fileSystemService;
        private string _currentPath;
        private FileItemViewModel _selectedItem;
        private ObservableCollection<FileItemViewModel> _items;
        private ObservableCollection<string> _pathHistory;
        private int _currentHistoryIndex;

        public ObservableCollection<FileItemViewModel> Items 
        { 
            get => _items; 
            set 
            { 
                _items = value; 
                OnPropertyChanged();
            } 
        }

        public FileItemViewModel SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (_selectedItem != null)
                    _selectedItem.IsSelected = false;
                
                _selectedItem = value;
                
                if (_selectedItem != null)
                    _selectedItem.IsSelected = true;
                
                OnPropertyChanged();
            }
        }

        public string CurrentPath
        {
            get => _currentPath;
            set
            {
                _currentPath = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanGoBack));
                OnPropertyChanged(nameof(CanGoForward));
                LoadCurrentDirectory();
            }
        }

        public bool CanGoBack => _currentHistoryIndex > 0;
        public bool CanGoForward => _currentHistoryIndex < _pathHistory.Count - 1;

        // 磁盘使用情况
        public int TotalBlocks => _fileSystemService.BitMap.TotalBlocks;
        public int UsedBlocks => _fileSystemService.BitMap.UsedBlocks;
        public int FreeBlocks => _fileSystemService.BitMap.FreeBlocks;
        public double UsagePercentage => _fileSystemService.BitMap.UsagePercentage;

        // 命令
        public ICommand NavigateCommand { get; }
        public ICommand GoBackCommand { get; }
        public ICommand GoForwardCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand CreateFileCommand { get; }
        public ICommand CreateFolderCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand RenameCommand { get; }
        public ICommand OpenCommand { get; }

        public MainViewModel()
        {
            _fileSystemService = new FileSystemService();
            _items = new ObservableCollection<FileItemViewModel>();
            _pathHistory = new ObservableCollection<string>();
            _currentHistoryIndex = -1;

            // 初始化命令
            NavigateCommand = new RelayCommand<string>(Navigate);
            GoBackCommand = new RelayCommand(GoBack, () => CanGoBack);
            GoForwardCommand = new RelayCommand(GoForward, () => CanGoForward);
            RefreshCommand = new RelayCommand(Refresh);
            CreateFileCommand = new RelayCommand<string>(CreateFile);
            CreateFolderCommand = new RelayCommand<string>(CreateFolder);
            DeleteCommand = new RelayCommand(Delete, () => SelectedItem != null);
            RenameCommand = new RelayCommand<string>(Rename, (newName) => SelectedItem != null && !string.IsNullOrEmpty(newName));
            OpenCommand = new RelayCommand(Open, () => SelectedItem != null);

            // 导航到根目录
            Navigate("\\");
        }

        private void Navigate(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;

            // 添加到历史记录
            if (_currentHistoryIndex < _pathHistory.Count - 1)
            {
                // 如果不在历史记录末尾，删除后面的记录
                for (int i = _pathHistory.Count - 1; i > _currentHistoryIndex; i--)
                {
                    _pathHistory.RemoveAt(i);
                }
            }

            if (_pathHistory.Count == 0 || _pathHistory.Last() != path)
            {
                _pathHistory.Add(path);
                _currentHistoryIndex = _pathHistory.Count - 1;
            }

            CurrentPath = path;
        }

        private void GoBack()
        {
            if (CanGoBack)
            {
                _currentHistoryIndex--;
                CurrentPath = _pathHistory[_currentHistoryIndex];
            }
        }

        private void GoForward()
        {
            if (CanGoForward)
            {
                _currentHistoryIndex++;
                CurrentPath = _pathHistory[_currentHistoryIndex];
            }
        }

        private void Refresh()
        {
            LoadCurrentDirectory();
            OnPropertyChanged(nameof(TotalBlocks));
            OnPropertyChanged(nameof(UsedBlocks));
            OnPropertyChanged(nameof(FreeBlocks));
            OnPropertyChanged(nameof(UsagePercentage));
        }

        private void LoadCurrentDirectory()
        {
            var contents = _fileSystemService.GetDirectoryContents(CurrentPath);
            Items.Clear();
            
            foreach (var fcb in contents.OrderBy(f => !f.IsDirectory).ThenBy(f => f.FileName))
            {
                Items.Add(new FileItemViewModel(fcb));
            }
        }

        private void CreateFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return;

            if (_fileSystemService.CreateFile(fileName, CurrentPath))
            {
                Refresh();
            }
        }

        private void CreateFolder(string folderName)
        {
            if (string.IsNullOrEmpty(folderName))
                return;

            if (_fileSystemService.CreateDirectory(folderName, CurrentPath))
            {
                Refresh();
            }
        }

        private void Delete()
        {
            if (SelectedItem?.FCB != null)
            {
                if (_fileSystemService.Delete(SelectedItem.FullPath))
                {
                    Refresh();
                    SelectedItem = null;
                }
            }
        }

        private void Rename(string newName)
        {
            if (SelectedItem?.FCB != null && !string.IsNullOrEmpty(newName))
            {
                if (_fileSystemService.Rename(SelectedItem.FullPath, newName))
                {
                    Refresh();
                }
            }
        }

        private void Open()
        {
            if (SelectedItem?.FCB != null && SelectedItem.IsDirectory)
            {
                Navigate(SelectedItem.FullPath);
            }
        }

        public void NavigateToParent()
        {
            if (CurrentPath != "\\")
            {
                var parentPath = System.IO.Path.GetDirectoryName(CurrentPath);
                if (string.IsNullOrEmpty(parentPath))
                    parentPath = "\\";
                Navigate(parentPath);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 