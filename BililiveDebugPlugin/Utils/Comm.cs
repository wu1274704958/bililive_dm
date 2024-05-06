using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
[Serializable]
public class Wrap<T>
{
    public T val;

    public Wrap(T val)
    {
        this.val = val;
    }

    public Wrap()
    {
        this.val = default(T);
    }

    public override bool Equals(object obj)
    {
        if (obj is Wrap<T> oth)
            return val.Equals(oth.val);
        return false;
    }

    public override int GetHashCode()
    {
        return val.GetHashCode();
    }

    public override string ToString()
    {
        return $"({val})";
    }
}

[Serializable]
public class Pair<T, T2>
{
    public T first;
    public T2 second;

    public Pair(T first, T2 second)
    {
        this.first = first;
        this.second = second;
    }
}

public class VarCache<T> where T : class
{
    private T t = null;
    private Func<T> construct;

    public VarCache(Func<T> construct)
    {
        this.construct = construct;
    }

    public T val
    {
        get
        {
            if (t == null && construct != null)
                t = construct.Invoke();
            return t;
        }
    }
    public void clear()
    {
        t = null;
    }
    public void SetVal(T v)
    {
        t = v;
    }
}


public interface GetVal<T>
{
    T getVal();
}

public abstract class TickTag
{
    public virtual bool Eq(TickTag b)
    {
        if (this is TickTagDynGeneric ttdg)
            return ttdg.Equals(b);
        return this.GetType() == b.GetType();
    }

    public override string ToString()
    {
        return GetType().FullName;
    }
}
public class NoTag : TickTag { }

public class TickOp
{
    public static TickOp operator +(TickOp a, TickOp b)
    {
        if (a == null) return b;
        if (b == null) return a;
        if (a is MlutTickOp arr && b is MlutTickOp arr2)
        {
            var old = arr.ops;
            arr.ops = new TickOp[arr.ops.Length + arr2.ops.Length];
            old.CopyTo(arr.ops, 0);
            arr2.ops.CopyTo(arr.ops, old.Length);
        }
        else if (a is MlutTickOp arr3)
        {
            var old = arr3.ops;
            arr3.ops = new TickOp[old.Length + 1];
            old.CopyTo(arr3.ops, 0);
            arr3.ops[old.Length] = b;
        }
        else if (b is MlutTickOp arr4)
        {
            var old = arr4.ops;
            arr4.ops = new TickOp[old.Length + 1];
            old.CopyTo(arr4.ops, 0);
            arr4.ops[old.Length] = a;
            return b;
        }
        else
        {
            var arr5 = new TickOp[2];
            arr5[0] = a;
            arr5[1] = b;
            return new MlutTickOp() { ops = arr5 };
        }
        return a;
    }

    public static void Test()
    {
        TickOp a = null;
        var b = a + new TickRmOp();
        var c = b + new TickClearOp();
        var d = c + new TickRmSelfOp();
        var arr = new MlutTickOp() { ops = new TickOp[] { new DelayTaskTickOp(), new TickAddNextOp() } };
        var e = arr + d;
        var f = new TickReplaceOp() + e;
    }
}
public class TickRmSelfOp : TickOp { }
public class TickRmOp : TickOp
{
    public int key;
}
public class TickClearOp : TickOp { }
public class TickAddNextOp : TickOp
{
    public TickTick next;
}
public class TickReplaceOp : TickOp
{
    public TickTick next;
}
public class TickReplaceMlutOp : TickOp
{
    public TickTick[] ts;
}
public class MlutTickOp : TickOp
{
    public TickOp[] ops;
}
public class DelayTaskTickOp : TickOp
{
    public Func<TickOp> action;
    public TickTag tickTag;
}
public class ClearTagTickOp : TickOp
{
    public TickTag tickTag;
}


public abstract class TickTick
{
    protected DateTime lastTime;
    protected TickTag tag = null;
    public bool IsPause = false;
    public TickTick()
    {
        lastTime = DateTime.Now;
    }
    public abstract TickOp update(TimeSpan t);
    public void update()
    {
        var now = DateTime.Now;
        if(!IsPause)update(now - lastTime);
        lastTime = now;
    }
    public virtual void OnAdd(TickGroup tg)
    {

    }

    public virtual void OnRemove()
    {

    }
    public virtual void Reset()
    {
        lastTime = DateTime.Now;
    }
    public TickTick SetTag<T>()
        where T : TickTag, new()
    {
        tag = new T();
        return this;
    }
    public TickTick SetTag(TickTag tag)
    {
        this.tag = tag;
        return this;
    }
    public bool IsTag<T>()
        where T : TickTag
    {
        return tag != null && tag is T;
    }
    public bool IsTag(TickTag tag)
    {
        return this.tag != null &&  this.tag.Eq(tag);
    }
    public TickTag GetTag()
    {
        return tag;
    }
    public virtual void Pause(bool b) { }
}


public class ExecOpTick : TickTick
{
    public TickOp op;
    public override TickOp update(TimeSpan t)
    {
        var res = new List<TickOp>();
        if (op != null)
            res.Add(op);
        res.Add(new TickRmSelfOp());
        return new MlutTickOp() { ops = res.ToArray() };
    }
}

public abstract class ProducableTick : TickTick
{
    public TickTick Next { get; protected set; }
    public List<TickTick> NextArr { get; protected set; }
    public ProducableTick SetNext(TickTick n)
    {
        if (n == null) return this;
        if (NextArr != null && NextArr.Count > 0)
            NextArr[0] = n;
        else
            Next = n;
        return this;
    }
    public ProducableTick AddNext(TickTick n)
    {
        if (n == null) return this;
        if (NextArr == null)
            NextArr = new List<TickTick>();
        if (Next != null)
        {
            NextArr.Add(Next);
            Next = null;
        }
        NextArr.Add(n);
        return this;
    }
    public bool HasNext => Next != null || (NextArr != null && NextArr.Count > 0);

    public TickOp NextOnRmSelf
    {
        get
        {
            TickOp res = new TickRmSelfOp();
            if (HasNext)
            { 
                if (Next != null)
                    res = new TickReplaceOp() { next = Next };
                else if (NextArr != null)
                    res = new TickReplaceMlutOp() { ts = NextArr.ToArray() };
            }
            return res;
        }
    }
}
public class TaskReproducible : DelayTask
{
    public Action _OnAdd, _OnRemove;
    public TickTick next;
    public TaskReproducible(Action action, TimeSpan time, int TriggerCount, TickTick next = null) : base(action, time, TriggerCount)
    {
        this.next = next;
    }
    public TaskReproducible(Func<TickOp> action, TimeSpan time, int TriggerCount, TickTick next = null) : base(action, time, TriggerCount)
    {
        this.next = next;
    }

    protected override TickOp OnTrigger()
    {
        var op = base.OnTrigger() as MlutTickOp;
        if (TriggerCount <= 0)
        {
            if (next != null)
                return new MlutTickOp() { ops = new TickOp[]{ op, new TickReplaceOp() { next = next } } };
            else
                return new MlutTickOp() { ops = new TickOp[] { op, new TickRmSelfOp() } };  
        }
        return op;
    }

    public TaskReproducible SetNext(TickTick n)
    {
        next = n;
        return this;
    }
    public override void OnRemove()
    {
        base.OnRemove();
        _OnRemove?.Invoke();
    }
    public override void OnAdd(TickGroup tg)
    {
        base.OnAdd(tg);
        _OnAdd?.Invoke();
    }
    public TaskReproducible SetOnRemove(Action n)
    {
        _OnRemove = n;
        return this;
    }
    public TaskReproducible SetOnAdd(Action n)
    {
        _OnAdd = n;
        return this;
    }
}

public class DelayLoopTask : TickTick
{
    protected Action action;
    protected Func<TickOp> actionWithOp;
    public Action<TimeSpan> UpdateAction; 
    protected TimeSpan time = TimeSpan.Zero, curr = TimeSpan.Zero;

    public DelayLoopTask(Action action, TimeSpan time = default)
    {
        this.action = action;
        this.time = time;
    }
    public DelayLoopTask(Func<TickOp> action, TimeSpan time = default)
    {
        this.actionWithOp = action;
        this.time = time;
    }

    public override TickOp update(TimeSpan ms)
    {
        UpdateAction?.Invoke(ms);
        curr += ms;
        if (curr >= time)
        {
            return OnTrigger();
        }
        return null;
    }

    protected virtual TickOp OnTrigger()
    {
        TickOp res = null;
        if (actionWithOp != null)
            res = actionWithOp.Invoke();
        else
           action?.Invoke();
        curr -= time;
        return res;
    }

    public TimeSpan Time
    {
        set
        {
            time = value;
            curr = TimeSpan.Zero;
        }
        get => time;
    }
}
public class DelayTaskTag : TickTag { }
public class DelayTask : ProducableTick
{
    protected Func<TickOp> actionWithOp;
    private Action m_Action;
    private Action<string> m_ActionWithStr;
    private TimeSpan m_DelayTime = TimeSpan.Zero;
    private TimeSpan m_Curr = TimeSpan.Zero;
    protected int TriggerCount = 0;
    public bool DelayRunAction = false;
    private string m_Para;
    public DelayTask(Action action, TimeSpan delay, int triggerCount = 1)
    {
        tag = new DelayTaskTag();
        m_Action = action;
        m_DelayTime = delay;
        this.TriggerCount = triggerCount;
    }
    public DelayTask(Action<string> action,string para, TimeSpan delay, int triggerCount = 1)
    {
        m_Para = para;
        tag = new DelayTaskTag();
        m_ActionWithStr = action;
        m_DelayTime = delay;
        this.TriggerCount = triggerCount;
    }
    public DelayTask(Func<TickOp> action, TimeSpan time, int triggerCount = 1)
    {
        tag = new DelayTaskTag();
        actionWithOp = action;
        m_DelayTime = time;
        TriggerCount = triggerCount;
    }

    public override TickOp update(TimeSpan ms)
    {
        m_Curr += ms;
        if (m_Curr >= m_DelayTime)
        {
            return OnTrigger();
        }
        return null;
    }

    public DelayTask SetDelayRunAction(bool v)
    {
        DelayRunAction = v;
        return this;
    }

    protected virtual TickOp OnTrigger()
    {
        TickOp res = null;
        if (DelayRunAction)
            res += new DelayTaskTickOp() { action = RunAction, tickTag = tag };
        else
        {
            res += RunAction();
        }
        m_Curr = TimeSpan.Zero;
        --TriggerCount;
        if (TriggerCount <= 0)
            res += NextOnRmSelf;
        return res;
    }

    private TickOp RunAction()
    {
        if (actionWithOp != null)
            return actionWithOp.Invoke();
        if (m_ActionWithStr != null)
        {
            m_ActionWithStr.Invoke(m_Para);
            return null;
        }
        m_Action?.Invoke();
        return null;
    }

    public void ForceTrigger()
    {
        m_Curr = m_DelayTime;
    }
}

public class DelayFrameTaskTag : TickTag { }
public class DelayFrameTask : TickTick
{
    protected Func<TickOp> actionWithOp;
    protected Action action;
    protected int time = 0, curr = 0;
    protected int triggerCount = 0;
    public bool DelayRunAction = false;
    public DelayFrameTask(Action action, int frame, int TriggerCount)
    {
        this.tag = new DelayFrameTaskTag();
        this.action = action;
        this.time = frame;
        this.triggerCount = TriggerCount;
    }
    public DelayFrameTask(Func<TickOp> action, int frame, int TriggerCount)
    {
        this.tag = new DelayFrameTaskTag();
        this.actionWithOp = action;
        this.time = frame;
        this.triggerCount = TriggerCount;
    }

    public override TickOp update(TimeSpan ms)
    {
        curr += 1;
        if (curr >= time)
        {
            return OnTrigger();
        }
        return null;
    }

    public DelayFrameTask SetDelayRunAction(bool v)
    {
        DelayRunAction = v;
        return this;
    }

    protected virtual TickOp OnTrigger()
    {
        List<TickOp> ops = new List<TickOp>();
        if (DelayRunAction)
            ops.Add(new DelayTaskTickOp() { action = RunAction, tickTag = tag });
        else
        {
            var op = RunAction();
            if (op != null) ops.Add(op);
        }
        curr = 0;
        --triggerCount;
        if (triggerCount <= 0) ops.Add(new TickRmSelfOp());
        return new MlutTickOp() { ops = ops.ToArray() };
    }

    private TickOp RunAction()
    {
        if (actionWithOp != null)
            return actionWithOp.Invoke();
        action?.Invoke();
        return null;
    }

    public void ForceTrigger()
    {
        curr = time;
    }
}

public class UselessTask : DelayTask
{
    public UselessTask(TimeSpan time) : base(null, time, 1)
    {

    }
    public UselessTask() : base(null, TimeSpan.FromSeconds(2), 1)
    {

    }
}

public class DelayLoopTaskdouble : DelayLoopTask
{
    protected bool FirstLevelTrigger = false;
    protected int SecondLevelTriggerTimes = 1;
    protected TimeSpan SecondLevelTriggerDur = TimeSpan.FromSeconds(1);
    public DelayLoopTaskdouble(Action action, TimeSpan time,
        int SecondLevelTriggerTimes,
        TimeSpan SecondLevelTriggerDur) : base(action, time)
    {
        this.SecondLevelTriggerDur = SecondLevelTriggerDur;
        this.SecondLevelTriggerTimes = SecondLevelTriggerTimes;
    }

    public override TickOp update(TimeSpan ms)
    {
        return base.update(ms);
    }

    protected override TickOp OnTrigger()
    {
        base.OnTrigger();
        FirstLevelTrigger = true;
        return new TickAddNextOp()
        {
            next = new DelayTask(() =>
            {
                action?.Invoke();
                FirstLevelTrigger = false;
                return null;
            }, SecondLevelTriggerDur, SecondLevelTriggerTimes)
        };
    }
}

public class DelayAssignment<T> : TickTick, GetVal<T>
{
    protected TimeSpan time = TimeSpan.Zero;
    protected bool isDelaySet = false;
    protected T t, delayVal;

    public T getVal()
    {
        return t;
    }

    public DelayAssignment(T t)
    {
        this.t = t;
    }

    public DelayAssignment()
    {
        this.t = default(T);
    }

    public void delaySet(T v, TimeSpan time)
    {
        delayVal = v;
        this.time = time;
        isDelaySet = true;
    }

    public void clearDelaySet()
    {
        delayVal = default(T);
        this.time = TimeSpan.Zero;
        isDelaySet = false;
    }

    public void immediatelySet(T t)
    {
        this.t = t;
        clearDelaySet();
    }
    public void immediatelySet()
    {
        this.t = delayVal;
        clearDelaySet();
    }

    public override TickOp update(TimeSpan ms)
    {
        if (!isDelaySet) return null;
        time -= ms;
        if (time < TimeSpan.Zero)
        {
            t = delayVal;
            clearDelaySet();
        }
        return null;
    }
}
public class AddTickTag : TickTagDynGeneric
{
    public AddTickTag(TickTag t) : base(t)
    { }
}
public class RemoveTickTag<T> : TickTag where T : TickTag { }
public class ClearAllTickTag : TickTag { }
public class TickGroup : TickTick
{
    protected ConcurrentDictionary<int, TickTick> tk;
    public bool Updating { get; private set; } = false;
    protected List<(Func<TickOp>, TickTag)> DelayActions = new List<(Func<TickOp>, TickTag)>();
    private List<int> m_TmpDel = new List<int>();
    private List<TickTick> m_TmpAdd = new List<TickTick>();
    public int Count => tk.Count;
    public int TaskCount => DelayActions.Count;
    public float Speed = 1.0f;
    public string GetTaskStr()
    {
        var res = new StringBuilder();
        foreach (var t in tk)
        {
            res.Append($"ID:{t.Key} Type:{t.Value.GetType().Name} Tag:{t.Value.GetTag()?.GetType()?.Name}\n");
        }
        return res.ToString();
    }
    public TickGroup(IEnumerable<TickTick> it)
    {
        tk = new ConcurrentDictionary<int, TickTick>();
        foreach (var i in it)
        {
            tk.TryAdd(i.GetHashCode(), i);
        }
    }

    public TickGroup()
    {
        tk = new ConcurrentDictionary<int, TickTick>();
    }

    public override TickOp update(TimeSpan ms)
    {
        Updating = true;
        var doClear = false;
        m_TmpAdd.Clear();
        m_TmpDel.Clear();
        var dur = TimeSpan.FromMilliseconds(ms.TotalMilliseconds * Speed);
        TickTick tick = null;
        try
        {
            foreach (var it in tk)
            {
                tick = it.Value;
                var op = it.Value.update(dur);
                if (op == null) continue;
                ExecOp(it, op, ref m_TmpDel, ref m_TmpAdd, ref doClear);
            }
        }
        catch (Exception e)
        {
            var tags = tick.GetTag() == null ? "null" : tick.GetTag().ToString();
            TmpLog.log("ExecOp failed {0} tag = {1} e = {2} Stack = {3}", "TG", tick.GetType(), tags, e,e.StackTrace);
        }
        foreach (var it in m_TmpDel)
            Rmtt(it);
        foreach (var it in m_TmpAdd)
            Addtt(it);
        RunDelayAction(ref m_TmpDel, ref m_TmpAdd, ref doClear);
        if (doClear)
            ClearEx();
        Updating = false;
        return null;
    }
    protected void ExecOp(KeyValuePair<int, TickTick> it, TickOp op, ref List<int> tmpDel, ref List<TickTick> tmpAdd, ref bool doClear)
    {
        if (op == null) return;
        switch (op)
        {
            case TickRmSelfOp rmSelf:
                {
                    if (it.Value != null)
                        tmpDel.Add(it.Key);
                }
                break;
            case TickRmOp rm:
                {
                    tmpDel.Add(rm.key);
                }
                break;
            case TickClearOp clear:
                {
                    doClear = true;
                }
                break;
            case TickAddNextOp add:
                {
                    tmpAdd.Add(add.next);
                }
                break;
            case TickReplaceOp replace:
                {
                    if (it.Value != null)
                        tmpDel.Add(it.Key);
                    tmpAdd.Add(replace.next);
                }
                break;
            case TickReplaceMlutOp replaceMult:
                {
                    if (it.Value != null)
                        tmpDel.Add(it.Key);
                    tmpAdd.AddRange(replaceMult.ts);
                } 
                break;
            case MlutTickOp mlut:
                {
                    foreach (var o in mlut.ops)
                        ExecOp(it, o, ref tmpDel, ref tmpAdd, ref doClear);
                }
                break;
            case DelayTaskTickOp delayTask:
                {
                    addDelayAction(delayTask.action, delayTask.tickTag);
                }
                break;
            case ClearTagTickOp clearTag:
                {
                    ClearByTag(clearTag.tickTag,false);
                }
                break;
        }
    }
    protected void Addtt(TickTick t)
    {
        if (t == null) return;
        if(tk.TryAdd(t.GetHashCode(), t))
           t.OnAdd(this);
        else
            TmpLog.log("TickGroup 添加重复的tick");           
    }
    protected void Rmtt(int k)
    {
        if (tk.TryRemove(k, out var tt))
        {
            tt.OnRemove();
        }
    }
    private void RunDelayAction(ref List<int> tmpDel, ref List<TickTick> tmpAdd, ref bool doClear)
    {
        tmpDel.Clear(); tmpAdd.Clear();
        lock (DelayActions)
        {
            for (var i = 0; i < DelayActions.Count; ++i)
            {
                try
                {
                    var op = DelayActions[i].Item1?.Invoke();
                    if (op != null)
                        ExecOp(new KeyValuePair<int, TickTick>(-1, null), op, ref tmpDel, ref tmpAdd, ref doClear);
                }
                catch (Exception e)
                {
                    TmpLog.log("TickGroup DelayActions Err {0}", e.ToString());
                }
            }
            DelayActions.Clear();
        }

        foreach (var it in tmpDel)
            Rmtt(it);
        foreach (var it in tmpAdd)
            Addtt(it);
    }
    public bool Contain(TickTick t)
    {
        return tk.ContainsKey(t.GetHashCode());
    }
    public void Add(TickTick t)
    {
        var tag = t?.GetTag();
        if (tag == null)
        {
            tag = new AddTickTag(new NoTag());
        }
        else
        {
            tag = new AddTickTag(tag);
        }

        lock (DelayActions)
        {
            DelayActions.Add((() => { return new TickAddNextOp() { next = t }; }
                , tag));
        }
    }

    public void AddImmediate(TickTick t)
    {
        if (!Contain(t))
        {
            if(tk.TryAdd(t.GetHashCode(), t))
                t.OnAdd(this);
        }
    }
    public void RemoveImmediate(TickTick t)
    {
        if(tk.TryRemove(t.GetHashCode(),out _))
        {
            t.OnRemove();
        }
    }
    protected void RemoveImmediate(int key)
    {
        if (tk.TryRemove(key, out var t))
        {
            t.OnRemove();
        }
    }
    public void RemoveImmediate(Func<TickTick,bool> selector)
    {
        foreach (var itTick in tk)
        {
            if (selector(itTick.Value))
            {
                m_TmpDel.Add(itTick.Key);
            }
        }

        foreach (var key in m_TmpDel)
        {
            RemoveImmediate(key);
        }
        m_TmpDel.Clear();
    }

    public void Remove(TickTick t)
    {
        var tag = t.GetTag();
        if (tag == null)
        {
            tag = new RemoveTickTag<NoTag>();
        }
        else
        {
            var T = tag.GetType();
            var ty = typeof(RemoveTickTag<>);
            ty = ty.MakeGenericType(T);
            tag = (TickTag)System.Activator.CreateInstance(ty);
        }

        lock (DelayActions)
        {
            DelayActions.Add((() => { return new TickRmOp() { key = t.GetHashCode() }; }
                , tag));
        }
    }

    public void Clear()
    {
        lock (DelayActions)
        {
            DelayActions.Add((() => { return new TickClearOp(); }
                , new ClearAllTickTag()));
        }
    }

    public void ClearEx()
    {
        foreach (var it in tk)
        {
            it.Value.OnRemove();
        }
        tk.Clear();
        lock (DelayActions)
        {
            DelayActions.Clear();
        }
    }

    public void ClearTick()
    {
        foreach (var it in tk)
        {
            it.Value.OnRemove();
        }
        tk.Clear();
    }

    public bool IsClean()
    {
        return tk.Count == 0 && DelayActions.Count == 0;
    }

    public bool IsEmpty()
    {
        return tk.Count == 0;
    }

    public void Foreach(Action<TickTick, int> on)
    {
        int i = 0;
        foreach (var a in tk)
        {
            on?.Invoke(a.Value, i++);
        }
    }

    public void addDelayAction(Func<TickOp> action, TickTag tag = null)
    {
        if (action != null)
        {
            lock (DelayActions)
            {
                DelayActions.Add((action, tag));
            }
        }
    }

    public void ClearImmediateByFunc(Func<TickTag, bool> f, bool incluedeDelayAction = true)
    {
        if (f == null) return;
        var rmLs = new List<int>();
        foreach (var it in tk)
        {
            if (f(it.Value.GetTag()))
            {
                rmLs.Add(it.Key);
                try
                {
                    it.Value.OnRemove();
                }
                catch (Exception e)
                {
                    var tag = it.Value.GetTag() == null ? "null" : it.Value.GetTag().GetType().FullName;
                    TmpLog.log("ClearImmediateByFunc exec {0} tag:{1} OnRemove failed {2}","TG",it.Value.GetType().FullName,tag,e);
                }
            }
        }
        foreach (var i in rmLs)
        {
            tk.TryRemove(i,out _);
        }
        if (incluedeDelayAction)
        {
            lock (DelayActions)
            {
                for (int i = DelayActions.Count - 1; i >= 0; --i)
                {
                    var it = DelayActions[i];
                    if (f(it.Item2))
                    {
                        DelayActions.RemoveAt(i);
                    }
                }
            }
        }
    }

    public void ClearImmediateByTag<T>(bool incluedeDelayAction = true)
        where T : TickTag
    {
        ClearImmediateByFunc((t) =>
        {
            return t is T;
        }, incluedeDelayAction);
    }

    public void ClearImmediateByTag(TickTag T, bool incluedeDelayAction = true)
    {
        ClearImmediateByFunc((t) =>
        {
            return t != null && t.Eq(T);//GetType() == T.GetType();
        }, incluedeDelayAction);
    }

    public void ClearImmediateByTags(HashSet<Type> types, bool incluedeDelayAction = true)
    {
        ClearImmediateByFunc((t) =>
        {
            return types.Contains(t.GetType());
        }, incluedeDelayAction);
    }
    public void ClearImmediateByTags(List<TickTag> tags, bool incluedeDelayAction = true)
    {
        ClearImmediateByFunc((t) =>
        {
            for (int i = 0; i < tags.Count; ++i)
            {
                return tags[i].Eq(t);
            }
            return false;
        }, incluedeDelayAction);
    }

    public void ClearByTag<T>(bool incluedeDelayAction = true)
        where T : TickTag
    {
        addDelayAction(() =>
        {
            ClearImmediateByTag<T>(incluedeDelayAction);
            return null;
        });
    }

    public void ClearByTag(TickTag tag, bool incluedeDelayAction = true)
    {
        addDelayAction(() =>
        {
            ClearImmediateByTag(tag, incluedeDelayAction);
            return null;
        });
    }

    public bool HasTag<T>()
        where T : TickTag
    {
        foreach (var it in tk)
        {
            if (it.Value.IsTag<T>())
            {
                return true;
            }
        }
        return false;
    }

    public bool HasTag(TickTag tag)
    {
        foreach (var it in tk)
        {
            if (it.Value.IsTag(tag))
            {
                return true;
            }
        }
        return false;
    }
    public bool HasTags(params Type[] tags)
    {
        foreach (var it in tk)
        {
            if (it.Value.GetTag() != null && tags.Contains(it.Value.GetTag().GetType()))
            {
                return true;
            }
        }
        return false;
    }
    public void CloseUIAndClearAll()
    {
        if (!Updating)
        {
            addDelayAction(() => new TickClearOp());
            try {update();}catch(Exception e){}
        }
    }
    public override void Pause(bool b)
    {
        IsPause = b;
    }
}
public static class TmpLog
{
    public enum LogType
    {
        Info = 0,
        Warn = 1,
        Error = 2,
    }

    private static LogType s_CurrentLogLevel = LogType.Error;
    public static void SetLogLevel(LogType logType)
    {
        s_CurrentLogLevel = logType;
    }
    public static void logw(string fmt, string tag = "", params object[] args)
    {
    }
    public static void log(string fmt, string tag = "", params object[] args)
    {
    }
    public static void logi(string fmt, string tag = "", params object[] args)
    {
    }
    public static void log(LogType t, string fmt, string tag, params object[] args)
    {
        if (t < s_CurrentLogLevel) return;
        switch (t)
        {
            case LogType.Info:
                break;
            case LogType.Warn:
                break;
            case LogType.Error:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(t), t, null);
        }
    }
}

public static class Serialize
{
    public static T Deserialize<T>(string s)
    {
        var o = Deserialize(s, typeof(T));
        if (o == null) return default(T);
        return (T)o;
    }
    public static object Deserialize(string s, System.Type t)
    {
        if (t == typeof(string))
        {
            return s;
        }
        if (t == typeof(int))
        {
            if (int.TryParse(s, out var v))
                return v;
        }
        if (t == typeof(char))
        {
            if (char.TryParse(s, out var v))
                return v;
        }
        if (t == typeof(long))
        {
            if (long.TryParse(s, out var v))
                return v;
        }
        if (t == typeof(float))
        {
            if (float.TryParse(s, out var v))
                return v;
        }
        if (t == typeof(bool))
        {
            if (bool.TryParse(s, out var v))
                return v;
        }
        if (t == typeof(double))
        {
            if (double.TryParse(s, out var v))
                return v;
        }
        if (t == typeof(AnyArray))
        {
            try
            {
                AnyArray array = new AnyArray(s);
                return array;
            }
            catch //(Exception e)
            { }
        }
        return null;
    }

}

public static class AnyArrayTyMap
{
    public static readonly Dictionary<string, System.Type> TypeMap = new Dictionary<string, System.Type>{
        
       {"b",typeof(bool)},
       {"i",typeof(int)},
       {"l",typeof(long)},
       {"f",typeof(float)},
       {"lf",typeof(double)},
       {"s",typeof(string)},
       {"ch",typeof(char)},
    };
}

public class AnyArray
{
    protected List<object> objs;
    protected List<System.Type> tyArr;
    public int Count => objs.Count;
    protected AnyArray() { }

    public object this[int i]
    {
        get
        {
            if (good_idx(i))
                return objs[i];
            return null;
        }
        set
        {
            if (good_idx(i))
            {
                objs[i] = value;
                tyArr[i] = value.GetType();
            }
        }
    }

    public AnyArray(string str)
    {
        if (str.Length == 0)
        {
            this.objs = new List<object>();
            this.tyArr = new List<Type>();
        }
        else
        if (parse(str, out var objs, out var tys))
        {
            this.objs = objs;
            this.tyArr = tys;
        }
        else
        {
            throw new Exception("Parse AnyArray Failed!!!");
        }
    }

    public AnyArray(object[] a)
    {
        objs = new List<object>();
        tyArr = new List<Type>();
        foreach (var t in a)
        {
            if(t == null) continue;
            tyArr.Add(t.GetType());
            objs.Add(t);
        }
    }

    public static bool TryParse(string str,out AnyArray arr)
    {
        arr = null;
        try
        {
            arr = new AnyArray(str);
            return true;
        }
        catch (Exception e)
        {
            TmpLog.log($"Parse Any Array failed src = {str}");
            return false;
        }
    }

    public bool good_idx(int idx)
    {
        return idx >= 0 && idx < objs.Count;
    }
    public bool TypeEq<T>(int idx)
    {
        if (good_idx(idx))
        {
            var inTy = typeof(T);
            if (inTy == tyArr[idx])
                return true;
            else
            {
                var currTy = tyArr[idx]; 
                while (currTy.BaseType != null)
                {
                    if (currTy.BaseType == inTy)
                        return true;
                    currTy = currTy.BaseType;
                }
                return false;
            }
        }
        return false;
    }

    public T Get<T>(int idx)
    {
        if (TypeEq<T>(idx))
        {
            return (T)objs[idx];
        }
        return default(T);
    }

    public bool TryGet<T>(int idx, out T v, T def = default)
    {
        v = def;
        if (TypeEq<T>(idx))
        {
            v = (T)objs[idx];
            return true;
        }
        return false;
    }

    public object[] GetValues()
    {
        return objs.ToArray();
    }
    public List<object> GetValuesInside()
    {
        return objs;
    }

    public bool parse(string str, out List<object> objs, out List<System.Type> tys)
    {
        var cs = str.Trim().ToCharArray();
        int step = 0;
        System.Type currTy = null;
        objs = new List<object>();
        tys = new List<Type>();
        for (int i = 0; i < cs.Length; ++i)
        {
            var it = cs[i];
            if (step == 0 && it == '[')
            {
                ++step; continue;
            }
            if (step == 1 && it == ']')
            {
                if (i + 1 == cs.Length)
                {
                    step = 100; break;
                }
            }
            if (step == 1)
            {
                if (GetType(cs, i, out var ty, out var new_it))
                {
                    i = new_it; currTy = ty; step += 1;
                    continue;
                }
                else return false;
            }
            if (step == 2)
            {
                if (GetVal(cs, i, currTy, out var obj, out var nit))
                {
                    i = nit; step = 1;
                    objs.Add(obj);
                    tys.Add(currTy);
                    if (cs[i] == ']')
                    {
                        step = 100;
                        break;
                    }
                    else continue;
                }
                else return false;
            }
        }
        return step == 100;
    }

    private bool GetType(char[] arr, int it, out System.Type ty, out int new_it)
    {
        ty = null; new_it = it;
        var step = 0;
        StringBuilder sb = new StringBuilder();
        for (int i = it; i < arr.Length; ++i)
        {
            var c = arr[i];
            if (step == 0)
            {
                if (c != ' ')
                {
                    sb.Append(c);
                    step += 1;
                }
                continue;
            }
            if (step == 1)
            {
                if (c != ':')
                    sb.Append(c);
                else
                {
                    step += 1;
                    new_it = i;
                    ty = GetTypeByTag(sb.ToString().TrimEnd());
                    break;
                }
            }
        }
        return step == 2 && ty != null;
    }

    private bool GetVal(char[] arr, int it, System.Type ty, out object val, out int new_it)
    {
        val = null; new_it = it;
        var step = 0;
        bool isEscapeCharacter = false;
        StringBuilder sb = new StringBuilder();
        for (int i = it; i < arr.Length; ++i)
        {
            var c = arr[i];
            if (c == '\\')
            {
                isEscapeCharacter = true;
                continue;
            }
            if (step == 0)
            {
                if (isEscapeCharacter)
                {
                    sb.Append(c);
                    step += 1;
                    isEscapeCharacter = false;
                    continue;
                }
                if (c != ' ')
                {
                    sb.Append(c);
                    step += 1;
                }
                continue;
            }
            if (step == 1)
            {
                if (isEscapeCharacter)
                {
                    sb.Append(c);
                    isEscapeCharacter = false;
                    continue;
                }
                if (c != ';' && c != ']')
                    sb.Append(c);
                else
                {
                    step += 1;
                    new_it = i;
                    val = Serialize.Deserialize(sb.ToString().TrimEnd(), ty);
                    break;
                }
            }
        }
        return step == 2 && val != null;
    }

    private System.Type GetTypeByTag(string res)
    {
        if (AnyArrayTyMap.TypeMap.TryGetValue(res, out var s))
        {
            return s;
        }
        return null;
    }
    
    public bool ToDict<K,V>(int begin,int end,out Dictionary<K, V> dict,out int step)
    {
        dict = null;step = 0;
        if ((end - begin) % 2 != 0) 
            return false;
        dict = new Dictionary<K, V>();
        for (int i = begin; i < end; i += 2)
        {
            if(typeof(K) == tyArr[i] && typeof(V) == tyArr[i + 1])
            {
                dict.Add((K)objs[i],(V)objs[i + 1]);
            }
            else
            {
                return false;
            }
        }
        step = end - begin;
        return true;
    }

    public object[] GetRange(int start)
    {
        if (start >= Count) return null;
        return objs.GetRange(start, objs.Count - start).ToArray();
    }
    public object[] GetRangeByEOF(int start,out int step)
    {
        step = 0;
        if (start >= Count) return null;
        for (int i = start; i < Count; ++i)
        {
            ++step;
            if (tyArr[i] == typeof(char) && objs[i] is char ch && ch == '0')
            {
                if (i - start <= 0) return null;
                return objs.GetRange(start, i - start).ToArray();
            }
        }
        step = 0;
        return null;
    }
    public object[] GetRange(int start, int count)
    {
        if (start >= Count) return null;
        return objs.GetRange(start, count).ToArray();
    }
    public AnyArray GetRangeArr(int start)
    {
        if (start >= Count) return null;
        return new AnyArray
        {
            objs = objs.GetRange(start, objs.Count - start),
            tyArr = tyArr.GetRange(start, objs.Count - start),
        };
    }
    public AnyArray GetRangeArr(int start, int count)
    {
        if (start >= Count) return null;
        return new AnyArray
        {
            objs = objs.GetRange(start, count),
            tyArr = tyArr.GetRange(start, count),
        };
    }
    public void Add<T>(T v)
    {
        if(v == null) return;
        objs.Add(v);
        tyArr.Add(v.GetType());
    }
    public void PopBack()
    {
        if(Count <= 0) return;
        objs.RemoveAt(Count - 1);
        tyArr.RemoveAt(Count - 1);
    }
    public bool AllIs<T>()
    {
        if (tyArr == null || tyArr.Count == 0) return false;
        for (int i = 0; i < tyArr.Count; ++i)
        {
            if (!TypeEq<T>(i))
                return false;
        }
        return true;
    }
    public List<T> GetAllAndIs<T>()
    {
        if (tyArr == null || tyArr.Count == 0) return null;
        List<T> arr = new List<T>();
        for (int i = 0; i < objs.Count; ++i)
        {
            if (objs[i] is T t)
                arr.Add(t);
            else
                return null;
        }
        return arr;
    }
}

public interface SendMessage<T>
{
    void Send<M>(M msg, params object[] args)
        where M : T;
}

public interface CanSetOnChangeAction
{
    void SetOnChangeAction(Action on);
}

[Serializable]
public abstract class IndexableData<I, T> : CanSetOnChangeAction
    where I : struct
    where T : new()
{
    #if UNITY_EDITOR
    public Dictionary<I,string> UserDataEditorWindowTmpStr;
    #endif
    protected Dictionary<I, T> map = new Dictionary<I, T>();
    [NonSerialized]
    protected Action OnChangeAction;

    private bool m_NeedSave;

    public T this[I i]
    {
        get
        {
            if (!map.ContainsKey(i))
                Add(i, new T());
            else
                MaybeNeedSave();
            return map[i];
        }
        set
        {
            if (!Set(i, value))
                Add(i, value);
        }
    }
    //public T Find(I id)
    //{
    //    if (map.TryGetValue(id, out var v))
    //        return v;
    //    return default(T);
    //}
    public bool Add(I id, T t)
    {
        if (map.ContainsKey(id))
            return false;
        map.Add(id, t);
        OnAdd(t);
        OnChanged();
        return true;
    }

    private void OnAdd(T t)
    {
        if (t is CanSetOnChangeAction cs)
        {
            cs.SetOnChangeAction(OnChangeAction);
        }
    }

    public bool Remove(I id)
    {
        var f = map.Remove(id);
#if UNITY_EDITOR
        UserDataEditorWindowTmpStr?.Remove(id);
#endif
        if (f)
            OnChanged();
        return f;
    }
    public bool RemoveOther(List<I> exclude)
    {
        var f = map.RemoveOther(exclude);
#if UNITY_EDITOR
        UserDataEditorWindowTmpStr?.RemoveOther(exclude);
#endif
        if (f)
            OnChanged();
        return f;
    }
    public bool Contain(I id)
    {
        return map.ContainsKey(id);
    }
    public Pair<I, T> GetFirst()
    {
        foreach (var v in map)
            return new Pair<I, T>(v.Key, v.Value);
        return null;
    }

    public void Foreach(Action<KeyValuePair<I, T>> on)
    {
        foreach (var it in map)
        {
            on?.Invoke(it);
        }
    }
    public bool Set(I id, T t)
    {
        if (map.ContainsKey(id))
        {
            var old = map[id];
            map[id] = t;
            OnAdd(t);
            if (!old.Equals(t)) 
                OnChanged();
            else
                MaybeNeedSave();
            return true;
        }
        return false;
    }
    public int Count => map.Count;
    public void Clear()
    {
        if (map.Count == 0) return;
#if UNITY_EDITOR
        UserDataEditorWindowTmpStr?.Clear();
#endif
        map.Clear();
        OnChanged();
    }

    protected virtual void OnChanged()
    {
        MaybeNeedSave();
        OnChangeAction?.Invoke();
    }
    public virtual void SetOnChangeAction(Action on)
    {
        OnChangeAction = on;
        foreach (var a in map)
        {
            if (a.Value is CanSetOnChangeAction cs)
            {
                cs.SetOnChangeAction(on);
            }
        }
    }

    public bool NeedSave()
    {
        return m_NeedSave;
    }

    private void MaybeNeedSave()
    {
        m_NeedSave = true;
    }

    public void OnSaved()
    {
        m_NeedSave = false;
    }

    public override bool Equals(object obj)
    {
        if (obj is IndexableData<I, T> oth)
        {
            var dict3 = oth.map.Where(x => !map.ContainsKey(x.Key) || !map[x.Key].Equals(x.Value))
                         .Union(map.Where(x => !oth.map.ContainsKey(x.Key) || !oth.map[x.Key].Equals(x.Value)))
                         .ToDictionary(x => x.Key, x => x.Value);
            return dict3.Count == 0;
        }
        return false;
    }
    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}
public interface IPersistentData
{
    void Load(string parentPath);
    void Save(bool force = false);
    void Dispose();
}
public class PersistentDataset : IPersistentData
{
    protected List<IPersistentData> datas = new List<IPersistentData>();

    public void Add(IPersistentData d)
    {
        datas.Add(d);
    }
    public void Dispose()
    {
        foreach (var it in datas)
            it.Dispose();
        datas.Clear();
    }

    public void Load(string parentPath)
    {
        foreach (var it in datas)
            it.Load(parentPath);
    }

    public void Save(bool force = false)
    {
        foreach (var it in datas)
        {
            it.Save(force);
        }
    }
}


public static class ListExt
{
    public static string toStr<T>(this IEnumerable<T> self, char delimiter = ',', bool hasBrackets = false, bool rmLastDelimiter = true)
    {
        return toStr(self, (a) => a.ToString(), delimiter, hasBrackets, rmLastDelimiter);
    }
    public static string toStr<T>(this IEnumerable<T> self, Func<T, string> to, char delimiter = ',', bool hasBrackets = false, bool rmLastDelimiter = true)
    {
        var sb = new StringBuilder();
        if (hasBrackets)
            sb.Append("[");
        int count = 0;
        foreach (var a in self)
        {
            sb.Append(to(a));
            sb.Append(delimiter);
            ++count;
        }
        if (count > 0 && rmLastDelimiter)
            sb.Remove(sb.Length - 1, 1);
        if (hasBrackets)
            sb.Append("]");
        return sb.ToString();
    }

    public static void MakeUp<T>(this List<T> self,int count)
    where T:new()
    {
         if(self.Count >= count) return;
         var len = count - self.Count;
         for (int i = 0; i < len; ++i)
         {
             self.Add(new T());
         }
    }
    public static void MakeUpDef<T>(this List<T> self,int count)
    {
        if(self.Count >= count) return;
        var len = count - self.Count;
        for (int i = 0; i < len; ++i)
        {
            self.Add(default(T));
        }
    }
    public static void Swap<T>(this List<T> self, int a, int b)
    {
        if(a < 0 || a >= self.Count || b < 0 || b > self.Count) return;
        (self[a], self[b]) = (self[b], self[a]);
    }
    public static void MoveRightLoop<T>(this List<T> self)
    {
        if(self.Count <= 1) return;
        var a = self[self.Count - 1];
        for (int i = self.Count - 1; i > 0; --i)
        {
            self[i] = self[i - 1];
        }
        self[0] = a;
    }
    public static void MoveLeftLoop<T>(this List<T> self)
    {
        if(self.Count <= 1) return;
        var a = self[0];
        for (int i = 1; i < self.Count; ++i)
        {
            self[i - 1] = self[i];
        }
        self[self.Count - 1] = a;
    }

    public static int CopyToSafe<T>(this List<T> self, T[] arr)
    {
        int res = Math.Min(self.Count, arr.Length);
        for (int i = 0; i < res; ++i)
        {
            arr[i] = self[i];
        }
        return res;
    }
}

public static class StringEx
{
    public static bool isVaild(this string self)
    {
        return !string.IsNullOrEmpty(self);
    }
}

public static class DictEx
{
    public static bool RemoveOther<K, V>(this Dictionary<K, V> map, List<K> exclude)
    {
        var list = new List<K>();
        foreach (var it in map)
        {
            if (exclude.Contains(it.Key)) continue;
            list.Add(it.Key);
        }
        var f = false;
        foreach (var it in list)
        {
            if (map.Remove(it))
                f = true;
        }
        return f;
    }
}

namespace CommTag
{
    public class OpenTag : TickTag { }
    public class CloseTag : TickTag { }
}
 namespace TagNum
 {
     public class _0 : TickTag { }
     public class _1 : TickTag { }
     public class _2 : TickTag { }
     public class _3 : TickTag { }
     public class _4 : TickTag { }
     public class _5 : TickTag { }
     public class _6 : TickTag { }
     public class _7 : TickTag { }
     public class _8 : TickTag { }
     public class _9 : TickTag { }
     public static class NumMap
     {
         // public static TickTag Get(int i)
         // {
         //     var ty = GetTy(i);
         //     if (ty == null) return null;
         //     return (TickTag)System.Activator.CreateInstance(ty);
         // }
         // public static Type GetTy(int i)
         // {
         //     return Type.GetType($"TagNum._{i}");
         // }
         public static TickTag Make<T>(int i)
            where T:TickTagDynGenericNum,new()
         {
             var t = new T();
             t.SetGeneric(new int[]{i});
             return t;
         }
         public static TickTag MakeEx<T>(params int[] i)
             where T:TickTagDynGenericNum,new()
         {
             var t = new T();
             t.SetGeneric(i);
             return t;
         }

         // public static Type MakeType(Type parentTy, params int[] i)
         // {
         //     var Ts = new Type[i.Length];
         //     for (int a = 0; a < i.Length; ++a)
         //     {
         //         Type t = null;
         //         if ((t = GetTy(i[a])) == null)
         //             return null;
         //         Ts[a] = t;
         //     }
         //     return parentTy.MakeGenericType(Ts);
         // }
         public static TickTag Make<T,F>(params int[] i)
             where T:TickTagDynGeneric,new()
             where F:TickTag,new()
         {
             var t = new T();
             t.SetGeneric(new TickTag[]{ new F(),new TickTagDynGenericNum(i)});
             return t;
         }
         public static bool HasNumTags<T>(this TickGroup self, int b = 0, int e = 2)
             where T:TickTagDynGenericNum,new()
         {
             for (int i = b; i < e; ++i)
             {
                 if (self.HasTag(Make<T>(i)))
                     return true;
             }
             return false;
         }
     }
 }