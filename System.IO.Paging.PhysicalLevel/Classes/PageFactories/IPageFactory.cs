﻿using System.IO.Paging.PhysicalLevel.Classes.Pages.Contracts;
using System.IO.Paging.PhysicalLevel.Classes.References;

namespace System.IO.Paging.PhysicalLevel.Classes.PageFactories
{
    internal interface IPageFactory
    {
        IHeaderedPage<THeader> GetHeaderAccessor<THeader>(BufferedPage page, PageReference pageNum, Action actionToClean) where THeader : new();
        IPageInfo GetPageInfo(BufferedPage page, PageReference pageNum, Action actionToClean);
        IPhysicalLocks GetPageLocks(BufferedPage page, PageReference pageNum, Action actionToClean);

        IBinarySearcher<TRecord> GetBinarySearcher<TRecord>(BufferedPage page, PageReference pageNum,
            Action actionToClean) where TRecord : struct;
        IPage<TRecord> GetRecordAccessor<TRecord>(BufferedPage page, PageReference pageNum, Action actionToClean)
            where TRecord : struct;
        ILogicalRecordOrderManipulation GetSorter<TRecord>(BufferedPage page, PageReference pageNum, Action actionToClean) where TRecord:struct;
    }
}