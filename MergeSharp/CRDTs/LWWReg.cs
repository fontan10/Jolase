using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MergeSharp;

[TypeAntiEntropyProtocol(typeof(LWWRegister<>))]
public class LWWRegisterMsg<T> : PropagationMessage
{
    [JsonInclude]
    public T value { get; private set; }
    [JsonInclude]
    public DateTime timestamp { get; private set; }
    [JsonInclude]
    public Guid replicaIdx { get; private set; }

    public LWWRegisterMsg()
    {
    }

    public LWWRegisterMsg(T value, DateTime timestamp, Guid replicaIdx)
    {
        this.value = value;
        this.timestamp = timestamp;
        this.replicaIdx = replicaIdx;
    }

    public override void Decode(byte[] input)
    {
        var json = JsonSerializer.Deserialize<LWWRegisterMsg<T>>(input);
        this.value = json.value;
        this.timestamp = json.timestamp;
        this.replicaIdx = json.replicaIdx;

    }

    public override byte[] Encode()
    {
        return JsonSerializer.SerializeToUtf8Bytes(this);
    }
}

[ReplicatedType("LWWRegister")]
public class LWWRegister<T> : CRDT
{
    private DateTime _timestamp;
    private T _value;
    private readonly Guid _replicaIdx;
    public virtual T Value
    {
        get => this._value;
        [OperationType(OpType.Update)]
        set { this._value = value; this._timestamp = DateTime.UtcNow; }
    }

    public LWWRegister()
    {
        this.Value = default;
        this._replicaIdx = Guid.NewGuid();
    }

    public LWWRegister(T initVal)
    {
        this.Value = initVal;
        this._replicaIdx = Guid.NewGuid();
    }

    public override void ApplySynchronizedUpdate(PropagationMessage receivedUpdate)
    {
        if (receivedUpdate is not LWWRegisterMsg<T>)
        {
            throw new NotSupportedException($"{System.Reflection.MethodBase.GetCurrentMethod().Name} does not support {nameof(receivedUpdate)} type of {receivedUpdate.GetType()}");
        }

        LWWRegisterMsg<T> received = (LWWRegisterMsg<T>) receivedUpdate;

        if ((received.timestamp > this._timestamp) ||
            (received.timestamp == this._timestamp && received.replicaIdx.CompareTo(this._replicaIdx) > 0))
        {
            this._timestamp = received.timestamp;
            this._value = received.value;
        }
    }

    public override PropagationMessage DecodePropagationMessage(byte[] input)
    {
        LWWRegisterMsg<T> msg = new();
        msg.Decode(input);
        return msg;
    }

    public override PropagationMessage GetLastSynchronizedUpdate()
    {
        return new LWWRegisterMsg<T>(this._value, this._timestamp, this._replicaIdx);
    }
}
