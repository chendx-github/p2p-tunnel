using System.Collections.Concurrent;
using System.Collections.Generic;

namespace common.libs
{
    public class SimpleObjectPool<T> where T : new()
    {
        private ConcurrentStack<T> objectStack = new ConcurrentStack<T>();

        public SimpleObjectPool()
        {
        }

        public T Rent()
        {
            if (!objectStack.TryPop(out T t))
            {
                return new T();
            }
            return t;
        }

        public void Return(T aObject)
        {
            if (aObject == null) return;
            objectStack.Push(aObject);
        }

        public void Clear()
        {
            objectStack.Clear();
        }
    }
}
