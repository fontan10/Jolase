using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MergeSharp;

[TypeAntiEntropyProtocol(typeof(CanilleraGraph))]
public class CanilleraGraphMsg : PropagationMessage
{
    [JsonInclude]
    public ORSetMsg<Guid> verticesMsg { get; private set; }

    [JsonInclude]
    // JsonInclude is incompatible with CDictionaryMsg<(Guid, Guid)>,
    // so represent the (Guid, Guid) as a string
    public CDictionaryMsg<string> edgesMsg { get; private set; }

    public CanilleraGraphMsg()
    {
    }

    public CanilleraGraphMsg(ORSet<Guid> vertices, CDictionary<string> edges)
    {
        this.verticesMsg = (ORSetMsg<Guid>) vertices.GetLastSynchronizedUpdate();
        this.edgesMsg = (CDictionaryMsg<string>) edges.GetLastSynchronizedUpdate();
    }

    public override void Decode(byte[] input)
    {
        var json = JsonSerializer.Deserialize<CanilleraGraphMsg>(input);
        this.verticesMsg = json.verticesMsg;
        this.edgesMsg = json.edgesMsg;
    }

    public override byte[] Encode()
    {
        return JsonSerializer.SerializeToUtf8Bytes(this);
    }
}


[ReplicatedType("CanilleraGraph")]
public class CanilleraGraph : CRDT
{
    public readonly struct Edge
    {
        public Guid src { get; }
        public Guid dst { get; }
        public Edge(Guid v1, Guid v2)
        {
            this.src = v1;
            this.dst = v2;
        }

        public Edge(string s)
        {
            var guids = s.Split('_');
            this.src = Guid.ParseExact(guids[0], "D");
            this.dst = Guid.ParseExact(guids[1], "D");
        }

        public override string ToString() => $"{this.src}_{this.dst}";
    }

    private readonly ORSet<Guid> _vertices;
    private readonly CDictionary<string> _edges;

    public CanilleraGraph()
    {
        this._vertices = new ORSet<Guid>();
        this._edges = new CDictionary<string>();
    }

    [OperationType(OpType.Update)]
    public virtual void AddVertex(Guid v)
    {
        _ = this._vertices.Add(v);
    }

    [OperationType(OpType.Update)]
    public virtual bool RemoveVertex(Guid v)
    {
        var activeEdges = this.EdgeCounts().Where(kv => kv.Value > 0);
        var srcVertices = activeEdges.Select(kv => kv.Key.src);
        var dstVertices = activeEdges.Select(kv => kv.Key.dst);

        // the vertex is in the set and the vertex does not support any active edges
        if (this._vertices.Contains(v) && !srcVertices.Contains(v) && !dstVertices.Contains(v))
        {
            return this._vertices.Remove(v);
        }
        return false;
    }

    [OperationType(OpType.Update)]
    public virtual bool AddEdge(Edge e)
    {
        var vertices = this.LookupVertices();

        if (!vertices.Contains(e.src) || !vertices.Contains(e.dst))
        {
            return false;
        }

        this._edges.Add(e.ToString());
        this.AddVertex(e.src);
        this.AddVertex(e.dst);
        return true;
    }
    [OperationType(OpType.Update)]
    public virtual bool AddEdge(Guid src, Guid dst) => this.AddEdge(new Edge(src, dst));

    [OperationType(OpType.Update)]
    public virtual bool RemoveEdge(Edge e)
    {
        var eStr = e.ToString();
        if (this._edges.ContainsKey(eStr) && this.EdgeCount(e) > 0)
        {
            return this._edges.Remove(eStr);
        }

        return false;
    }
    [OperationType(OpType.Update)]
    public virtual bool RemoveEdge(Guid src, Guid dst) => this.RemoveEdge(new Edge(src, dst));

    public IEnumerable<Guid> LookupVertices()
    {
        return this._vertices.LookupAll();
    }

    public Dictionary<Edge, int> EdgeCounts()
    {
        var result = new Dictionary<Edge, int>();
        foreach (KeyValuePair<string, int> kv in this._edges)
        {
            var edge = new Edge(kv.Key);
            result.Add(edge, Math.Max(kv.Value, 0));
        }

        return result;
    }

    public int EdgeCount(Edge edge)
    {
        var eStr = edge.ToString();
        if (this._edges.TryGetValue(eStr, out int numEdges))
        {
            return Math.Max(numEdges, 0);
        }

        return 0;
    }

    public override void ApplySynchronizedUpdate(PropagationMessage receivedUpdate)
    {
        if (receivedUpdate is not CanilleraGraphMsg)
        {
            throw new NotSupportedException($"{System.Reflection.MethodBase.GetCurrentMethod().Name} does not support {nameof(receivedUpdate)} type of {receivedUpdate.GetType()}");
        }

        CanilleraGraphMsg received = (CanilleraGraphMsg) receivedUpdate;
        this._edges.ApplySynchronizedUpdate(received.edgesMsg);
        this._vertices.ApplySynchronizedUpdate(received.verticesMsg);
    }

    public override PropagationMessage DecodePropagationMessage(byte[] input)
    {
        CanilleraGraphMsg msg = new();
        msg.Decode(input);
        return msg;
    }

    public override PropagationMessage GetLastSynchronizedUpdate() => new CanilleraGraphMsg(this._vertices, this._edges);
}
