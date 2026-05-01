using System.Collections.Generic;

namespace Storylines.DataStructures
{
    public class PartialStack<T>
    {
        public List<T> items = new List<T>();

        public int Count => items.Count;

        public void Push(T item)
        {
            items.Add(item);
        }

        public T Pop()
        {
            if (items.Count > 0)
            {
                T temp = items[items.Count - 1];
                items.RemoveAt(items.Count - 1);
                return temp;
            }
            else
                return default;
        }

        public void Clear()
        {
            items.Clear();
        }
    }
}
