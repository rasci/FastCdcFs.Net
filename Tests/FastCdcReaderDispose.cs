using FastCdcFs.Net;
using System;
using System.Collections.Generic;
using System.Text;

namespace Tests;

public class FastCdcReaderDispose : TestBase
{

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DoNotDisposeStreamWhenLeaveOpen(bool leaveOpen)
    {
        var ms = new DisposableMemoryStream();
        CreateDefaultFile(ms);

        using (var reader = new FastCdcFsReader(ms, leaveOpen: leaveOpen)) { }

        Assert.NotEqual(leaveOpen, ms.IsDisposed);
    }

    private class DisposableMemoryStream : MemoryStream
    {
        public bool IsDisposed { get; private set; }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            IsDisposed = true;
        }
    }
}
