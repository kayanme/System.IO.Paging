﻿using System.Collections.Generic;
using File.Paging.PhysicalLevel.Classes;

namespace Benchmark.Paging.PhysicalLevel
{
    public class TestRecord : TypedRecord
    {
        public byte[] Values = new byte[7];

        public ushort RecordSize => 7;

        public void FillByteArray(IList<byte> b)
        {
            for (int i = 0; i < RecordSize; i++)
            {
                b[i] = Values[i];
            }
        }

        public void FillFromByteArray(IList<byte> b)
        {

            for (int i = 0; i < RecordSize; i++)
            {
                Values[i] = b[i];
            }
        }
    }
}
