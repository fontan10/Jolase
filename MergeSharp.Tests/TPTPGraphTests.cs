using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace MergeSharp.Tests;

public class TPTPGraphTests
{
    [Fact]
    public void SingleGraph()
    {
        TPTPGraph graph = new();
        var v1 = Guid.NewGuid();
        var v2 = Guid.NewGuid();
        Assert.False(graph.AddEdge(v1, v2));
        Assert.Empty(graph.Edges);

        graph.AddVertex(v1);
        Assert.Equal(graph.Vertices, new[] { v1 });

        Assert.False(graph.AddEdge(v1, v2));
        Assert.False(graph.AddEdge(v2, v1));

        graph.AddVertex(v2);
        Assert.Equal(graph.Vertices.ToHashSet(), new HashSet<Guid> { v1, v2 });

        Assert.True(graph.AddEdge(v1, v2));
        Assert.True(graph.AddEdge(v1, v2));
        Assert.True(graph.AddEdge(v1, v1));
        Assert.True(graph.AddEdge(v2, v1));

        Assert.Equal(graph.Edges.ToHashSet(),
                        new HashSet<(Guid, Guid)> {
                            (v1, v2),
                            (v1, v1),
                            (v2, v1)
                         });

        Assert.False(graph.RemoveVertex(v1));

        Assert.False(graph.RemoveEdge(v2, v2));
        Assert.True(graph.RemoveEdge(v1, v2));
        Assert.False(graph.RemoveEdge(v1, v2));
        Assert.True(graph.RemoveEdge(v2, v1));

        Assert.False(graph.RemoveVertex(v1));
        Assert.True(graph.RemoveVertex(v2));
    }

    [Fact]
    public void MultipleGraphs1()
    {
        var v1 = Guid.NewGuid();
        var v2 = Guid.NewGuid();

        TPTPGraph first = new();
        TPTPGraph second = new();

        first.AddVertex(v1);
        first.AddVertex(v2);

        second.ApplySynchronizedUpdate(first.GetLastSynchronizedUpdate());

        first.RemoveVertex(v1);
        second.AddEdge(v1, v2);

        Assert.Equal(new[] { v2 }, first.Vertices);
        Assert.Empty(first.Edges);

        Assert.Equal(new HashSet<Guid> { v1, v2 }, second.Vertices.ToHashSet());
        Assert.Equal(new[] { (v1, v2) }, second.Edges);

        first.ApplySynchronizedUpdate(second.GetLastSynchronizedUpdate());
        second.ApplySynchronizedUpdate(first.GetLastSynchronizedUpdate());

        Assert.Equal(first.Vertices, second.Vertices);
        Assert.Equal(first.Edges, second.Edges);

        Assert.Equal(new[] { v2 }, first.Vertices);
        Assert.Empty(first.Edges);
    }

    [Fact]
    public void MultipleGraphs2()
    {
        var v1 = Guid.NewGuid();
        var v2 = Guid.NewGuid();

        TPTPGraph first = new();
        TPTPGraph second = new();

        first.AddVertex(v1);
        first.AddVertex(v2);

        second.ApplySynchronizedUpdate(first.GetLastSynchronizedUpdate());

        first.RemoveVertex(v1);
        second.AddEdge(v1, v2);

        first.ApplySynchronizedUpdate(second.GetLastSynchronizedUpdate());
        second.ApplySynchronizedUpdate(first.GetLastSynchronizedUpdate());

        second.AddVertex(v1);

        first.ApplySynchronizedUpdate(second.GetLastSynchronizedUpdate());
        second.ApplySynchronizedUpdate(first.GetLastSynchronizedUpdate());

        Assert.Equal(first.Vertices, second.Vertices);
        Assert.Equal(first.Edges, second.Edges);

        Assert.Equal(new[] { v2 }, first.Vertices);
        Assert.Empty(first.Edges);
    }

    [Fact]
    public void JsonTPTPGraph()
    {
        TPTPGraph graph = new();
        var v1 = Guid.NewGuid();
        var v2 = Guid.NewGuid();

        graph.AddVertex(v1);
        graph.AddVertex(v2);

        graph.AddEdge(v1, v2);
        graph.AddEdge(v2, v1);

        // NOTE: we cannot deserialize the string back into a TPTPGraph because
        // it does not contain all the info needed (private _vertices or _edges)
        // it just contains the public IEnumerable Vertices and Edges
        string jsonString = JsonConvert.SerializeObject(graph);
        Assert.Contains("\"Vertices\":[", jsonString);
        Assert.Contains(v1.ToString(), jsonString);
        Assert.Contains(v2.ToString(), jsonString);

        Assert.Contains("\"Edges\":[", jsonString);
        Assert.Contains($"{{\"Item1\":\"{v1.ToString()}\",\"Item2\":\"{v2.ToString()}\"}}", jsonString);
        Assert.Contains($"{{\"Item1\":\"{v2.ToString()}\",\"Item2\":\"{v1.ToString()}\"}}", jsonString);
    }
}

public class TPTPGraphMsgTests
{
    [Fact]
    public void EncodeDecode()
    {
        TPTPGraph graph1 = new();
        var v1 = Guid.NewGuid();
        graph1.AddVertex(v1);

        TPTPGraph graph2 = new();
        var v2 = Guid.NewGuid();
        graph2.AddVertex(v2);

        var encodedMsg2 = graph2.GetLastSynchronizedUpdate().Encode();
        TPTPGraphMsg decodedMsg2 = new();
        decodedMsg2.Decode(encodedMsg2);

        graph1.ApplySynchronizedUpdate(decodedMsg2);

        Assert.Equal(graph1.Vertices, new List<Guid> { v1, v2 });
    }
}
