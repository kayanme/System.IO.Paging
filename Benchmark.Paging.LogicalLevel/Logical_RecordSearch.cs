﻿using System;
using System.Linq;
using BenchmarkDotNet.Attributes;
using System.IO.Paging.PhysicalLevel.Configuration;
using System.IO.Paging.PhysicalLevel.Classes.Pages;
using System.IO.Paging.PhysicalLevel.Contracts;
using System.IO.Paging.LogicalLevel.Classes;
using System.IO.Paging.LogicalLevel.Configuration;
using System.IO.Paging.LogicalLevel.Contracts;
using System.IO.Paging.PhysicalLevel.Classes.Pages.Contracts;
using System.IO.Paging.LogicalLevel;

namespace Benchmark.Paging.LogicalLevel
{
    public class Logical_RecordSearch
    {

        IPageManager _manager;
        [Params(PageManagerConfiguration.PageSize.Kb4, PageManagerConfiguration.PageSize.Kb8)]
        public PageManagerConfiguration.PageSize PageSize;

        private class Config : LogicalPageManagerConfiguration
        {

            public Config(PageManagerConfiguration.PageSize pageSize) : base(pageSize)
            {
                DefinePageType(1)
                    .AsPageWithRecordType<TestRecord>()
                    .UsingRecordDefinition((ref TestRecord t, byte[] b) => { t.FillByteArray(b); }, (byte[] b, ref TestRecord t) => { t.FillFromByteArray(b); }, 4)
                    .ApplyLogicalSortIndex()
                    .ApplyRecordOrdering((a) => a.Order);

                DefinePageType(2)
                    .AsPageWithRecordType<TestRecord>()
                    .UsingRecordDefinition((ref TestRecord t, byte[] b) => { t.FillByteArray(b); }, (byte[] b, ref TestRecord t) => { t.FillFromByteArray(b); }, 4);

                DefinePageType(3).AsPageWithRecordType<TestRecord>()
                    .UsingRecordDefinition((ref TestRecord t, byte[] b) => { t.FillByteArray(b); }, (byte[] b, ref TestRecord t) => { t.FillFromByteArray(b); }, 4)
                    .AsVirtualHeapPage(4);
            }
        }


        private IPage<TestRecord>[] _pages;

        private int[] _data;
        private int _count;
        private readonly Random _rnd = new Random();
        [GlobalSetup]
        public void Init()
        {
            var config = new Config(PageSize);

            _manager = new LogicalPageManagerFactory().CreateManager("RecordSearch", config, true);
            _pages = new IPage<TestRecord>[5];
            _pages[0] = _manager.GetRecordAccessor<TestRecord>(_manager.CreatePage(1));
          
            while (_pages[0].AddRecord(new TestRecord { Order = _rnd.Next(100) }) != null) ;

            _data = Enumerable.Range(0, 1000).Select(_ => _rnd.Next(100)).ToArray();
        }
        [GlobalCleanup]
        public void Clean()
        {
            _manager.Dispose();
        }

        [Benchmark]
        public void Search()
        {
            var d = _data[_count];
            var e = _pages[0] as IOrderedPage<TestRecord, int>;
            var r = e.FindByKey(d);
            _count = (_count + 1) % _data.Length;
        }

        [Benchmark]
        public void Scan()
        {
            var d = _data[_count];
            var e = _pages[0] as IOrderedPage<TestRecord, int>;
            var r = e.IterateRecords().FirstOrDefault(k=>k.Data.Order == d);
            _count = (_count + 1) % _data.Length;
        }

        [Benchmark]
        public void SearchRange()
        {
            var d = _data[_count];
            var d2 = _data[_count + 1];
            if (d > d2)
            {
                var t = d2;
                d2 = d;
                d = t;
            }
            var e = _pages[0] as IOrderedPage<TestRecord, int>;
            var r = e.FindInKeyRange(d,d2);
            _count = (_count + 2) % _data.Length;
        }

        [Benchmark]
        public void ScanForRange()
        {
            var d = _data[_count];
            var d2 = _data[_count + 1];
            if (d > d2)
            {
                var t = d2;
                d2 = d;
                d = t;
            }
            var e = _pages[0] as IOrderedPage<TestRecord, int>;
            var r = e.IterateRecords().SkipWhile(k => k.Data.Order != d).TakeWhile(k=>k.Data.Order != d2).ToArray();
            _count = (_count + 2) % _data.Length;
        }
    }
}
