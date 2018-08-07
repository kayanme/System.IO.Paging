﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using TimeArchiver.Contracts;

namespace TimeArchiver.Classes
{
    internal class DataInteraction<T> : IDataInteraction<T> where T : struct
    {
        private readonly IIndexInteraction<T> indexInteraction;

        public DataInteraction(IIndexInteraction<T> indexInteraction)
        {
            this.indexInteraction = indexInteraction ?? throw new ArgumentNullException(nameof(indexInteraction));
        }

        private (long,long) GetUnitedBorders((long ,long ) block1,(long ,long ) block2)
            => (Math.Min(block1.Item1, block2.Item1), Math.Max(block1.Item2, block2.Item2));

        private (long, long) GetUnitedBorders(IndexRecord block1, (long, long) block2)
         => (Math.Min(block1.Start, block2.Item1), Math.Max(block1.End, block2.Item2));

        private long Range((long , long) b) => b.Item2-b.Item1;

        private IndexRecord SelectIndexToInsert((long,long) bounds,IndexRecord index1,IndexRecord index2)
        {
            var (l, u) = GetUnitedBorders(index1,bounds);
            var (l2, u2) = GetUnitedBorders(index2, bounds);
            if (u2 - l2 < u - l)
                return index2;
            return index1;
        }

        private IndexRecord ProcessIndexLevel(IndexRecord currentRoot, DataRecord<T>[] block)
        {
            
            var (lowbound, upbound) = (block.First().Stamp, block.Last().Stamp);
                    
            var firstLevelChildren = indexInteraction.GetChildren(currentRoot);
            Debug.Assert(firstLevelChildren.Length == 2, "firstLevelChildren.Length == 2");
            var indexToInsertBlock = SelectIndexToInsert((lowbound, upbound), firstLevelChildren[0], firstLevelChildren[1]);
            var otherIndex = firstLevelChildren[0].Equals(indexToInsertBlock) ? firstLevelChildren[1] : firstLevelChildren[0];
            currentRoot = indexInteraction.ResizeIndex(currentRoot, lowbound, upbound);
            indexToInsertBlock = InsertOrProcessNextlevel(block, lowbound, upbound, indexToInsertBlock);
            var secondLevelChildren = indexInteraction.GetChildren(indexToInsertBlock);
            if (secondLevelChildren[0].MaxUnderlyingDepth != secondLevelChildren[1].MaxUnderlyingDepth)
            {
                var lesserGrandChild = secondLevelChildren[0].MaxUnderlyingDepth > secondLevelChildren[1].MaxUnderlyingDepth ? secondLevelChildren[1] : secondLevelChildren[0];
                otherIndex = indexInteraction.CreateUnderlayingIndexRecord(otherIndex);
                indexInteraction.MoveIndex(otherIndex, lesserGrandChild);
                indexInteraction.ResetTreeDepth(otherIndex);
            }
            
            currentRoot = indexInteraction.ResetTreeDepth(currentRoot);
            return currentRoot;
        }

        private IndexRecord InsertOrProcessNextlevel(DataRecord<T>[] block, long lowbound, long upbound, IndexRecord indexToInsertBlock)
        {
            var (newLowBorder, newUpBorder) = GetUnitedBorders(indexToInsertBlock, (lowbound, upbound));
            if (indexToInsertBlock.StoresData || lowbound <= indexToInsertBlock.Start && upbound >= indexToInsertBlock.End)
            {
                indexToInsertBlock = indexInteraction.CreateUnderlayingIndexRecord(indexToInsertBlock);
                indexInteraction.CreateDataBlock(indexToInsertBlock, block);
                indexToInsertBlock = indexInteraction.ResizeIndex(indexToInsertBlock, newLowBorder, newUpBorder);
            }
            else
            {
                indexToInsertBlock= ProcessIndexLevel(indexToInsertBlock, block);
            }

            return indexToInsertBlock;
        }

        public async Task AddBlock(DataRecord<T>[] sortedData)
        {
            var (lowbound, upbound) = (sortedData.First().Stamp, sortedData.Last().Stamp);
            var root = indexInteraction.GetRoot();
            if (root == null)
            {
                indexInteraction.CreateDataBlock(sortedData);
                return;
            }
            var currentIndex = root.Value;
            if (currentIndex.StoresData)
            {
                currentIndex = indexInteraction.CreateUnderlayingIndexRecord(currentIndex);
                indexInteraction.CreateDataBlock(currentIndex, sortedData);
                var (a, b) = GetUnitedBorders(currentIndex, (lowbound, upbound));
                indexInteraction.ResizeIndex(currentIndex, a, b);
                return;
            }


            await indexInteraction.PrepareIndexChange();
            ProcessIndexLevel(currentIndex, sortedData);
            indexInteraction.FinalizeIndexChange();

        }

        public void AddValue(DataRecord<T> value)
        {
            throw new NotImplementedException();
        }

        public DataRecord<T> FindBefore(long stamp)
        {
            throw new NotImplementedException();
        }

        public DataRecord<T>[] FindInRange(long start, long end)
        {
            throw new NotImplementedException();
        }

        public void Remove(long stamp)
        {
            throw new NotImplementedException();
        }
    }
}
