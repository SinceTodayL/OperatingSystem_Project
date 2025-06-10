using System;
using System.Collections.Generic;

namespace FileManagerSystem.Models
{
    /// <summary>
    /// 文件控制块 (File Control Block)
    /// </summary>
    public class FCB
    {
        public string FileName { get; set; } = string.Empty;
        public bool IsDirectory { get; set; }
        public long Size { get; set; }
        public DateTime CreatedTime { get; set; }
        public DateTime ModifiedTime { get; set; }
        public DateTime AccessedTime { get; set; }
        public FileAttributes Attributes { get; set; }
        public int StartBlock { get; set; }  // 起始磁盘块
        public List<int> AllocatedBlocks { get; set; } = new List<int>();
        public string ParentPath { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        
        // 文本内容存储（用于.txt文件）
        public string TextContent { get; set; } = string.Empty;
        public string MimeType { get; set; } = "text/plain";
        public string Encoding { get; set; } = "UTF-8";
        
        // 对于目录，存储子项
        public List<FCB> Children { get; set; } = new List<FCB>();
        
        public FCB()
        {
            CreatedTime = DateTime.Now;
            ModifiedTime = DateTime.Now;
            AccessedTime = DateTime.Now;
        }
        
        public FCB(string fileName, bool isDirectory, string parentPath = "") : this()
        {
            FileName = fileName;
            IsDirectory = isDirectory;
            ParentPath = parentPath;
            FullPath = string.IsNullOrEmpty(parentPath) ? fileName : $"{parentPath}\\{fileName}";
            
            if (!isDirectory && !string.IsNullOrEmpty(fileName))
            {
                var extension = System.IO.Path.GetExtension(fileName).ToLowerInvariant();
                MimeType = GetMimeType(extension);
            }
        }

        private string GetMimeType(string extension)
        {
            return extension switch
            {
                ".txt" => "text/plain",
                _ => "application/octet-stream"
            };
        }

        public bool IsTextEditable()
        {
            if (IsDirectory) return false;
            
            var extension = System.IO.Path.GetExtension(FileName).ToLowerInvariant();
            return extension == ".txt";
        }

        public string GetFileTypeDescription()
        {
            if (IsDirectory) return "文件夹";
            
            var extension = System.IO.Path.GetExtension(FileName).ToLowerInvariant();
            return extension switch
            {
                ".txt" => "文本文档",
                _ => "文件"
            };
        }
    }
    
    [Flags]
    public enum FileAttributes
    {
        None = 0,
        ReadOnly = 1,
        Hidden = 2,
        System = 4,
        Directory = 8,
        Archive = 16,
        Compressed = 32
    }
} 