using System;
using System.Collections.Concurrent;

namespace Utils
{
    public static class Locator
    {
        private static ConcurrentDictionary<Type,object> _pools = new ConcurrentDictionary<Type, object>();
        private static ConcurrentDictionary<object,ConcurrentDictionary<Type, object>> _poolsOfObj = new ConcurrentDictionary<object,ConcurrentDictionary<Type, object>>();

        public static T Get<T>()
        {
            if (TryGetByPool(typeof(T), out object res))
                return (T)res;
            return default(T);
        }
        
        public static T Get<T>(object depositor)
        {
            if (TryGetByDepositor(depositor,typeof(T),out object res))
                return (T)res;
            return Get<T>();
        }

        private static bool TryGetByDepositor(object depositor, Type t, out object res)
        {
            res = null;
            if(_poolsOfObj.TryGetValue(depositor,out ConcurrentDictionary<Type,object> pool))
                return TryGetByPool(pool,t,out res);
            return false;
        }

        private static bool TryGetByPool(ConcurrentDictionary<Type,object> pool, Type type, out object o)
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
        public static bool TryGetByPool(Type type, out object o)
        {
            return TryGetByPool(_pools, type, out o);
        }

        public static void Deposit<T>(T t)
        {
            _pools.TryAdd(typeof(T), t);
        }

        public static void Deposit<T>(object depositor,T t)
        {
            if (!_poolsOfObj.TryGetValue(depositor, out ConcurrentDictionary<Type, object> pool))
            {
                pool = new ConcurrentDictionary<Type, object>();
                _poolsOfObj.TryAdd(depositor, pool);
            }
            pool.TryAdd(typeof(T), t);
        }

        public static void Remove<T>()
        {
            _pools.TryRemove(typeof(T),out _);
        }

        public static void Remove<T>(object depositor)
        {
            if (_poolsOfObj.TryGetValue(depositor, out ConcurrentDictionary<Type, object> pool))
            {
                pool.TryRemove(typeof(T), out _);
            }
        }

        public static T DepositOrExchange<T>(T t)
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