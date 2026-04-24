namespace Polar.HabboHotel.Pathfinding
{
    internal sealed class MinHeap<T> where T : IComparable<T>
    {
        private T[] _array;
        private int _capacity;

        public MinHeap() : this(16) { }

        public MinHeap(int capacity)
        {
            Count = 0;
            _capacity = capacity;
            _array = new T[capacity];
        }

        public int Count { get; private set; }

        public void Clear()
        {
            Array.Clear(_array, 0, Count);
            Count = 0;
        }

        public void BuildHead()
        {
            for (int pos = (Count - 1) >> 1; pos >= 0; pos--)
                MinHeapify(pos);
        }

        public void Add(T item)
        {
            Count++;
            if (Count > _capacity) DoubleArray();

            _array[Count - 1] = item;
            int position = Count - 1;
            int parentPosition = (position - 1) >> 1;

            while (position > 0 && _array[parentPosition].CompareTo(_array[position]) > 0)
            {
                T tmp = _array[position];
                _array[position] = _array[parentPosition];
                _array[parentPosition] = tmp;
                position = parentPosition;
                parentPosition = (position - 1) >> 1;
            }
        }

        private void DoubleArray()
        {
            _capacity <<= 1;
            T[] newArray = new T[_capacity];
            Array.Copy(_array, newArray, _array.Length);
            _array = newArray;
        }

        public T ExtractFirst()
        {
            if (Count == 0) throw new InvalidOperationException("Heap is empty.");

            T first = _array[0];
            _array[0] = _array[Count - 1];
            _array[Count - 1] = default!;
            Count--;
            MinHeapify(0);
            return first;
        }

        private void MinHeapify(int position)
        {
            while (true)
            {
                int left = (position << 1) + 1;
                int right = left + 1;
                int min = position;

                if (left < Count && _array[left].CompareTo(_array[min]) < 0) min = left;
                if (right < Count && _array[right].CompareTo(_array[min]) < 0) min = right;

                if (min == position) return;

                T tmp = _array[position];
                _array[position] = _array[min];
                _array[min] = tmp;
                position = min;
            }
        }
    }
}