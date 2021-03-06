﻿using System.Collections.Generic;

namespace Test.Paging.PhysicalLevel
{
    public class TestHeader
    {
        public ushort RecordSize => 7;

        public int Value = 0;

        public void FillByteArray(IList<byte> b)
        {
            b[0] = (byte)(Value & 0xFF000000);
            b[1] = (byte)(Value & 0x00FF0000);
            b[2] = (byte)(Value & 0x0000FF00);
            b[3] = (byte)(Value & 0x000000FF);
        }

        public void FillFromByteArray(IList<byte> b)
        {
            Value = b[0] << 24 + b[1] << 16 + b[2] << 8 + b[3];
        }
    }
}
