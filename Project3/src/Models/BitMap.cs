using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace FileManagerSystem.Models
{
    /// <summary>
    /// 位图算法实现，用于磁盘空间管理
    /// </summary>
    public class BitMap
    {
        private BitArray _bitArray;
        private int _totalBlocks;
        private int _usedBlocks;

        public int TotalBlocks => _totalBlocks;
        public int UsedBlocks => _usedBlocks;
        public int FreeBlocks => _totalBlocks - _usedBlocks;
        public double UsagePercentage => (double)_usedBlocks / _totalBlocks * 100;

        public BitMap(int totalBlocks)
        {
            _totalBlocks = totalBlocks;
            _bitArray = new BitArray(totalBlocks, false); // false表示空闲
            _usedBlocks = 0;
        }

        /// <summary>
        /// 分配指定数量的连续磁盘块
        /// </summary>
        /// <param name="blockCount">需要分配的块数</param>
        /// <returns>分配的起始块索引，-1表示分配失败</returns>
        public int AllocateBlocks(int blockCount)
        {
            if (blockCount <= 0 || FreeBlocks < blockCount)
                return -1;

            // 查找连续的空闲块
            for (int i = 0; i <= _totalBlocks - blockCount; i++)
            {
                bool canAllocate = true;
                for (int j = 0; j < blockCount; j++)
                {
                    if (_bitArray[i + j])
                    {
                        canAllocate = false;
                        break;
                    }
                }

                if (canAllocate)
                {
                    // 标记这些块为已使用
                    for (int j = 0; j < blockCount; j++)
                    {
                        _bitArray[i + j] = true;
                    }
                    _usedBlocks += blockCount;
                    return i;
                }
            }

            return -1; // 无法找到连续的空闲块
        }

        /// <summary>
        /// 分配指定数量的磁盘块（可以不连续）
        /// </summary>
        /// <param name="blockCount">需要分配的块数</param>
        /// <returns>分配的块索引列表</returns>
        public List<int> AllocateBlocksNonContiguous(int blockCount)
        {
            var allocatedBlocks = new List<int>();
            
            if (blockCount <= 0 || FreeBlocks < blockCount)
                return allocatedBlocks;

            for (int i = 0; i < _totalBlocks && allocatedBlocks.Count < blockCount; i++)
            {
                if (!_bitArray[i])
                {
                    _bitArray[i] = true;
                    allocatedBlocks.Add(i);
                    _usedBlocks++;
                }
            }

            return allocatedBlocks;
        }

        /// <summary>
        /// 释放指定的磁盘块
        /// </summary>
        /// <param name="blocks">要释放的块索引列表</param>
        public void DeallocateBlocks(List<int> blocks)
        {
            foreach (int block in blocks)
            {
                if (block >= 0 && block < _totalBlocks && _bitArray[block])
                {
                    _bitArray[block] = false;
                    _usedBlocks--;
                }
            }
        }

        /// <summary>
        /// 释放连续的磁盘块
        /// </summary>
        /// <param name="startBlock">起始块索引</param>
        /// <param name="blockCount">块数量</param>
        public void DeallocateBlocks(int startBlock, int blockCount)
        {
            for (int i = startBlock; i < startBlock + blockCount && i < _totalBlocks; i++)
            {
                if (_bitArray[i])
                {
                    _bitArray[i] = false;
                    _usedBlocks--;
                }
            }
        }

        /// <summary>
        /// 检查指定块是否已分配
        /// </summary>
        /// <param name="blockIndex">块索引</param>
        /// <returns>true表示已分配，false表示空闲</returns>
        public bool IsBlockAllocated(int blockIndex)
        {
            if (blockIndex < 0 || blockIndex >= _totalBlocks)
                return false;
            return _bitArray[blockIndex];
        }

        /// <summary>
        /// 获取位图状态的字符串表示
        /// </summary>
        /// <returns>位图状态字符串</returns>
        public string GetBitMapString()
        {
            var result = new System.Text.StringBuilder();
            for (int i = 0; i < _totalBlocks; i++)
            {
                result.Append(_bitArray[i] ? '1' : '0');
                if ((i + 1) % 64 == 0)
                    result.AppendLine();
            }
            return result.ToString();
        }
    }
} 