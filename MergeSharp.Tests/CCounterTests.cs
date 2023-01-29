using Xunit;
using MergeSharp;

namespace MergeSharp.Tests;

public class CCounterTests
{
    [Fact]
    public void CCSingle()
    {
        CCounter cc = new CCounter();
        Assert.Equal(0, cc.Get());

        cc.Increment(5);
        Assert.Equal(5, cc.Get());

        cc.Increment(1);
        Assert.Equal(6, cc.Get());

        cc.Decrement(3);
        Assert.Equal(3, cc.Get());

        cc.Decrement(4);
        Assert.Equal(-1, cc.Get());

        cc.Increment(1);
        Assert.Equal(1, cc.Get());

        cc.Decrement(4);
        Assert.Equal(-3, cc.Get());

        cc.Increment(1);
        Assert.Equal(1, cc.Get());
    }

    [Fact]
    public void CCMerge()
    {
        CCounter cc1 = new CCounter();
        CCounter cc2 = new CCounter();

        cc1.Increment(5);
        cc1.Decrement(8); // -3
        cc1.Increment(10);
        cc1.Decrement(3); // 7

        var cc1Msg = cc1.GetLastSynchronizedUpdate();
        var cc2Msg = cc2.GetLastSynchronizedUpdate();
        cc2.ApplySynchronizedUpdate(cc1Msg);
        cc1.ApplySynchronizedUpdate(cc2Msg);

        Assert.Equal(cc1.Get(), cc2.Get());
        Assert.Equal(4, cc1.Get());
    }

    [Fact]
    public void CCMerge2()
    {
        CCounter cc1 = new CCounter();
        CCounter cc2 = new CCounter();

        cc1.Decrement(10);
        cc1.Increment(1);
        cc2.Increment(1);
        cc2.Decrement(10);
        cc2.Increment(1);

        var cc1Msg = cc1.GetLastSynchronizedUpdate();
        var cc2Msg = cc2.GetLastSynchronizedUpdate();
        cc2.ApplySynchronizedUpdate(cc1Msg);
        cc1.ApplySynchronizedUpdate(cc2Msg);

        Assert.Equal(cc1.Get(), cc2.Get());
        Assert.Equal(2, cc1.Get());
    }

    [Fact]
    public void CCMerge3()
    {
        CCounter cc1 = new CCounter();
        CCounter cc2 = new CCounter();

        cc1.Decrement(10);
        cc1.Increment(1);
        cc2.Increment(1);
        cc2.Decrement(10);

        var cc1Msg = cc1.GetLastSynchronizedUpdate();
        var cc2Msg = cc2.GetLastSynchronizedUpdate();
        cc2.ApplySynchronizedUpdate(cc1Msg);
        cc1.ApplySynchronizedUpdate(cc2Msg);

        Assert.Equal(cc1.Get(), cc2.Get());
        Assert.Equal(-8, cc1.Get());
    }
}

public class CCounterMsgTests
{
    [Fact]
    public void EncodeDecode()
    {
        CCounter c1 = new();
        c1.Increment(5);
        c1.Decrement(2);

        CCounter c2 = new();
        c2.Increment(1);
        c2.Decrement(3);

        var encodedMsg2 = c2.GetLastSynchronizedUpdate().Encode();
        CCounterMsg decodedMsg2 = new();
        decodedMsg2.Decode(encodedMsg2);

        c1.ApplySynchronizedUpdate(decodedMsg2);

        Assert.Equal(1, c1.Get());
    }
}
