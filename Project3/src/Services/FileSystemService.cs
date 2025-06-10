using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FileManagerSystem.Models;

namespace FileManagerSystem.Services
{
    /// <summary>
    /// 文件系统服务，管理虚拟文件系统
    /// </summary>
    public class FileSystemService
    {
        private FCB _rootDirectory;
        private BitMap _bitMap;
        private Dictionary<string, FCB> _fcbTable; // FCB表，通过路径快速查找
        private const int TOTAL_BLOCKS = 1024; // 模拟磁盘总块数
        private const int BLOCK_SIZE = 512; // 每块大小（字节）

        public FCB RootDirectory => _rootDirectory;
        public BitMap BitMap => _bitMap;

        public FileSystemService()
        {
            InitializeFileSystem();
        }

        private void InitializeFileSystem()
        {
            _bitMap = new BitMap(TOTAL_BLOCKS);
            _fcbTable = new Dictionary<string, FCB>();
            
            // 创建根目录
            _rootDirectory = new FCB("Root", true, "");
            _rootDirectory.FullPath = "\\";
            _fcbTable["\\"] = _rootDirectory;
            
            // 为根目录分配磁盘块
            var rootBlocks = _bitMap.AllocateBlocksNonContiguous(1);
            _rootDirectory.AllocatedBlocks = rootBlocks;
            _rootDirectory.StartBlock = rootBlocks.FirstOrDefault();
            
            // 创建一些初始目录和文件作为示例
            CreateInitialStructure();
        }

        private void CreateInitialStructure()
        {
            // 创建示例目录
            CreateDirectory("Documents", "\\");
            CreateDirectory("Pictures", "\\");
            CreateDirectory("Music", "\\");
            CreateDirectory("Videos", "\\");
            
            // 创建示例文件
            CreateFile("readme.txt", "\\", "这是一个示例文本文件。");
            CreateFile("example.txt", "\\Documents", "文档示例内容。");
            CreateFile("photo.jpg", "\\Pictures", "图片文件内容");
        }

        /// <summary>
        /// 创建目录
        /// </summary>
        public bool CreateDirectory(string directoryName, string parentPath)
        {
            try
            {
                if (string.IsNullOrEmpty(directoryName) || _fcbTable.ContainsKey($"{parentPath}\\{directoryName}"))
                    return false;

                var parentFCB = _fcbTable.ContainsKey(parentPath) ? _fcbTable[parentPath] : null;
                if (parentFCB == null || !parentFCB.IsDirectory)
                    return false;

                var newDirectory = new FCB(directoryName, true, parentPath);
                
                // 分配磁盘块
                var blocks = _bitMap.AllocateBlocksNonContiguous(1);
                if (blocks.Count == 0)
                    return false;

                newDirectory.AllocatedBlocks = blocks;
                newDirectory.StartBlock = blocks.First();

                // 添加到父目录和FCB表
                parentFCB.Children.Add(newDirectory);
                _fcbTable[newDirectory.FullPath] = newDirectory;

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 创建文件
        /// </summary>
        public bool CreateFile(string fileName, string parentPath, string content = "")
        {
            try
            {
                if (string.IsNullOrEmpty(fileName) || _fcbTable.ContainsKey($"{parentPath}\\{fileName}"))
                    return false;

                var parentFCB = _fcbTable.ContainsKey(parentPath) ? _fcbTable[parentPath] : null;
                if (parentFCB == null || !parentFCB.IsDirectory)
                    return false;

                var newFile = new FCB(fileName, false, parentPath);
                newFile.Size = content.Length;
                
                if (fileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                {
                    newFile.TextContent = content;
                }

                int blocksNeeded = Math.Max(1, (int)Math.Ceiling((double)content.Length / BLOCK_SIZE));
                var blocks = _bitMap.AllocateBlocksNonContiguous(blocksNeeded);
                if (blocks.Count == 0)
                    return false;

                newFile.AllocatedBlocks = blocks;
                newFile.StartBlock = blocks.First();

                // 添加到父目录和FCB表
                parentFCB.Children.Add(newFile);
                _fcbTable[newFile.FullPath] = newFile;

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 删除文件或目录
        /// </summary>
        public bool Delete(string fullPath)
        {
            try
            {
                if (!_fcbTable.ContainsKey(fullPath) || fullPath == "\\")
                    return false;

                var fcb = _fcbTable[fullPath];
                
                // 递归删除目录内容
                if (fcb.IsDirectory)
                {
                    var childrenToDelete = new List<FCB>(fcb.Children);
                    foreach (var child in childrenToDelete)
                    {
                        Delete(child.FullPath);
                    }
                }

                // 释放磁盘块
                _bitMap.DeallocateBlocks(fcb.AllocatedBlocks);

                // 从父目录中移除
                var parentPath = fcb.ParentPath;
                if (_fcbTable.ContainsKey(parentPath))
                {
                    var parent = _fcbTable[parentPath];
                    parent.Children.RemoveAll(c => c.FullPath == fullPath);
                }

                // 从FCB表中移除
                _fcbTable.Remove(fullPath);

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 重命名文件或目录
        /// </summary>
        public bool Rename(string fullPath, string newName)
        {
            try
            {
                if (!_fcbTable.ContainsKey(fullPath) || string.IsNullOrEmpty(newName) || fullPath == "\\")
                    return false;

                var fcb = _fcbTable[fullPath];
                var newFullPath = $"{fcb.ParentPath}\\{newName}";
                
                // 检查新名称是否已存在
                if (_fcbTable.ContainsKey(newFullPath))
                    return false;

                // 更新FCB
                var oldFullPath = fcb.FullPath;
                fcb.FileName = newName;
                fcb.FullPath = newFullPath;
                fcb.ModifiedTime = DateTime.Now;

                // 更新FCB表
                _fcbTable.Remove(oldFullPath);
                _fcbTable[newFullPath] = fcb;

                // 如果是目录，递归更新子项路径
                if (fcb.IsDirectory)
                {
                    UpdateChildrenPaths(fcb, oldFullPath, newFullPath);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private void UpdateChildrenPaths(FCB directory, string oldBasePath, string newBasePath)
        {
            foreach (var child in directory.Children)
            {
                var oldChildPath = child.FullPath;
                child.ParentPath = newBasePath;
                child.FullPath = child.FullPath.Replace(oldBasePath, newBasePath);
                
                // 更新FCB表
                _fcbTable.Remove(oldChildPath);
                _fcbTable[child.FullPath] = child;

                if (child.IsDirectory)
                {
                    UpdateChildrenPaths(child, oldChildPath, child.FullPath);
                }
            }
        }

        /// <summary>
        /// 获取目录内容
        /// </summary>
        public List<FCB> GetDirectoryContents(string directoryPath)
        {
            if (_fcbTable.ContainsKey(directoryPath) && _fcbTable[directoryPath].IsDirectory)
            {
                var directory = _fcbTable[directoryPath];
                directory.AccessedTime = DateTime.Now;
                return new List<FCB>(directory.Children);
            }
            return new List<FCB>();
        }

        /// <summary>
        /// 获取FCB信息
        /// </summary>
        public FCB GetFCB(string fullPath)
        {
            return _fcbTable.ContainsKey(fullPath) ? _fcbTable[fullPath] : null;
        }

        /// <summary>
        /// 获取磁盘使用情况
        /// </summary>
        public (int total, int used, int free, double percentage) GetDiskUsage()
        {
            return (_bitMap.TotalBlocks, _bitMap.UsedBlocks, _bitMap.FreeBlocks, _bitMap.UsagePercentage);
        }

        public string GetFileContent(string fullPath)
        {
            if (_fcbTable.ContainsKey(fullPath))
            {
                return _fcbTable[fullPath].TextContent;
            }
            return string.Empty;
        }

        public bool UpdateFileContent(string fullPath, string content)
        {
            if (_fcbTable.ContainsKey(fullPath))
            {
                var fcb = _fcbTable[fullPath];
                fcb.TextContent = content;
                fcb.Size = content.Length;
                fcb.ModifiedTime = DateTime.Now;
                
                int newBlocksNeeded = Math.Max(1, (int)Math.Ceiling((double)content.Length / BLOCK_SIZE));
                int currentBlocks = fcb.AllocatedBlocks.Count;
                
                if (newBlocksNeeded != currentBlocks)
                {
                    _bitMap.DeallocateBlocks(fcb.AllocatedBlocks);
                    
                    var newBlocks = _bitMap.AllocateBlocksNonContiguous(newBlocksNeeded);
                    if (newBlocks.Count > 0)
                    {
                        fcb.AllocatedBlocks = newBlocks;
                        fcb.StartBlock = newBlocks.First();
                    }
                    else
                    {
                        return false;
                    }
                }
                
                return true;
            }
            return false;
        }
    }
} 