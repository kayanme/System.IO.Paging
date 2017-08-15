﻿using File.Paging.PhysicalLevel.Contracts;
using File.Paging.PhysicalLevel.Implementations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;

namespace Test.Pager.Headers
{
    [TestClass]
    public class VariableRecordPageHeadersTest
    {
        public TestContext TestContext { get; set; }

        private IPageHeaders Create(byte[] page)
        {
         
            
            var m = new MockRepository().StrictMock<IPageAccessor>();
            
            m.Expect(k => k.GetByteArray(0, page.Length)).Return(page);
            m.Expect(k => k.PageSize).Repeat.Any().Return(page.Length);
            m.Replay();
            var p = new VariableRecordPageHeaders(m, false);
            TestContext.Properties.Add("page", m);
            
            return p;
        }
        private IPageAccessor Page => TestContext.Properties["page"] as IPageAccessor;
        
        [TestMethod]
        public void FreePage_ThatNotFree()
        {
            var pageContent = new byte[] { 0x10, 0x02, 0, 0, 0, 0, 0, 0 };
            var headers = Create(pageContent);
            Page.BackToRecord();
            Page.Expect(k => k.SetByteArray(new byte[] { 0 }, 0, 1));
            Page.Replay();
            headers.FreeRecord(0);
            Page.VerifyAllExpectations();
        }

        [TestMethod]
        public void FreePage_ThatIsFree()
        {
            var pageContent = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 };
            var headers = Create(pageContent);
            Page.BackToRecord();
         //   page.Expect(k => k.SetByteArray(new byte[] { 0 }, 0 + shift, 1));
            Page.Replay();
            headers.FreeRecord(0);
            Page.VerifyAllExpectations();
        }


        [TestMethod]
        public void AcquirePage_WhenAvailable()
        {
            var pageContent = new byte[] { 0x10, 0x02, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            var headers = Create(pageContent);
            Page.BackToRecord();
            Page.Expect(k => k.SetByteArray(new byte[] { 0x10,0x03 }, 4 , 2));
            Page.Replay();
            var pos = headers.TakeNewRecord(1,3);
            Assert.AreEqual(1, pos);
            Assert.AreEqual(6, headers.RecordShift(1));
            Assert.AreEqual(3, headers.RecordSize(1));
            Page.VerifyAllExpectations();
        }


        [TestMethod]
        public void AcquirePage_WhenNotAvailable()
        {
            var pageContent = new byte[] { 0x10, 0x06, 0, 0, 0, 0, 0, 0, 0x10, 0x06, 0, 0, 0, 0, 0, 0 };
            var headers = Create(pageContent);

            var pos = headers.TakeNewRecord(1,7);
            Assert.AreEqual(-1, pos);
            Page.VerifyAllExpectations();
        }

        [TestMethod]
        public void AcquirePage_WhenSizeIsnotEnough()
        {
            var pageContent = new byte[] { 0x10, 0x06, 0, 0, 0, 0, 0, 0, 0x10, 0x05, 0, 0, 0, 0, 0, 0,0,0,0 };
            var headers = Create(pageContent);

            var pos = headers.TakeNewRecord(1, 7);
            Assert.AreEqual(-1, pos);
            Page.VerifyAllExpectations();
        }


        [TestMethod]
        public void IsPageFree_ThatNotFree()
        {
            var pageContent = new byte[] { 0x10, 0x07, 0, 0, 0, 0, 0, 0 };
            var headers = Create(pageContent);

            var isFree = headers.IsRecordFree(0);
            Page.VerifyAllExpectations();
            Assert.AreEqual(isFree, false);
        }

        [TestMethod]
        public void IsPageFree_ThatIsFree()
        {
            var pageContent = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 };
            var headers = Create(pageContent);

            var isFree = headers.IsRecordFree(0);
            Page.VerifyAllExpectations();
            Assert.AreEqual(isFree, true);
        }


        [TestMethod]
        public void InitialRead()
        {
            var pageContent = new byte[] { 0x10, 0x02, 0, 0, 0x20, 0x03, 0, 0,0,0 };
            var headers = Create(pageContent);

            
            Page.VerifyAllExpectations();
            Assert.AreEqual(2, headers.RecordShift(0));
            Assert.AreEqual(2, headers.RecordSize(0));
            Assert.AreEqual(1, headers.RecordType(0));
            Assert.AreEqual(6 , headers.RecordShift(1));
            Assert.AreEqual(3, headers.RecordSize(1));
            Assert.AreEqual(2, headers.RecordType(1));

            Assert.AreEqual(2, headers.RecordCount);
            Assert.AreEqual(9, headers.TotalUsedSize);
        }
    }
}