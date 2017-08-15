﻿using File.Paging.PhysicalLevel.Classes;

namespace Test.Paging.LogicalLevel
{
    public class TestRecord:TypedRecord
    {
        public int Order;

        public override bool Equals(object obj)
        {
            var t = obj as TestRecord;
            if (t == null)
                return false;
            return Order == t.Order && Reference?.LogicalRecordNum == t.Reference?.LogicalRecordNum;
        }

        public override int GetHashCode()
        {
            return Order.GetHashCode() ^ Reference.LogicalRecordNum.GetHashCode();
        }
    }
}