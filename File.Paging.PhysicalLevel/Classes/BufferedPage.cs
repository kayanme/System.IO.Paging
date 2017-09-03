﻿using File.Paging.PhysicalLevel.Classes.Configurations;
using File.Paging.PhysicalLevel.Contracts;

namespace File.Paging.PhysicalLevel.Classes
{
    internal class BufferedPage
    {
        public byte PageType;
        public IPageAccessor Accessor;
        public IPageAccessor ContentAccessor;
        public IPageHeaders Headers;
        public PageContentConfiguration Config;
        public PageHeadersConfiguration HeaderConfig;
        public bool MarkedForRemoval;
        public int UserCount;
    }
}
