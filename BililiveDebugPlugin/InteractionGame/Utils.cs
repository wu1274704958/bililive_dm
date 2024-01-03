using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace InteractionGame
{
    public class Utils
    {
        public static int CharToInt(char c)
        {
            int sub = c >= '0' && c <= '9' ? '0' : c >= 'a' && c <= 'z' ? ('a' - 10) : 0;
            return sub > 0 ? c - sub : -1;
        }
        private static Dictionary<int, int> TmpDict = new Dictionary<int, int>();
        public static Dictionary<int, int> StringToDict(string s)
        {
            TmpDict.Clear();
            for (int i = 0; i < s.Length; i++)
            {
                var v = CharToInt(s[i]);
                if (v < 0) continue;
                if (TmpDict.ContainsKey(v))
                    TmpDict[v] = TmpDict[v] + 1;
                else
                    TmpDict[v] = 1;
            }
            return TmpDict;
        }

        public static void StringToDictAndForeach(string s, Action<KeyValuePair<int, int>> f)
        {
            lock (TmpDict)
            {
                TmpDict.Clear();
                for (int i = 0; i < s.Length; i++)
                {
                    var v = CharToInt(s[i]);
                    if (v < 0) continue;
                    if (TmpDict.ContainsKey(v))
                        TmpDict[v] = TmpDict[v] + 1;
                    else
                        TmpDict[v] = 1;
                }
                foreach (var v in TmpDict)
                {
                    f?.Invoke(v);
                }
            }
        }
        public class TimeLinerInteger
        {
            private DateTime UpdateTime;
            private int factor;
            private int Value;
            public double AddFactor { get;set; } = 1;
            public double Rest = 0.0;
            public int Limit { get; private set; } = int.MaxValue;
            
            public TimeLinerInteger(int value, int factor = 1,int limit = int.MaxValue)
            {
                this.factor = factor;
                Value = value;
                UpdateTime = DateTime.Now;
                Limit = limit;
            }
            public int val
            {
                get
                {
                    UpdateVal();
                    return Value;
                }
            }

            public int Factor => factor;

            private void UpdateVal()
            {
                if(Value >= Limit)
                {
                    UpdateTime = DateTime.Now;
                    return;
                }
                var ts = DateTime.Now - UpdateTime;
                if (ts.TotalSeconds > 0)
                {
                    var old = Value;
                    var add = (int)(ts.TotalSeconds * factor);
                    Rest += AddFactor * add;
                    if(Rest > 1)
                    {
                        var v = Math.Truncate(Rest);
                        add += (int)v;
                        Rest -= v;
                    }
                    var @new = Value + add;
                    if (old != @new)
                    {
                        Value = @new;
                        UpdateTime = UpdateTime.AddSeconds(ts.TotalSeconds);
                    }
                }
            }
            public void SetNewFactor(int f)
            {
                if (f != factor)
                {
                    UpdateVal();
                    factor = f;
                }
            }
            public int Append(int a)
            {
                UpdateVal();
                Value += a;
                return Value;
            }
            public int Sub(int a)
            {
                UpdateVal();
                Value -= a;
                return Value;
            }
            public int SubNotNeg(int a)
            {
                UpdateVal();
                Value -= a;
                if (Value < 0)
                    Value = 0;
                return Value;
            }
        }

        public class ObjectPool<T>
        {
            private readonly ConcurrentBag<T> _objects;
            private readonly Func<T> _objectGenerator;
            private readonly Action<T> _objectResetor;

            public ObjectPool(Func<T> objectGenerator, Action<T> objectResetor = null)
            {
                _objectGenerator = objectGenerator ?? throw new ArgumentNullException(nameof(objectGenerator));
                _objects = new ConcurrentBag<T>();
                _objectResetor = objectResetor;
            }

            public T Get() => _objects.TryTake(out T item) ? item : _objectGenerator();

            public void Return(T item)
            {
                _objectResetor?.Invoke(item);
                _objects.Add(item);
            }
        }
    }
    //泛型单例基类
    public abstract class Singleton<T> where T : new()
    {
        private static T _instance;
        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new T();
                }
                return _instance;
            }
        }

        public static void _Dispose()
        {
            _instance = default(T);
        }
    }

    public class ObjPoolMgr : Singleton<ObjPoolMgr>
    {
        private ConcurrentDictionary<Type, object> _pools = new ConcurrentDictionary<Type, object>();

        public Utils.ObjectPool<T> Get<T>(Func<T> objectGenerator = null, Action<T> objectResetor = null)
            where T : new()
        {
            if(_pools.TryGetValue(typeof(T), out object pool))
                return pool as Utils.ObjectPool<T>;
            else
            {
                if(objectGenerator == null)
                    objectGenerator = () => new T();
                var p = new Utils.ObjectPool<T>(objectGenerator, objectResetor);
                _pools.TryAdd(typeof(T), p);
                return p;
            }
        }

        public void TryRemove<T>()
        {
            _pools.TryRemove(typeof(T),out _);
        }
    }

}

