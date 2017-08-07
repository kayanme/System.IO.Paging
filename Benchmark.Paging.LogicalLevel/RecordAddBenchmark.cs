﻿using System;
using System.IO;
using System.Linq;
using System.Threading;
using BenchmarkDotNet.Attributes;
using File.Paging.PhysicalLevel.Classes.Configurations;
using File.Paging.PhysicalLevel.Classes.Pages;
using File.Paging.PhysicalLevel.Contracts;
using FIle.Paging.LogicalLevel.Classes;
using FIle.Paging.LogicalLevel.Classes.Configurations;

namespace Benchmark.Paging.LogicalLevel
{
    public class RecordAddBenchmark
    {
        IPageManager _manager;
        [Params(PageManagerConfiguration.PageSize.Kb4,PageManagerConfiguration.PageSize.Kb8)]        
        public PageManagerConfiguration.PageSize PageSize;

        private class Config : LogicalPageManagerConfiguration
        {

            public Config(PageSize pageSize) : base(pageSize)
            {
                DefinePageType(1)
                    .AsPageWithRecordType<TestRecord>()
                    .UsingRecordDefinition((t, b) => { t.FillFromByteArray(b); }, (b, t) => { t.FillByteArray(b); }, 4)
                    .ApplyRecordOrdering((a) => a.Order);

                DefinePageType(2)
                    .AsPageWithRecordType<TestRecord>()
                    .UsingRecordDefinition((t, b) => { t.FillFromByteArray(b); }, (b, t) => { t.FillByteArray(b); }, 4);
            }
        }


        private IPage<TestRecord>[] _pages;
        
        private FileStream _other;
        private readonly Random _rnd = new Random();
        [GlobalSetup]
        public void Init()
        {
            var config = new Config( PageSize );
           
            _manager = new LogicalPageManagerFactory().CreateManager("testFile", config,true);
            _pages = new IPage<TestRecord>[4];
            _pages[0] = _manager.CreatePage(1) as IPage<TestRecord>;
            _pages[1] = _manager.CreatePage(2) as IPage<TestRecord>;
            _pages[2] = _manager.CreatePage(1) as IPage<TestRecord>;
            _pages[3] = _manager.CreatePage(2) as IPage<TestRecord>;
            while (_pages[2].AddRecord(new TestRecord { Order = _rnd.Next(1000) })) ;
            while (_pages[3].AddRecord(new TestRecord { Order = _rnd.Next(1000) })) ;
            _other = System.IO.File.Open("testfile2" , FileMode.OpenOrCreate);

        }

        //[Benchmark]
        public void AddRecordWithOrder()
        {
            var t = _rnd.Next(1000);
            var t2 = new TestRecord { Order = t };
            _pages[0].AddRecord(t2);
            
        }

       //[Benchmark(Baseline = true)]
        public void AddRecordWithoutOrder()
        {
            var t = _rnd.Next(1000);
            var t2 = new TestRecord { Order = t };
            _pages[1].AddRecord(t2);
            

        }

        [Benchmark]
        public void ChangeRecordWithOrder()
        {
            var t = _rnd.Next(1000);
            var t2 = _pages[2].IterateRecords().First();
            t2.Order = t;
            _pages[2].StoreRecord(t2);
            
        }

        [Benchmark(Baseline = true)]
        public void ChangeRecordWithoutOrder()
        {
            var t = _rnd.Next(1000);
            var t2 = _pages[3].IterateRecords().First();
            t2.Order = t;
            _pages[3].StoreRecord(t2);

        }

        [GlobalCleanup]
        public void DeleteFile()
        {
            _other.Dispose();
             
            _manager.Dispose();
            
            try
            {
                System.IO.File.Delete("testFile");
                System.IO.File.Delete("testFile2");
            }
            catch
            {

            }
            Thread.Sleep(100);
        }
    }
}
