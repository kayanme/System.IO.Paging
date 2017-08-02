﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FIle.Paging.LogicalLevel.Classes.Configurations;
using FIle.Paging.LogicalLevel.Contracts;
using Pager;
using Pager.Classes;

namespace FIle.Paging.LogicalLevel.Classes
{
    
    internal sealed class LogicalPageManager : ILogicalPageManager
    {
        private IPageManager _physicalManager;
        private LogicalPageManagerConfiguration _config;
        public LogicalPageManager (IPageManager manager,LogicalPageManagerConfiguration config)
        {
            _physicalManager = manager;
            _config = config;
        }

        public IPage CreatePage(byte type)
        {
            LogicalPageConfiguration conf = null;
            if (_config.Configuration.ContainsKey(type))
                conf = _config.Configuration[type];
            var page = _physicalManager.CreatePage(type);
            if (conf == null)
                return page;
            return conf.CreateLogicalPage(page);
        }

        public void DeletePage(PageReference page, bool ensureEmptyness)
        {
            _physicalManager.DeletePage(page,ensureEmptyness);
        }

        public void Dispose()
        {
            _physicalManager.Dispose();
        }

        public void GroupFlush(params IPage[] pages)
        {
            _physicalManager.GroupFlush(pages);
        }

        public IPage RetrievePage(PageReference pageNum)
        {
            LogicalPageConfiguration conf = null;
            var page = _physicalManager.RetrievePage(pageNum);
            if (_config.Configuration.ContainsKey(page.RegisteredPageType))
                conf = _config.Configuration[page.RegisteredPageType];
            
            if (conf == null)
                return page;
            return conf.CreateLogicalPage(page);
        }
    }
}
