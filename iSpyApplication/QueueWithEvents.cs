using System;
using System.Collections.Generic;

namespace iSpyApplication
{
    public class QueueWithEvents<T>
    {
        private readonly Queue<T> _queue = new Queue<T>();
        public event EventHandler Changed;
        protected virtual void OnChanged()
        {
            if (Changed != null) Changed(this, EventArgs.Empty);
        }
        public virtual void Enqueue(T item)
        {
            _queue.Enqueue(item);
            OnChanged();
        }
        public int Count { get { return _queue.Count; } }

        public virtual T Dequeue()
        {
            T item = _queue.Dequeue();
            OnChanged();
            return item;
        }

        public virtual void  Clear()
        {
            _queue.Clear();
        }
    }
}
