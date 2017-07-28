﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Pager.Classes;
using Pager.Implementations;

namespace Pager
{
    [Export(typeof(IPageManager))]
    internal sealed class PageManager : IPageManager
    {
       
        private IExtentAccessorFactory _blockFactory;
        private IGAMAccessor _accessor;
        private int _pageSize;
        private PageManagerConfiguration _config;
        private IUnderlyingFileOperator _operatorForDisposal;
        private ConcurrentDictionary<int, BufferedPage> _bufferedPages = new ConcurrentDictionary<int, BufferedPage>();
        private int _pages;

        [ImportingConstructor]
        internal PageManager(PageManagerConfiguration config,IGAMAccessor accessor,
            IExtentAccessorFactory blockFactory,IUnderlyingFileOperator operatorForDisposal)
        {
          
            _accessor = accessor;
            _blockFactory = blockFactory;
            _config = config;
            
            _pageSize = config.SizeOfPage == PageManagerConfiguration.PageSize.Kb4 ? 4096 : 8192;
            _operatorForDisposal = operatorForDisposal;
            _pages = (int)((operatorForDisposal.FileSize - Extent.Size) / _pageSize);            
        }

      


        public void DeletePage(PageReference page, bool ensureEmptyness)
        {
            _accessor.MarkPageFree(page.PageNum);
            Interlocked.Decrement(ref _pages);
        }

        public void Dispose()
        {
            foreach(var p in _bufferedPages)
            {
                p.Value.Accessor.Dispose();                           
            }
            _accessor.Dispose();
            _operatorForDisposal.Dispose();
        }

        public TypedPage RetrievePage(PageReference pageNum) 
        {
            var page = _bufferedPages.GetOrAdd(pageNum.PageNum, i =>
            {
                var block = _blockFactory.GetAccessor(Extent.Size + i * _pageSize, _pageSize);
                
                var type = _config.PageMap[_accessor.GetPageType(pageNum.PageNum)];
                var headers = type.CreateHeaders(block,0);
                                                    
                return new BufferedPage {Accessor = block,Headers =headers,Config = type };
            });
            
            return page.Config.CreatePage(page.Headers,page.Accessor, pageNum,_pageSize);
        }

        public void GroupFlush(params TypedPage[] pages)
        {
            foreach(var t in  pages.Select(k=>_bufferedPages[k.Reference.PageNum]).GroupBy(k=>k.Accessor.PageSize).Select(k=>k.First()))
            {
                t.Accessor.Flush();
            }
        }


        public TypedPage CreatePage(byte type)
        {
            
            if (type == 0)
                throw new ArgumentException("TRecordType");
            var newPageNum = _accessor.MarkPageUsed(type);
            Interlocked.Increment(ref _pages);
            return RetrievePage(new PageReference(newPageNum));
        }
    }
}