using System;
using System.Linq;
using System.IO.Paging.PhysicalLevel.Configuration.Builder;

namespace Durability.Paging.PhysicalLevel
{
    internal class Proc3 : IFixedSizeRecordDefinition<TestRecord>
    {
        public void FillBytes(ref TestRecord r, byte[] b)
        {
            Buffer.BlockCopy(r.Data.ToByteArray(), 0, b, 0, b.Count());

        }

        public void FillFromBytes(byte[] b,ref TestRecord r)
        {
            r.Data = new Guid(b.ToArray());
        }
      

        public int Size => 16;
    }
}