﻿using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Paging.PhysicalLevel.Classes;
using System.IO.Paging.PhysicalLevel.Classes.Pages.Contracts;
using System.IO.Paging.PhysicalLevel.Classes.References;
using System.Transactions;

namespace System.IO.Paging.LogicalLevel.Classes.Transactions
{
    internal sealed class TransactionProxyPage<TRecord> : IPage<TRecord> where TRecord : struct
    {
        private readonly IPage<TRecord> _inner;
        private readonly ConcurrentDictionary<Transaction, TransactionContentResource<TRecord>> _transactionBlocks = new ConcurrentDictionary<Transaction, TransactionContentResource<TRecord>>();

        public TransactionProxyPage(IPage<TRecord> inner)
        {
            _inner = inner;
        }

        public double PageFullness => throw new NotImplementedException();
       

      

        public TypedRecord<TRecord> AddRecord(TRecord type)
        {
            var store = GetStore();
            if (store != null)
                return store.AddRecord(type);
            else
                return _inner.AddRecord(type);
        }

       
        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public IBinarySearcher<TRecord> BinarySearch()
        {
            throw new NotImplementedException();
        }

        public void Flush()
        {
            throw new NotImplementedException();
        }

        public void FreeRecord(TypedRecord<TRecord> record)
        {
            var store = GetStore();
            if (store != null)
                store.FreeRecord(record);
            else
                _inner.AddRecord(record.Data);
        }

        public TypedRecord<TRecord> GetRecord(PageRecordReference reference)
        {
            var store = GetStore();
            if (store != null)
                return store.GetRecord(reference);
            else
                return _inner.GetRecord(reference);
        }

        public IEnumerable<TypedRecord<TRecord>> GetRecordRange(PageRecordReference start, PageRecordReference end)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<TypedRecord<TRecord>> IterateRecords()
        {
            var store = GetStore();
            if (store != null)
                return store.IterateRecords();
            else
                return _inner.IterateRecords();
        }

        public void StoreRecord(TypedRecord<TRecord> record)
        {
            var store = GetStore();
            if (store != null)
                 store.StoreRecord(record);
            else
                 _inner.StoreRecord(record);
        }


        private TransactionContentResource<TRecord> GetStore()
        {
            if (Transaction.Current != null)
            {
                var store = _transactionBlocks.GetOrAdd(Transaction.Current, (k) =>
                {                    
                    if (Transaction.Current.IsolationLevel == IsolationLevel.ReadUncommitted)
                        throw new InvalidOperationException("Isolation level `read uncommited` is unsupported");
                    var block = new TransactionContentResource<TRecord>(() => _transactionBlocks.TryRemove(k, out var b), _inner, Transaction.Current.IsolationLevel);
                    Transaction.Current.EnlistVolatile(block, EnlistmentOptions.None);
                    return block;
                });
                return store;
            }
            else
                return null;
        }
    }
}
