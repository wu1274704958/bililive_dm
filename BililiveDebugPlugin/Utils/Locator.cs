using System;
using System.Collections.Concurrent;

namespace Utils
{
    public class Locator : Singleton<Locator>
    {
        private ConcurrentDictionary<Type,object> _pools = new ConcurrentDictionary<Type, object>();
        private ConcurrentDictionary<object,ConcurrentDictionary<Type, object>> _poolsOfObj = new ConcurrentDictionary<object,ConcurrentDictionary<Type, object>>();

        public T Get<T>()
        {
            if (TryGetByPool(typeof(T), out object res))
                return (T)res;
            return default(T);
        }
        
        public T Get<T>(object depositor)
        {
            if (TryGetByDepositor(depositor,typeof(T),out object res))
                return (T)res;
            return Get<T>();
        }

        private bool TryGetByDepositor(object depositor, Type t, out object res)
        {
            res = null;
            if(_poolsOfObj.TryGetValue(depositor,out ConcurrentDictionary<Type,object> pool))
                return TryGetByPool(pool,t,out res);
            return false;
        }

        private bool TryGetByPool(ConcurrentDictionary<Type,object> pool, Type type, out object o)
        {
            Type t = type;
            do
            {
                if (pool.TryGetValue(t, out o))
                    return true;
            } while ((t = t.BaseType) != null);

            if (type.IsInterface)
            {
                foreach (var v in pool)
                {
                    if (type.IsAssignableFrom(v.Key))
                    {
                        o = v.Value;
                        return true;
                    }
                }
            }
            foreach (var v in pool)
            {
                t = v.Key;
                do
                {
                    if((t.IsInterface && t.IsAssignableFrom(type)) || t == type)
                    {
                        o = v.Value;
                        return true;
                    }
                } while ((t = t.BaseType) != null);
            }
            return false;
        }
        public bool TryGetByPool(Type type, out object o)
        {
            return TryGetByPool(_pools, type, out o);
        }

        public void Deposit<T>(T t)
        {
            _pools.TryAdd(typeof(T), t);
        }

        public void Deposit<T>(object depositor,T t)
        {
            if (!_poolsOfObj.TryGetValue(depositor, out ConcurrentDictionary<Type, object> pool))
            {
                pool = new ConcurrentDictionary<Type, object>();
                _poolsOfObj.TryAdd(depositor, pool);
            }
            pool.TryAdd(typeof(T), t);
        }
        
        public T DepositOrExchange<T>(T t)
        {
            var ty = typeof(T);
            if (_pools.TryGetValue(ty, out var v))
            {
                _pools[ty] = t;
                return (T)v;
            }else
                _pools.TryAdd(ty, t);
            return default(T);
        }
        
    }
}