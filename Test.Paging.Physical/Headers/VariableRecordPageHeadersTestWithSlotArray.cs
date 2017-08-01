﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pager;
using Pager.Contracts;
using Pager.Implementations;
using Rhino.Mocks;

namespace Test.Pager
{
    [TestClass]
    public class VariableRecordPageHeadersTestWithSlotInfo
    {
        public TestContext TestContext { get; set; }

        private IPageHeaders Create(byte[] page)
        {
            var m = new MockRepository().StrictMock<IPageAccessor>();            
            m.Expect(k => k.GetByteArray(0, page.Length)).Return(page);
            m.Expect(k => k.PageSize).Repeat.Any().Return(page.Length);
            m.Replay();
            var p = new VariableRecordPageHeaders(m,true);
            TestContext.Properties.Add("page", m);
            return p;
        }
        private IPageAccessor page => TestContext.Properties["page"] as IPageAccessor;

        [TestMethod]
        public void FreePage_ThatNotFree()
        {
            var pageContent = new byte[] { 0x10, 0x02, 0, 0, 0, 0, 0, 0 };
            var headers = Create(pageContent);
            page.BackToRecord();
            page.Expect(k => k.SetByteArray(new byte[] { 0 }, 0, 1));
            page.Replay();
            headers.FreeRecord(0);
            page.VerifyAllExpectations();
        }

        [TestMethod]
        public void FreePage_ThatIsFree()
        {
            var pageContent = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 };
            var headers = Create(pageContent);
            page.BackToRecord();
        //    page.Expect(k => k.SetByteArray(new byte[] { 0 }, 0, 1));
            page.Replay();
            headers.FreeRecord(0);
            page.VerifyAllExpectations();
        }


        [TestMethod]
        public void AcquirePage_WhenAvailable()
        {
            var pageContent = new byte[] { 0x10, 0x02, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            var headers = Create(pageContent);
            page.BackToRecord();
           page.Expect(k => k.SetByteArray(new byte[] { 0x10,0x03,0x0,0x01 }, 6, 4));
            page.Replay();
            var pos = headers.TakeNewRecord(1,3);
            Assert.AreEqual(1, pos);
            Assert.AreEqual(10, headers.RecordShift(1));
            Assert.AreEqual(3, headers.RecordSize(1));
            page.VerifyAllExpectations();
        }


        [TestMethod]
        public void AcquirePage_WhenNotAvailable()
        {
            var pageContent = new byte[] { 0x10, 0x06, 0, 0, 0, 0, 0, 0, 0x10, 0x06, 0, 0x01, 0, 0, 0, 0 };
            var headers = Create(pageContent);

            var pos = headers.TakeNewRecord(1,7);
            Assert.AreEqual(-1, pos);
            page.VerifyAllExpectations();
        }

        [TestMethod]
        public void AcquirePage_WhenSizeIsnotEnough()
        {
            var pageContent = new byte[] { 0x10, 0x04, 0, 0, 0, 0, 0, 0, 0x10, 0x03, 0, 0x01, 0, 0, 0, 0,0,0,0 };
            var headers = Create(pageContent);

            var pos = headers.TakeNewRecord(1, 7);
            Assert.AreEqual(-1, pos);
            page.VerifyAllExpectations();
        }


        [TestMethod]
        public void IsPageFree_ThatNotFree()
        {
            var pageContent = new byte[] { 0x10, 0x07, 0, 0, 0, 0, 0, 0,0,0,0 };
            var headers = Create(pageContent);

            var isFree = headers.IsRecordFree(0);
            page.VerifyAllExpectations();
            Assert.AreEqual(isFree, false);
        }

        [TestMethod]
        public void IsPageFree_ThatIsFree()
        {
            var pageContent = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 };
            var headers = Create(pageContent);

            var isFree = headers.IsRecordFree(0);
            page.VerifyAllExpectations();
            Assert.AreEqual(isFree, true);
        }


        [TestMethod]
        public void InitialRead()
        {
            var pageContent = new byte[] { 0x10, 0x02, 0, 0x01,0,0, 0x20, 0x03, 0, 0,0,0 ,0,0};
            var headers = Create(pageContent);

            
            page.VerifyAllExpectations();
            Assert.AreEqual(10, headers.RecordShift(0));
            Assert.AreEqual(3, headers.RecordSize(0));
            Assert.AreEqual(2, headers.RecordType(0));

            Assert.AreEqual(4, headers.RecordShift(1));
            Assert.AreEqual(2, headers.RecordSize(1));
            Assert.AreEqual(1, headers.RecordType(1));
            
        }

        [TestMethod]
        public void SwapRecords()
        {
            var pageContent = new byte[] { 0x10, 0x02, 0, 0x01, 0, 0, 0x20, 0x03, 0, 0, 0, 0, 0, 0 };
            var headers = Create(pageContent);
            
            page.BackToRecord();
            page.Expect(k => k.SetByteArray(new byte[] { 0,0 }, 2, 2));
            page.Expect(k => k.SetByteArray(new byte[] { 0,0x01 }, 8, 2));
            page.Replay();

            headers.SwapRecords(0, 1);
            page.VerifyAllExpectations();

            Assert.AreEqual(10, headers.RecordShift(1));
            Assert.AreEqual(3, headers.RecordSize(1));
            Assert.AreEqual(2, headers.RecordType(1));

            Assert.AreEqual(4, headers.RecordShift(0));
            Assert.AreEqual(2, headers.RecordSize(0));
            Assert.AreEqual(1, headers.RecordType(0));
        }
    }
}
