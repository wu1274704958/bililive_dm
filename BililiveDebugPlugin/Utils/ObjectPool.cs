using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Utils
{
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
            if (item == null) return;
            _objectResetor?.Invoke(item);
            _objects.Add(item);
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
    
    public static class DefObjectRecycle{
        public static void OnListRecycle<T>(List<T> ls)
        {
            ls.Clear();   
        }
        public static void OnDictRecycle<K,V>(Dictionary<K,V> dict)
        {
            dict.Clear();
        }
    }
}