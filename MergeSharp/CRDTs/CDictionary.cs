using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MergeSharp;

[TypeAntiEntropyProtocol(typeof(CDictionary<>))]
public class CDictionaryMsg<TKey> : PropagationMessage
{
    [JsonInclude]
    public Dictionary<TKey, CCounterMsg> cCountMsgVector { get; private set; }

    public CDictionaryMsg() { }

    public CDictionaryMsg(Dictionary<TKey, CCounter> cCountVector)
    {
        this.cCountMsgVector = new();

        foreach (var kv in cCountVector)
        {
            this.cCountMsgVector.Add(kv.Key, (CCounterMsg) kv.Value.GetLastSynchronizedUpdate());
        }
    }

    public override void Decode(byte[] input)
    {
        var json = JsonSerializer.Deserialize<CDictionaryMsg<TKey>>(input);
        this.cCountMsgVector = json.cCountMsgVector;
    }

    public override byte[] Encode()
    {
        return JsonSerializer.SerializeToUtf8Bytes(this);
    }
}

[ReplicatedType("CDictionary")]
public class CDictionary<TKey> : CRDT, IDictionary<TKey, int>
{
    private readonly Dictionary<TKey, CCounter> _cCountVector;

    public ICollection<TKey> Keys => this.LookupKeys();
    public ICollection<int> Values => this.LookupValues();

    public int Count => this.Keys.Count;
    public bool IsReadOnly => false;

    public virtual int this[TKey key]
    {
        get
        {
            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }
            else if (this._cCountVector.TryGetValue(key, out CCounter counter))
            {
                return counter.Get();
            }
            else
            {
                throw new KeyNotFoundException();
            }
        }
        [OperationType(OpType.Update)]
        set
        {
            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }
            else if (this._cCountVector.TryGetValue(key, out CCounter counter))
            {
                var currVal = counter.Get();
                var diff = value - currVal;
                this.Add(key, diff);
            }
            else
            {
                throw new KeyNotFoundException();
            }
        }
    }

    public CDictionary()
    {
        this._cCountVector = new Dictionary<TKey, CCounter>();
    }


    [OperationType(OpType.Update)]
    public virtual void Add(TKey key, int value)
    {
        if (value < 0)
        {
            _ = this.Remove(key, Math.Abs(value));
        }
        else
        {
            if (this._cCountVector.TryGetValue(key, out CCounter counter))
            {
                counter.Increment(value);
            }
            else
            {
                counter = new();
                counter.Increment(value);
                this._cCountVector.Add(key, counter);
            }
        }
    }
    [OperationType(OpType.Update)]
    public virtual void Add(KeyValuePair<TKey, int> item) => this.Add(item.Key, item.Value);
    [OperationType(OpType.Update)]
    public virtual void Add(TKey key) => this.Add(key, 1);

    [OperationType(OpType.Update)]
    public virtual bool Remove(TKey key, int value)
    {
        if (value < 0)
        {
            this.Add(key, Math.Abs(value));
            return true;
        }
        else
        {
            if (this._cCountVector.TryGetValue(key, out CCounter counter))
            {
                counter.Decrement(value);
                return true;
            }
            else
            {
                counter = new();
                counter.Decrement(value);
                this._cCountVector.Add(key, counter);
                return true;
            }
        }
    }
    [OperationType(OpType.Update)]
    public virtual bool Remove(KeyValuePair<TKey, int> item) => this.Remove(item.Key, item.Value);
    [OperationType(OpType.Update)]
    public virtual bool Remove(TKey key) => this.Remove(key, 1);

    [OperationType(OpType.Update)]
    public virtual void Clear()
    {
        this._cCountVector.Clear();
    }


    private ICollection<TKey> LookupKeys()
    {
        return this._cCountVector.Keys;
    }

    private ICollection<int> LookupValues()
    {
        return this._cCountVector.Values.Select(counter => counter.Get()).ToList();
    }


    public override void ApplySynchronizedUpdate(PropagationMessage receivedUpdate)
    {
        if (receivedUpdate is not CDictionaryMsg<TKey>)
        {
            throw new NotSupportedException($"{System.Reflection.MethodBase.GetCurrentMethod().Name} does not support type of {receivedUpdate.GetType()}");
        }

        CDictionaryMsg<TKey> received = (CDictionaryMsg<TKey>) receivedUpdate;

        foreach (KeyValuePair<TKey, CCounterMsg> kv in received.cCountMsgVector)
        {
            if (this._cCountVector.TryGetValue(kv.Key, out CCounter counter))
            {
                counter.ApplySynchronizedUpdate(kv.Value);
            }
            else
            {
                counter = new();
                counter.ApplySynchronizedUpdate(kv.Value);
                this._cCountVector.Add(kv.Key, counter);
            }
        }
    }

    public override PropagationMessage DecodePropagationMessage(byte[] input)
    {
        CDictionaryMsg<TKey> msg = new();
        msg.Decode(input);
        return msg;
    }

    public override PropagationMessage GetLastSynchronizedUpdate()
    {
        return new CDictionaryMsg<TKey>(this._cCountVector);
    }


    public bool Contains(KeyValuePair<TKey, int> item) => this.TryGetValue(item.Key, out int val) && val == item.Value;

    public bool ContainsKey(TKey key) => this.Keys.Contains(key);

    public bool TryGetValue(TKey key, out int value)
    {
        if (this.ContainsKey(key))
        {
            value = this[key];
            return true;
        }

        value = 0;
        return false;
    }


    public void CopyTo(KeyValuePair<TKey, int>[] array, int arrayIndex) => throw new NotImplementedException();

    public IEnumerator<KeyValuePair<TKey, int>> GetEnumerator() => this.Keys.Zip(this.Values, (first, second) => new KeyValuePair<TKey, int>(first, second)).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
}
