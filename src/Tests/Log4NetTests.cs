﻿using System;
using System.Linq;
using Xunit;
using System.IO;
using LogViewer;
using log4net.Core;
using log4net.Layout;
using LogViewer.Infrastructure;

namespace IntegrationTests
{
    public class Log4NetTests
    {
        private string _buffer =
          @"<log4net:event 
	logger=""IntegrationTests.LogTests"" 
	timestamp=""2011-03-23T21:39:31.3833441+01:00"" 
	level=""ERROR"" 
	thread=""7"" 
	domain=""IsolatedAppDomainHost: IntegrationTests"" 
username=""AWESOMEMACHINE\Administrator"">
<log4net:message>msg</log4net:message>
<log4net:properties>
	<log4net:data name=""log4net:HostName"" value=""AWESOMEMACHINE"" />
</log4net:properties>
<log4net:exception>System.Exception: test</log4net:exception>
<log4net:locationInfo class=""IntegrationTests.LogTests"" 
	method=""TestLog"" 
	file=""C:\projects\LogViewer\IntegrationTests\LogTests.cs"" line=""19"" /></log4net:event>";
        [Fact]
        public void ParseStream()
        {
            using (var s = new MemoryStream())
            using (var w = new StreamWriter(s))
            {
                w.Write(_buffer);
                w.Flush();
                s.Position = 0;
                var entry = new LogEntryParser().Parse(s).Single();
                Assert.Equal("ERROR", entry.Data.Level.Name);
                Assert.Equal(@"AWESOMEMACHINE", entry.HostName);
                Assert.Equal(@"IsolatedAppDomainHost: IntegrationTests", entry.Data.Domain);
                Assert.Equal("msg", entry.Data.Message);
                Assert.Equal("IntegrationTests.LogTests", entry.Class());
                Assert.Equal("TestLog", entry.Method());
                Assert.Equal("19", entry.Line());
                Assert.Equal(@"C:\projects\LogViewer\IntegrationTests\LogTests.cs", entry.File());
            }
        }
        [Fact]
        public void Parse3()
        {
            using (var s = new MemoryStream())
            using (var w = new StreamWriter(s))
            {

                w.Write(_buffer);
                w.Flush();
                s.Position = 0;

                var entry = new LogEntryParser().Parse(s).Single();
                Assert.Equal("ERROR", entry.Data.Level.Name);
                Assert.Equal(@"AWESOMEMACHINE", entry.HostName);
                Assert.Equal(@"IsolatedAppDomainHost: IntegrationTests", entry.Data.Domain);
                Assert.Equal("msg", entry.Data.Message);
                Assert.Equal("IntegrationTests.LogTests", entry.Class());
                Assert.Equal("TestLog", entry.Method());
                Assert.Equal("19", entry.Line());
                Assert.Equal(@"C:\projects\LogViewer\IntegrationTests\LogTests.cs", entry.File());
            }
        }
        [Fact]
        public void ParseStreamAtPosition()
        {
            long p = 0;
            var path = Path.GetTempFileName();
            using (var s = new FileStream(path,
                FileMode.Truncate,
                FileAccess.ReadWrite))
            using (var w = new StreamWriter(s))
            {
                w.Write(_buffer);
                w.Flush();
                s.Position = 0;
                var entry = new LogEntryParser().Parse(s).Single();// read written entry
                p = s.Position;
            }
            using (var s = new FileStream(path,
                FileMode.Append,
                FileAccess.Write))
            using (var w = new StreamWriter(s))
            {
                w.Write(_buffer);
                w.Flush();
            }

            using (var s = FileUtil.OpenReadOnly(path, position: p))
            {
                var entry = new LogEntryParser().Parse(s).Single();
                Assert.Equal("ERROR", entry.Data.Level.Name);
                Assert.Equal(@"AWESOMEMACHINE", entry.HostName);
                Assert.Equal(@"IsolatedAppDomainHost: IntegrationTests", entry.Data.Domain);
                Assert.Equal("msg", entry.Data.Message);
                Assert.Equal("IntegrationTests.LogTests", entry.Class());
                Assert.Equal("TestLog", entry.Method());
                Assert.Equal("19", entry.Line());
                Assert.Equal(@"C:\projects\LogViewer\IntegrationTests\LogTests.cs", entry.File());
            }
        }
        [Fact]
        public void ParseStreamAtPositionShouldThrowException()
        {
            long p = 0;
            var path = Path.GetTempFileName();
            using (var s = new FileStream(path,
                FileMode.Truncate,
                FileAccess.ReadWrite))
            using (var w = new StreamWriter(s))
            {
                w.Write(_buffer);
                w.Flush();
                s.Position = 0;
                var entry = new LogEntryParser().Parse(s).Single();// read written entry
                p = s.Position;
            }
            Assert.Throws<OutOfBoundsException>(() =>
            {
                using (var s = FileUtil.OpenReadOnly(path, position: p * 10))
                {
                    new LogEntryParser().Parse(s);
                }
            });
        }
        [Fact]
        public void Test()
        {
            var layout = new PatternLayout("%date [%thread] %-5level %logger - %message%newline");

            var stringWriter = new StringWriter();
            layout.Format(stringWriter, new LoggingEvent(new LoggingEventData
                                                             {
                                                                 Level = Level.Error,
                                                                 Domain = "Domain",
                                                                 Message = "msg",
                                                                 ThreadName = "thread",
                                                                 LoggerName = "logger",
                                                                 TimeStamp = new DateTime(2001, 1, 1)
                                                             }));
            stringWriter.Flush();
            Assert.Equal("2001-01-01 00:00:00,000 [thread] ERROR logger - msg" + Environment.NewLine, stringWriter.ToString());
        }
    }
}