﻿using System;
using System.Collections.Concurrent;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Threading;
using System.Threading.Tasks;
using File.Paging.PhysicalLevel.Contracts;

namespace File.Paging.PhysicalLevel.Implementations
{
    [Export(typeof(IUnderlyingFileOperator))]
    internal sealed class UnderyingFileOperator : IUnderlyingFileOperator
    {
        private readonly FileStream _file;
        private MemoryMappedFile _map;

        private readonly ConcurrentDictionary<MemoryMappedFile,int> _oldMaps = new ConcurrentDictionary<MemoryMappedFile,int>();

        [ImportingConstructor]
        internal UnderyingFileOperator(FileStream file)
        {
            _file = file;
            _map = MemoryMappedFile.CreateFromFile(_file, "PageMap"+Guid.NewGuid() , _file.Length!=0?_file.Length:Extent.Size , MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, false);
            _oldMaps.TryAdd(_map, 0);
        }

        public long FileSize => _file.Length;

        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

        public async Task<MemoryMappedFile> GetMappedFile(long desiredFileLength)
        {

            if (_file.Length < desiredFileLength)
            {
                await AddExtent(1);
            }
            return await Task.Factory.StartNew(() =>
            {
                try
                {

                    _lock.EnterReadLock();
                    int i;
                    do
                    {
                        i = _oldMaps[_map];
                    } while (!_oldMaps.TryUpdate(_map, i + 1, i));
                    return _map;
                }
                finally
                {
                    if (_lock.IsReadLockHeld)
                        _lock.ExitReadLock();
                }
            });

        }

        private void CheckMapForCleaning(MemoryMappedFile oldMap)
        {
            if (_oldMaps[oldMap] == 0)
            {                
                _oldMaps.TryRemove(oldMap, out int i);
                Debug.Assert(i == 0, "i==0");
                oldMap.Dispose();
            }
        }

        public Task ReturnMappedFile(MemoryMappedFile file)
        {
            return Task.Factory.StartNew(() =>
            {
                int i;
                do
                {
                    i = _oldMaps[file];
                } while (!_oldMaps.TryUpdate(file, i - 1, i));
                CheckMapForCleaning(file);
            });
        }

        #region IDisposable Support
        private bool _disposedValue = false; // To detect redundant calls

        void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _map.Dispose();
                    _file.Dispose();
                    _lock.Dispose();
                }
                
                _disposedValue = true;
            }
        }

       
         ~UnderyingFileOperator()
        {            
            Dispose(true);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {         
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public Task AddExtent(int extentCount)
        {
            return Task.Factory.StartNew(() =>
            {
                MemoryMappedFile oldMap;
                try
                {

                    _lock.EnterWriteLock();
                    var map = MemoryMappedFile.CreateFromFile(_file,
                        "PageMap" + _file.Length + Extent.Size * extentCount, _file.Length + Extent.Size * extentCount,
                        MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, false);
                    _oldMaps.TryAdd(map, 0);
                    oldMap = _map;
                    _map = map;
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
                CheckMapForCleaning(oldMap);
            });
        }
        #endregion
    }
}