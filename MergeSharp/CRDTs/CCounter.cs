using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MergeSharp;

[TypeAntiEntropyProtocol(typeof(CCounter))]
public class CCounterMsg : PropagationMessage
{
    [JsonInclude]
    public Dictionary<Guid, int> pVector;
    [JsonInclude]
    public Dictionary<Guid, int> nVector;


    public CCounterMsg()
    {
    }

    public CCounterMsg(Dictionary<Guid, int> pVector, Dictionary<Guid, int> nVector)
    {
        this.pVector = pVector;
        this.nVector = nVector;
    }


    public override void Decode(byte[] input)
    {
        var json = JsonSerializer.Deserialize<CCounterMsg>(input);
        this.pVector = json.pVector;
        this.nVector = json.nVector;
    }


    public override byte[] Encode()
    {
        return JsonSerializer.SerializeToUtf8Bytes(this);
    }
}


[ReplicatedType("CCounter")]
public class CCounter : CRDT
{
    private readonly Dictionary<Guid, int> _pVector;
    private readonly Dictionary<Guid, int> _nVector;

    private readonly Guid _replicaIdx;

    public CCounter()
    {
        this._replicaIdx = Guid.NewGuid();

        this._pVector = new Dictionary<Guid, int>();
        this._nVector = new Dictionary<Guid, int>();
        this._pVector[this._replicaIdx] = 0;
        this._nVector[this._replicaIdx] = 0;
    }


    public int Get()
    {
        return this._pVector.Sum(x => x.Value) - this._nVector.Sum(x => x.Value);
    }

    [OperationType(OpType.Update)]
    public virtual void Increment(int i)
    {
        if (i == 0)
        {
            return;
        }

        if (i < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(i));
        }

        this._pVector[this._replicaIdx] += i;

        int diff = this.Get();
        if (diff <= 0)
        {
            this._pVector[this._replicaIdx] += Math.Abs(diff) + 1;
        }
    }

    [OperationType(OpType.Update)]
    public virtual void Decrement(int i)
    {
        if (i < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(i));
        }

        this._nVector[this._replicaIdx] += i;
    }


    public override PropagationMessage GetLastSynchronizedUpdate()
    {
        return new CCounterMsg(this._pVector, this._nVector);
    }

    public override void ApplySynchronizedUpdate(PropagationMessage receivedUpdate)
    {
        if (receivedUpdate is not CCounterMsg)
        {
            throw new NotSupportedException($"{System.Reflection.MethodBase.GetCurrentMethod().Name} does not support receivedUpdate type of {receivedUpdate.GetType()}");
        }

        CCounterMsg received = (CCounterMsg) receivedUpdate;

        foreach (var kv in received.pVector)
        {
            _ = this._pVector.TryGetValue(kv.Key, out int value);
            this._pVector[kv.Key] = Math.Max(value, kv.Value);
        }

        foreach (var kv in received.nVector)
        {
            _ = this._nVector.TryGetValue(kv.Key, out int value);
            this._nVector[kv.Key] = Math.Max(value, kv.Value);
        }
    }

    public override PropagationMessage DecodePropagationMessage(byte[] input)
    {
        CCounterMsg msg = new();
        msg.Decode(input);
        return msg;
    }
}

