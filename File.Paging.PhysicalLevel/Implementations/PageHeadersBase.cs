﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Pager.Contracts;
using Pager.Exceptions;

namespace Pager.Implementations
{
    internal abstract class PageHeadersBase : IPageHeaders
    {

      
        private const byte RecordUseMask = 0xFF;

        protected abstract  int[] _recordInfo { get; }
        


        public ushort RecordCount => (ushort)_recordInfo.Count(k => RecordType(0) != 0);

       

      

        protected abstract IEnumerable<int> PossibleRecordsToInsert();

        protected abstract void SetFree(ushort record);

        protected abstract ushort SetUsed(ushort record, ushort size, byte type);

        protected abstract void UpdateUsed(ushort record,ushort shift, ushort size, byte type);

        public void FreeRecord(ushort record)
        {

            Thread.BeginCriticalRegion();
            var r = _recordInfo[record];
            var newInf = FormRecordInf(0, RecordSize(record), RecordShift(record));
            if (Interlocked.CompareExchange(ref _recordInfo[record], newInf, r) == r)
            {
                SetFree(record);                
            }
            else
                throw new RecordWriteConflictException();
            Thread.EndCriticalRegion();

        }

        private const uint ShiftMask = 0xFFFC0000;
        private const uint SizeMask = 0x0003FFF0;
        private const uint TypeMask = 0x0000000F;

        [MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
        public virtual ushort RecordShift(ushort record) => (ushort)((_recordInfo[record] & ShiftMask) >> 18);//14 бит = 16384
        [MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
        public byte RecordType(ushort record) => (byte)(_recordInfo[record] & TypeMask);//4 бит = 16
        [MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
        public ushort RecordSize(ushort record) => (ushort)((_recordInfo[record] & SizeMask) >> 4);//14 бит = 16384


        public bool IsRecordFree(ushort record)
        {
            return RecordType(record) == 0; ;
        }

        protected abstract ushort TotalRecords { get; }
        protected virtual void SetNewLogicalRecordNum(ushort logicalRecordNum,ushort shift)
        {
            throw new InvalidOperationException("Not supported");
        }
        protected int FormRecordInf(byte rType, ushort rSize, ushort rShift) => (rShift << 18) | (rSize << 4) | (rType);
        public short TakeNewRecord(byte rType,ushort rSize)
        {
         
            Thread.BeginCriticalRegion();                     
            short index = -1;
            foreach (var i in PossibleRecordsToInsert())
            {
                var it = FormRecordInf(rType, rSize, ushort.MaxValue);
                if (Interlocked.CompareExchange(ref _recordInfo[i], it, 0) == 0)
                {                    
                    var shift = SetUsed((ushort)i, rSize, rType);
                    if (shift == ushort.MaxValue)//если запись данного размера не влезает в свободое место
                    {
                        _recordInfo[i] = 0;
                        break;                    
                    }                  
                    else
                    {
                        _recordInfo[i] = FormRecordInf(rType, rSize, shift);
                        index = (short)i;
                        break;
                    }
                }
            };
            Thread.EndCriticalRegion();
            if (index!=-1)
            {
                return (short)(index);
            }
            else
            {
                return -1;
            }

        }

        public IEnumerable<ushort> NonFreeRecords()=>  _recordInfo.Where((k,i) => RecordType((ushort)i) != 0).Select((k, i) => (ushort)i);

        public void SetNewRecordInfo(ushort recordNum,ushort rSize, byte rType)
        {
            var oldInf = _recordInfo[recordNum];
            var shift = RecordShift(recordNum);
            var t = FormRecordInf(rType, rSize, shift);
            if (Interlocked.CompareExchange(ref _recordInfo[recordNum], t, oldInf) != oldInf)
                throw new RecordWriteConflictException();
            UpdateUsed(recordNum, shift, rSize, rType);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void SwapRecords(ushort recordOne, ushort recordTwo)
        {
            if (RecordType(recordOne) == 0 || RecordType(recordTwo) == 0)
                throw new InvalidOperationException();
            int oldOne;
            int oldTwo;

            oldTwo = _recordInfo[recordTwo];
            oldOne = Interlocked.Exchange(ref _recordInfo[recordOne], _recordInfo[recordTwo]);
            if (Interlocked.CompareExchange(ref _recordInfo[recordTwo], oldOne, oldTwo) != oldTwo)
            {
                Interlocked.CompareExchange(ref _recordInfo[recordOne], oldOne, oldTwo);
                throw new RecordWriteConflictException();
            }
            SetNewLogicalRecordNum(recordOne, RecordShift(recordOne));
            SetNewLogicalRecordNum(recordTwo, RecordShift(recordTwo));

        }
    }
}