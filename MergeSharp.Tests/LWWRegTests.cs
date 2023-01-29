using Xunit;

namespace MergeSharp.Tests;

public class LWWRegTests
{
    [Fact]
    public void LWWRegSingle()
    {
        LWWRegister<int> reg = new LWWRegister<int>();
        Assert.Equal(0, reg.Value);

        reg.Value = 5;
        Assert.Equal(5, reg.Value);

        reg.Value = -1;
        Assert.Equal(-1, reg.Value);

        reg.Value = 5;
        Assert.Equal(5, reg.Value);
    }

    [Fact]
    public void LWWRegMerge()
    {
        LWWRegister<int> reg1 = new LWWRegister<int>();
        LWWRegister<int> reg2 = new LWWRegister<int>();

        reg1.Value = 5;

        var reg2Msg = reg2.GetLastSynchronizedUpdate();
        reg1.ApplySynchronizedUpdate(reg2Msg);

        Assert.Equal(5, reg1.Value);

        reg1.Value = 6;
        reg2.Value = 7;

        reg2Msg = reg2.GetLastSynchronizedUpdate();
        reg1.ApplySynchronizedUpdate(reg2Msg);
        Assert.Equal(7, reg1.Value);

        var reg1Msg = reg1.GetLastSynchronizedUpdate();
        reg2.ApplySynchronizedUpdate(reg1Msg);
        Assert.Equal(7, reg2.Value);
    }

    [Fact]
    public void LWWRegMerge2()
    {
        LWWRegister<int> reg1 = new LWWRegister<int>();
        LWWRegister<int> reg2 = new LWWRegister<int>(6);

        var reg2Msg = reg2.GetLastSynchronizedUpdate();
        reg1.ApplySynchronizedUpdate(reg2Msg);

        Assert.Equal(6, reg1.Value);
    }
}

public class LWWRegMsgTests
{
    [Fact]
    public void EncodeDecode()
    {
        LWWRegister<int> reg1 = new();
        reg1.Value = 5;

        LWWRegister<int> reg2 = new(6);

        var encodedMsg2 = reg2.GetLastSynchronizedUpdate().Encode();
        LWWRegisterMsg<int> decodedMsg2 = new();
        decodedMsg2.Decode(encodedMsg2);

        reg1.ApplySynchronizedUpdate(decodedMsg2);

        Assert.Equal(6, reg1.Value);
    }
}
