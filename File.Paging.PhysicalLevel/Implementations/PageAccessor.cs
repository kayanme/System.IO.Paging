﻿using System;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using File.Paging.PhysicalLevel.Contracts;

namespace File.Paging.PhysicalLevel.Implementations
{
 
    internal class PageAccessor: IPageAccessor
    {
        private readonly IExtentAccessorFactory _disposer;
        private readonly MemoryMappedViewAccessor _map;
        private readonly int _startOffset;

        internal PageAccessor(int startOffset,int pageSize,uint extentNumber,MemoryMappedViewAccessor accessor, IExtentAccessorFactory disposer)
        {
            _map = accessor;
            _startOffset = startOffset;
            _disposer = disposer;
            PageSize = pageSize;
            ExtentNumber = extentNumber;
        }
    
        public int PageSize { get; }

        public uint ExtentNumber { get; }


        public void Flush()
        {
            if (!_disposedValue)
               _map.Flush();
        }

        public byte[] GetByteArray(int position, int length)
        {
           if (_disposedValue)
                throw new ObjectDisposedException("IPageAccessor");
            Debug.Assert(position + length <= PageSize, "position + length <= _pageSize");
            var b = new byte[length];
            _map.ReadArray(position + _startOffset, b, 0, length);
            return b; 
        }

        public void SetByteArray(byte[] record, int position, int length)
        {
            if (_disposedValue)
                throw new ObjectDisposedException("IPageAccessor");
            Debug.Assert(position + length <= PageSize, "position + length <= _pageSize");
            _map.WriteArray(position + _startOffset, record, 0, length);
        }

        public void ClearPage()
        {
            _map.WriteArray(_startOffset, new byte[PageSize], 0, PageSize);
        }

        public IPageAccessor GetChildAccessorWithStartShift(ushort startShirt)
        {
            if (_disposedValue)
                throw new ObjectDisposedException("IPageAccessor");
            if (startShirt == 0)
                return this;
            return new PageAccessor(_startOffset + startShirt, PageSize - startShirt, ExtentNumber, _map, null);
        }

        private bool _disposedValue = false;
        void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    Flush();
                    _disposer?.ReturnAccessor(_map);
                }

                _disposedValue = true;
            }
        }
        ~PageAccessor()
        {
            Dispose(true);
        }


        public  void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

    
}
