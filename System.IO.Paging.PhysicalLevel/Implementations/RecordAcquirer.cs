﻿using System.ComponentModel.Composition;
using System.IO.Paging.PhysicalLevel.Configuration;
using System.IO.Paging.PhysicalLevel.Contracts.Internal;
using System.IO.Paging.PhysicalLevel.Implementations.Serialization;

namespace System.IO.Paging.PhysicalLevel.Implementations
{
    [Export(typeof(IRecordAcquirer<>))]
    internal sealed class RecordAcquirer<TRecord> : IRecordAcquirer<TRecord> where TRecord : struct
    {
        private readonly IPageAccessor _accessor;
        private readonly SerializationCore<TRecord> _serializer;

        [ImportingConstructor]
        public RecordAcquirer(IPageAccessor accessor,RecordDeclaration<TRecord> configuration)
        {
            _accessor = accessor;
            _serializer = new SerializationCore<TRecord>(configuration);
        }

        public void Flush()
        {
            _accessor.Flush();
        }

        public unsafe TRecord GetRecord(ushort offset, ushort size)
        {
            TRecord r = default(TRecord);
            _accessor.QueueByteArrayOperation(offset, size,
                b =>
                {
                   
                     r = _serializer.DeserializeFixedSize(b, size);
                  
                });
            return r;
        }

        public unsafe void SetRecord(ushort offset, ushort size, TRecord record)
        {
            _accessor.QueueByteArrayOperation(offset, size,
                b =>
                {
                   _serializer.SerializeFixedSize(b, size,record);

                });
            
        }
    }
}
