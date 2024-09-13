using Antlr4.Runtime.Misc;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utils
{
    public class EventDispatcher<T>
    {
        private readonly SortedDictionary<int, LinkedList<Action<T>>> _listeners = new SortedDictionary<int, LinkedList<Action<T>>>();

        public Action<T> AddListener(Action<T> action, int priority = 0)
        {
            lock (_listeners)
            {
                if (!_listeners.ContainsKey(priority))
                {
                    _listeners[priority] = new LinkedList<Action<T>>();
                }
                _listeners[priority].AddLast(action);
                return action;
            }
        }

        public bool RemoveListener(Action<T> action)
        {
            lock (_listeners)
            {
                foreach (var kvp in _listeners)
                {
                    if (kvp.Value.Remove(action))
                        return true;
                }
                return false;
            }
        }

        public void ClearListeners()
        {
            lock (_listeners)
            {
                _listeners.Clear();
            }
        }

        public void Dispatch(T arg)
        {
            lock (_listeners)
            {
                foreach (var kvp in _listeners.Reverse())
                {
                    foreach (var action in kvp.Value)
                    {
                        action.Invoke(arg);
                    }
                }
            }
        }
    }
}
