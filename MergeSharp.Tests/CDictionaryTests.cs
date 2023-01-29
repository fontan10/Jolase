using Xunit;
using System;
using System.Collections.Generic;

namespace MergeSharp.Tests;

public class CDictionaryTests
{
    [Fact]
    public void CDictSingle()
    {
        CDictionary<string> dict = new();
        dict.Remove("a", 1);
        dict.Remove("a", 1);
        dict.Remove("a", 1);

        Assert.Equal(-3, dict["a"]);

        dict["a"] = 3;
        Assert.Equal(3, dict["a"]);

        dict["a"] = -2;
        Assert.Equal(-2, dict["a"]);

        dict.Add("a");
        Assert.Equal(1, dict["a"]);

        dict.Remove("a");
        Assert.Equal(0, dict["a"]);
    }

    [Fact]
    public void CDictMultiple()
    {
        CDictionary<string> dict1 = new();
        CDictionary<string> dict2 = new();

        dict1.Add("a", -9);
        dict2.Add("a", 1);
        dict2.Add("b", -9);
        dict2.Add("b");

        var d1Msg = dict1.GetLastSynchronizedUpdate();
        var d2Msg = dict2.GetLastSynchronizedUpdate();

        dict1.ApplySynchronizedUpdate(d2Msg);
        dict2.ApplySynchronizedUpdate(d1Msg);

        Assert.Equal(dict1.Keys, dict2.Keys);
        Assert.Equal(dict1.Values, dict2.Values);

        Assert.Equal(-8, dict1["a"]);
        Assert.Equal(1, dict1["b"]);
    }

    [Fact]
    public void Empty()
    {
        CDictionary<string> dict = new();
        Assert.Empty(dict);

        dict.Add("a", 2);
        Assert.Equal(new List<string> { "a" }, dict.Keys);
        Assert.NotEmpty(dict);

        dict.Remove("a", 2);
        Assert.NotEmpty(dict);
        Assert.Equal(0, dict["a"]);

        Assert.Throws<KeyNotFoundException>(() => dict["b"]);
    }
}

public class CDictionaryMsgTests
{
    [Fact]
    public void EncodeDecode()
    {
        CDictionary<string> c1 = new();
        c1.Add("a", 4);

        CDictionary<string> c2 = new();
        c2.Add("a");
        c2.Add("b");
        c2.Add("c", -4);

        var encodedMsg2 = c2.GetLastSynchronizedUpdate().Encode();
        CDictionaryMsg<string> decodedMsg2 = new();
        decodedMsg2.Decode(encodedMsg2);

        c1.ApplySynchronizedUpdate(decodedMsg2);

        Assert.Equal(5, c1["a"]);
        Assert.Equal(1, c1["b"]);
        Assert.Equal(-4, c1["c"]);
    }
}
