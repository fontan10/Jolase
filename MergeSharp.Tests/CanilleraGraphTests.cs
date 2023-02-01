using Xunit;
using System;
using System.Linq;
using System.Collections.Generic;


namespace MergeSharp.Tests;

public class CanilleraGraphTests
{
    [Fact]
    public void SingleGraphFalseyChecks()
    {
        CanilleraGraph graph = new();

        var v1 = Guid.NewGuid();
        var v2 = Guid.NewGuid();

        Assert.False(graph.AddEdge(new CanilleraGraph.Edge(v1, v2)));
        Assert.False(graph.EdgeCounts().ContainsKey(new CanilleraGraph.Edge(v1, v2)));
    }

    [Fact]
    public void SingleGraphMultipleSameEdge()
    {
        CanilleraGraph graph = new();

        var v1 = Guid.NewGuid();

        graph.AddVertex(v1);
        graph.AddEdge(v1, v1);
        graph.AddEdge(v1, v1);
        graph.AddEdge(v1, v1);

        Assert.Equal(3, graph.EdgeCounts().Sum(x => x.Value));
    }

    [Fact]
    public void SingleGraphMultipleSameVertexAdd()
    {
        CanilleraGraph graph = new();

        var v1 = Guid.NewGuid();

        graph.AddVertex(v1);
        graph.AddVertex(v1);
        graph.AddVertex(v1);

        Assert.Equal(1, graph.LookupVertices().Count());
    }

    [Fact]
    public void SecondGraphMatchFirst()
    {
        CanilleraGraph cg1 = new CanilleraGraph();
        CanilleraGraph cg2 = new CanilleraGraph();

        Guid v1 = Guid.NewGuid();
        Guid v2 = Guid.NewGuid();
        Guid v3 = Guid.NewGuid();

        cg1.AddVertex(v1);
        cg1.AddVertex(v2);
        cg1.AddEdge(v1, v2);

        cg2.ApplySynchronizedUpdate(cg1.GetLastSynchronizedUpdate());

        Assert.Equal(cg2.LookupVertices(), cg1.LookupVertices());
        Assert.Equal(cg2.EdgeCounts(), cg1.EdgeCounts());
    }

    [Fact]
    public void CGMerge2()
    {
        CanilleraGraph cg1 = new CanilleraGraph();
        CanilleraGraph cg2 = new CanilleraGraph();

        Guid v1 = Guid.NewGuid();
        Guid v2 = Guid.NewGuid();

        cg1.AddVertex(v1);
        cg1.AddVertex(v2);
        cg1.AddEdge(v1, v2);

        cg2.ApplySynchronizedUpdate(cg1.GetLastSynchronizedUpdate());

        // both sync'd graphs:
        // v1 -> v2

        cg1.AddEdge(v1, v2);
        cg2.AddEdge(v1, v2);

        // both graphs added their own second edge:
        // v1 ---> v2
        //  |       ^
        //  |_______|

        cg1.ApplySynchronizedUpdate(cg2.GetLastSynchronizedUpdate());
        cg2.ApplySynchronizedUpdate(cg1.GetLastSynchronizedUpdate());

        Assert.Equal(cg2.LookupVertices(), cg1.LookupVertices());
        Assert.Equal(cg2.EdgeCounts(), cg1.EdgeCounts());
        Assert.Equal(2, cg2.LookupVertices().Count());
        Assert.Equal(3, cg2.EdgeCounts().Sum(x => x.Value));

        // should only remove a single edge out of the three
        cg2.RemoveEdge(v1, v2);
        Assert.Equal(2, cg2.EdgeCounts().Sum(x => x.Value));
    }
}

public class CanilleraGraphComplicatedTests
{
    CanilleraGraph cg1, cg2, cg3;

    Guid v1, v2, v3, v4;

    public CanilleraGraphComplicatedTests()
    {
        cg1 = new CanilleraGraph();
        cg2 = new CanilleraGraph();
        cg3 = new CanilleraGraph();
        v1 = Guid.NewGuid();
        v2 = Guid.NewGuid();
        v3 = Guid.NewGuid();
        v4 = Guid.NewGuid();
    }

    [Fact]
    public void Merge1()
    {
        cg1.AddVertex(v1);
        cg1.AddVertex(v2);
        cg1.AddEdge(v1, v2);

        cg2.AddVertex(v3);
        cg2.AddVertex(v4);

        cg1.ApplySynchronizedUpdate(cg2.GetLastSynchronizedUpdate());

        cg1.AddEdge(v1, v3);

        cg2.ApplySynchronizedUpdate(cg1.GetLastSynchronizedUpdate());

        Assert.Equal(cg1.LookupVertices().OrderBy(x => x), cg2.LookupVertices().OrderBy(x => x)); // graphs should have the same vertices
        Assert.Equal(cg1.EdgeCounts().OrderBy(x => x.Key.ToString()), cg2.EdgeCounts().OrderBy(x => x.Key.ToString())); // graphs should have the same number of edges
        Assert.Equal(cg1.LookupVertices().Count(), 4);
        Assert.Equal(cg1.EdgeCounts().Sum(x => x.Value), 2); // total of 2 edges in the graph
    }

    [Fact]
    public void ConcurrentRemoveVertexAndAddEdge()
    {
        cg1.AddVertex(v1);
        cg1.AddVertex(v2);

        cg2.ApplySynchronizedUpdate(cg1.GetLastSynchronizedUpdate());

        cg1.RemoveVertex(v1);
        cg2.AddEdge(v1, v1);

        cg1.ApplySynchronizedUpdate(cg2.GetLastSynchronizedUpdate());
        cg2.ApplySynchronizedUpdate(cg1.GetLastSynchronizedUpdate());

        Assert.Equal(cg1.LookupVertices().OrderBy(x => x), cg2.LookupVertices().OrderBy(x => x));
        Assert.Equal(cg1.EdgeCounts().OrderBy(x => x.Key.ToString()), cg2.EdgeCounts().OrderBy(x => x.Key.ToString()));
        Assert.Equal(cg1.LookupVertices().Count(), 2);
        Assert.Equal(cg1.EdgeCounts().Sum(x => x.Value), 1);
    }

    [Fact]
    public void MultipleConcurrentAddAndRemoveEdges()
    {
        cg1.AddVertex(v1);
        cg1.AddEdge(v1, v1);

        cg2.ApplySynchronizedUpdate(cg1.GetLastSynchronizedUpdate());
        cg3.ApplySynchronizedUpdate(cg1.GetLastSynchronizedUpdate());

        // All graphs have v1 and edge from v1->v1

        cg1.AddEdge(v1, v1);
        cg2.AddEdge(v1, v1);
        cg3.RemoveEdge(v1, v1);

        cg1.ApplySynchronizedUpdate(cg2.GetLastSynchronizedUpdate());
        cg1.ApplySynchronizedUpdate(cg3.GetLastSynchronizedUpdate());

        Assert.Equal(cg1.LookupVertices().Count(), 1);
        Assert.Equal(cg1.EdgeCounts().Sum(x => x.Value), 2);
    }

    [Fact]
    public void AddEdgeWithNegativeCount()
    {
        cg1.AddVertex(v1);
        cg1.AddEdge(v1, v1);

        cg2.ApplySynchronizedUpdate(cg1.GetLastSynchronizedUpdate());
        cg3.ApplySynchronizedUpdate(cg1.GetLastSynchronizedUpdate());

        cg1.RemoveEdge(v1, v1);
        cg2.RemoveEdge(v1, v1);
        cg3.RemoveEdge(v1, v1);

        cg1.ApplySynchronizedUpdate(cg2.GetLastSynchronizedUpdate());
        cg1.ApplySynchronizedUpdate(cg3.GetLastSynchronizedUpdate());

        Assert.Equal(cg1.LookupVertices().Count(), 1);
        Assert.Equal(cg1.EdgeCounts().Sum(x => x.Value), 0); // count should not be negative

        cg1.AddEdge(v1, v1);
        Assert.Equal(cg1.EdgeCounts().Sum(x => x.Value), 1);
    }
}

public class CanilleraGraphMsgTests
{
    [Fact]
    public void EncodeDecode()
    {
        CanilleraGraph graph1 = new();
        CanilleraGraph graph2 = new();

        var v1 = Guid.NewGuid();
        var v2 = Guid.NewGuid();
        var v3 = Guid.NewGuid();

        graph1.AddVertex(v1);
        graph1.AddVertex(v2);
        graph1.AddEdge(new CanilleraGraph.Edge(v1, v2));

        graph2.AddVertex(v1);
        graph2.AddVertex(v2);
        graph2.AddVertex(v3);
        graph2.AddEdge(new CanilleraGraph.Edge(v1, v2));
        graph2.AddEdge(new CanilleraGraph.Edge(v2, v3));

        var encodedMsg2 = graph2.GetLastSynchronizedUpdate().Encode();
        CanilleraGraphMsg decodedMsg2 = new();
        decodedMsg2.Decode(encodedMsg2);

        graph1.ApplySynchronizedUpdate(decodedMsg2);

        Assert.Equal(new List<Guid>() { v1, v2, v3 }, graph1.LookupVertices());
    }
}
