﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pager.Contracts;

namespace Pager.Classes
{
    public sealed class ComplexRecordTypePage<TRecordType> : TypedPage where TRecordType : TypedRecord, new()
    {

        private IPageHeaders _headers;
        private IPageAccessor _accessor;
        private int _pageSize;
        private VariableRecordTypePageConfiguration<TRecordType> _config;
        internal ComplexRecordTypePage(IPageHeaders headers, IPageAccessor accessor, PageReference reference, int pageSize, VariableRecordTypePageConfiguration<TRecordType> config)
        {
            Reference = reference;
            _headers  = headers;
            _accessor = accessor;
            _config = config;
        }


        private void SetRecord<TType>(int offset, TType record,RecordDeclaration<TType> config) where TType : TRecordType
        {
            var recordStart = _headers.RecordShift((ushort)offset);
            var recordSize = _headers.RecordSize((ushort)offset);
            var bytes = _accessor.GetByteArray(recordStart, recordSize);
            config.FillBytes(record, bytes);
            _accessor.SetByteArray(bytes, recordStart, recordSize);
        }

        private RecordDeclaration<TType> FindConfig<TType>() where TType : TRecordType
        {
            byte t;
            var config = FindConfig<TType>(out t);
            return config;
        }

        private VariableSizeRecordDeclaration<TType> FindConfig<TType>(out byte type) where TType:TRecordType
        {
            var map = _config.RecordMap.FirstOrDefault(k => k.Value is VariableSizeRecordDeclaration<TType>);
            if (map.Value == null)
                throw new InvalidOperationException("No such type in page map");
            var config = map.Value as VariableSizeRecordDeclaration<TType>;
            type = map.Key;
            return config;
        }

        public bool AddRecord<TType>(TType type) where TType : TRecordType
        {
            byte mapKey;
            var config = FindConfig<TType>(out mapKey);
            var record = _headers.TakeNewRecord(mapKey, (ushort)config.GetSize(type));
            if (record == -1)
                return false;
            SetRecord(record, type,config);
            if (type.Reference == null)
                type.Reference = new PageRecordReference { Page = Reference };
            type.Reference.Record = record;
            return true;
        }

        public TRecordType GetRecord(PageRecordReference reference) 
        {
            if (Reference != reference.Page)
                throw new ArgumentException("The record is on another page");

            if (!_headers.IsRecordFree((ushort)reference.Record))
            {
                var offset = _headers.RecordShift((ushort)reference.Record);
                var size = _headers.RecordSize((ushort)reference.Record);
                var type = _headers.RecordType((ushort)reference.Record);
                var bytes = _accessor.GetByteArray(offset, size);
                var r = new TRecordType();
                r.Reference = reference;             
                var config = _config.RecordMap[type] as IVariableSizeRecordDeclaration<TRecordType>;
                config.FillFromBytes(bytes, r);
                return r;
            }
            return null;
        }

        public void StoreRecord<TType>(TType record) where TType:TRecordType
        {
            if (record.Reference.Page != this.Reference)
                throw new ArgumentException();
            var config = FindConfig<TType>();
            SetRecord(record.Reference.Record, record, config);
        }

        public void FreeRecord(TRecordType record)
        {
            if (record.Reference.Record == -1)
                throw new ArgumentException("Trying to delete deleted record");
            _headers.FreeRecord((ushort)record.Reference.Record);
            record.Reference.Record = -1;
        }

      

        public override double PageFullness => 0;

        public override PageReference Reference { get;  }

        public override void Dispose()
        {
            _accessor.Flush();
        }

        public override void Flush()
        {
            _accessor.Flush();
        }
    }
}