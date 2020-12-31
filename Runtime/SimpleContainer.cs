using System;
using System.Linq;
using System.Reflection;
using Generic = System.Collections.Generic;

namespace Transient.SimpleContainer {
    public sealed class List<E> {
        E[] data;
        private readonly int ext;
        private readonly int extf;

        public E[] Data => data;
        public E this[int i] { get => data[i]; set => data[i] = value; }
        public int Count { get; private set; }

        public List(int c) : this(c, 1, 2) {
        }
        public List(int c, int ext_) : this(c, ext_, 1) {
        }
        private List(int c, int ext_, int extf_) {
            data = new E[c + 1];
            Count = 0;
            ext = ext_;
            extf = extf_;
        }

        #region Enumerator

        public struct Enumerator {
            private readonly List<E> list;
            private int index;

            public E Current { get; private set; }

            internal Enumerator(List<E> list_) {
                list = list_;
                index = 0;
                Current = default;
            }

            public bool MoveNext() {
                if (index < list.Count) {
                    Current = list.data[index];
                    ++index;
                    return true;
                }
                return MoveNextRare();
            }

            private bool MoveNextRare() {
                index = list.Count + 1;
                Current = default;
                return false;
            }

            public void Reset() {
                index = 0;
                Current = default;
            }
        }

        public Enumerator GetEnumerator() => new Enumerator(this);

        #endregion Enumerator

        public void CopyFrom(List<E> other_) {
            Ensure(other_.data.Length);
            Count = other_.Count;
            Array.Copy(other_.data, data, Count);
            Array.Clear(data, Count, data.Length - Count);
        }

        public void Clear() {
            if (Count <= 0) return;
            Array.Clear(data, 0, Count);
            Count = 0;
        }

        public bool Contains(E e) => Array.IndexOf(data, e, 0, Count) >= 0;

        public void Add(E e) {
            int sz = Count++;
            if (Count >= data.Length) {
                //UnityEngine.Debug.Log("resize " + data.Length);
                Array.Resize(ref data, data.Length * extf + ext);
            }
            data[sz] = e;
        }

        public void ExpandToInsertAt(int i, E e) {
            Count = i + 1;
            while (Count >= data.Length) {
                Array.Resize(ref data, data.Length * extf + ext);
            }
            data[i] = e;
        }

        public void Ensure(int size_) {
            if (data.Length >= size_)//no shrinking
                return;
            Array.Resize(ref data, size_);
        }

        public void Resize(int size_) {
            if (Count == size_)
                return;
            Count = size_;
            Array.Resize(ref data, size_);
        }

        public void Trim() {
            if (data.Length - Count > (data.Length >> 3)) {
                var rval = new E[Count];
                Array.Copy(data, rval, Count);
                data = rval;
            }
        }

        public E[] ToArray() {
            var rval = new E[Count];
            Array.Copy(data, rval, Count);
            return rval;
        }

        public void BubbleSort(Comparison<E> Compare) {
            int a = 0;
            E t;
            while (++a < Count) {
                for (int r = a, k = a - 1; r > 0; --r, --k) {
                    if (Compare(data[k], data[r]) > 0) {
                        t = data[k];
                        data[k] = data[r];
                        data[r] = t;
                    }
                    else {
                        goto innerbreak;
                    }
                }
                innerbreak: { }
            }
        }

        //Array.Sort caused allocation in ArraySortHelper<T>.Sort(), it is unclear how that happened
        public void Sort(Generic.IComparer<E> comparer) => Array.Sort(data, 0, Count, comparer);

        public int IndexOf(E v) => Array.IndexOf(data, v, 0, Count);

        public void Remove(E v) {
            int vi = IndexOf(v);
            if (vi >= 0) {
                RemoveAt(vi);
            }
        }

        public void OutOfOrderRemove(E v) {
            int vi = IndexOf(v);
            if (vi >= 0) {
                OutOfOrderRemoveAt(vi);
            }
        }

        public void RemoveAt(int r) {
            --Count;
            if (r < Count) {
                Array.Copy(data, r + 1, data, r, Count - r);
            }
            data[Count] = default;
        }

        public void OutOfOrderRemoveAt(int r) {
            --Count;
            data[r] = data[Count];
            data[Count] = default;
        }

        public void RemoveAll(Func<E, bool> condition_) {
            int b = 0;
            for (int a = 0; a < Count; ++a) {
                if (condition_(data[a])) {
                    data[a] = default;
                }
                else {
                    data[b] = data[a];
                    ++b;
                }
            }
            Count = b;
        }

        public void Push(E e) {
            if (Count == data.Length) {
                Array.Resize(ref data, Count + ext);
            }
            data[Count++] = e;
        }

        public E Pop() {
            E rval = data[--Count];
            data[Count] = default;
            return rval;
        }

        public E Peek() => data[Count - 1];
    }

    public sealed class Queue<E> {
        E[] data;
        int ps, pe;
        readonly int ext;
        public int Count { get; private set; }
        public E[] Data => data;

        public Queue(int c, int ext_) {
            data = new E[c];
            Count = 0;
            ext = ext_;
            ps = pe = 0;
        }

        public void Clear() {
            Array.Clear(data, 0, Count);
            Count = 0;
            ps = pe = 0;
        }

        public void Enqueue(E e) {
            if (Count == data.Length) {
                Array.Resize(ref data, Count + ext);
                if (ps > 0) {
                    int nps = ps + ext;
                    Array.Copy(data, ps, data, nps, Count - ps);
                    Array.Clear(data, ps, ext);
                    ps = nps;
                }
            }
            data[pe] = e;
            pe = (pe + 1) % data.Length;
            ++Count;
        }

        public E Dequeue() {
            E rval = data[ps];
            ps = (ps + 1) % data.Length;
            --Count;
            return rval;
        }

        public void Dequeue(int n) {
            ps = (ps + n) % data.Length;
            Count -= n;
        }

        public E Peek() => data[ps];

        public E Peek(int i) => data[(ps + i) % data.Length];

        public int RawIndex(int index) => (ps + index) % data.Length;

        #region Enumerator

        public struct Enumerator {
            private readonly Queue<E> list;
            private int index;

            public E Current { get; private set; }

            internal Enumerator(Queue<E> list_) {
                list = list_;
                index = 0;
                Current = default;
            }

            public bool MoveNext() {
                if (index < list.Count) {
                    Current = list.data[index];
                    ++index;
                    return true;
                }
                return MoveNextRare();
            }

            private bool MoveNextRare() {
                index = list.Count + 1;
                Current = default;
                return false;
            }

            public void Reset() {
                index = 0;
                Current = default;
            }
        }

        public Enumerator GetEnumerator() => new Enumerator(this);

        #endregion Enumerator
    }

    public sealed class StorageList<E> where E : new() {
        E[] data;
        readonly int ext;
        //may read into reserve list
        public E this[int i] { get => data[i]; set => data[i] = value; }
        public int Count { get; private set; }
        public int Reserve { get; private set; }
        private Func<E> CreateOne;

        public StorageList(int c, int ext_, Func<E> CreateOne_) {
            data = new E[c];
            Count = 0;
            Reserve = 0;
            ext = ext_;
            CreateOne = CreateOne_;
        }

        #region Enumerator

        public struct Enumerator {
            private readonly StorageList<E> list;
            private int index;

            public E Current { get; private set; }

            internal Enumerator(StorageList<E> list_) {
                list = list_;
                index = 0;
                Current = default;
            }

            public bool MoveNext() {
                if (index < list.Count) {
                    Current = list.data[index];
                    ++index;
                    return true;
                }
                return MoveNextRare();
            }

            private bool MoveNextRare() {
                index = list.Count + 1;
                Current = default;
                return false;
            }

            public void Reset() {
                index = 0;
                Current = default;
            }
        }

        public Enumerator GetEnumerator() => new Enumerator(this);

        #endregion Enumerator

        public void Clear() {
            Array.Clear(data, 0, data.Length);
            Count = 0;
            Reserve = 0;
        }

        public void DropReserve() {
            Array.Clear(data, Count, Reserve);
            Reserve = 0;
        }

        public E Add() {
            if (Reserve > 0) {
                --Reserve;
                return data[Count++];
            }
            //Reserve = 0
            if (Count == data.Length) Array.Resize(ref data, Count + ext);
            return data[Count++] = CreateOne();
        }

        public void Add(E e) {
            if (Count + Reserve + 1 >= data.Length) Array.Resize(ref data, Count + Reserve + 1 + ext);
            //move first reserve to last
            data[Count + Reserve + 1] = data[Count];
            //add new to last
            data[Count++] = e;
        }

        public void Remove(E e) => RemoveAt(Array.IndexOf(data, e, 0, Count));

        public E RemoveAt(int i) {
            E ret = data[i];
            Array.Copy(data, i + 1, data, i, Count - i - 1);
            data[Count + Reserve] = ret;
            --Count;
            ++Reserve;
            return ret;
        }

        public void OutOfOrderRemove(E e) => OutOfOrderRemoveAt(Array.IndexOf(data, e, 0, Count));

        public E OutOfOrderRemoveAt(int i) {
            E ret = data[i];
            --Count;
            data[i] = data[Count];
            data[Count] = ret;
            ++Reserve;
            return ret;
        }

        /// <summary>
        /// Add n items to index i,
        /// assume n is small(n < 100), use for loop
        /// </summary>
        /// <param name="i">start index</param>
        /// <param name="n">count</param>
        /// <returns>count from reserve</returns>
        public void AddTo(int i, int n) {
            int toAdd = n - Reserve;
            if (Count + toAdd >= data.Length) Array.Resize(ref data, Count + toAdd);
            var temp = new E[n];
            int r;
            if (Reserve >= n) {
                for (r = 0; r < n; ++r) {
                    temp[r] = data[Count + Reserve - n + r];
                }
                Reserve -= n;
            }
            else {
                for (r = 0; r < Reserve; ++r) {
                    temp[r] = data[Count + r];
                }
                Reserve = 0;
                for (; r < n; ++r) {
                    temp[r] = CreateOne();
                }
            }
            Array.Copy(data, i, data, i + n, Count + Reserve - i);
            Count += n;
            for (r = 0; r < n; ++r) {
                data[i + r] = temp[r];
            }
        }

        //assume n is small(n < 100), use for loop
        public void RemoveAt(int i, int n) {
            var temp = new E[n];
            for (int r = 0; r < n; ++r) {
                temp[r] = data[i + r];
            }
            Count -= n;
            Array.Copy(data, i + n, data, i, Count - i);
            for (int r = 0; r < n; ++r) {
                data[Count + r] = temp[r];
            }
            Reserve += n;
        }

        public void RemoveAll() {
            Reserve += Count;
            Count = 0;
        }
    }

    //trimed from (4.0) System.Collections.Generic.HashHelpers
    internal static class HashHelpers {
        internal static readonly int[] primes = new int[] {
            3, 7, 11, 17, 23, 29, 37, 47, 59, 71, 89, 107, 131, 163, 197, 239, 293, 353, 431, 521, 631, 761, 919,
            1103, 1327, 1597, 1931, 2333, 2801, 3371, 4049, 4861, 5839, 7013, 8419, 10103,
            12143, 14591, 17519, 21023, 25229, 30293, 36353, 43627, 52361, 62851, 75431, 90523, 108631,
            130363, 156437, 187751, 225307, 270371, 324449, 389357, 467237, 560689, 672827, 807403, 968897,
            1162687, 1395263, 1674319, 2009191, 2411033, 2893249, 3471899, 4166287, 4999559, 5999471, 7199369
        };

        internal static bool IsPrime(int candidate) {
            if ((candidate & 1) != 0) {
                int num = (int)Math.Sqrt(candidate);
                for (int i = 3; i <= num; i += 2) {
                    if (candidate % i == 0) {
                        return false;
                    }
                }
                return true;
            }
            return candidate == 2;
        }

        internal static int GetPrime(int min) {
            for (int i = 0; i < primes.Length; i++) {
                int num = primes[i];
                if (num >= min) {
                    return num;
                }
            }
#if DEBUG
            throw new InvalidOperationException("expensive op: prime value exceeds lookup table max value [" + min + "]");
#else
		    for (int j = min|1; j < 2147483647; j += 2) {
			    if (IsPrime(j) && (j - 1) % 101 != 0) {
				    return j;
			    }
		    }
		    return min;
#endif
        }

        public static int ExpandPrime(int oldSize) {
            int num = 2 * oldSize;
            if (num > 2146435069 && 2146435069 > oldSize) {
                return 2146435069;
            }
            return GetPrime(num);
        }

        internal static int GetMinPrime() => primes[0];
    }

    //trimed from (3.5) System.Collections.Generic.HashSet<T>
    public sealed class HashSet<T> {
        internal struct Slot {
            internal int hashCode;
            internal T value;
            internal int next;
        }

        private int[] m_buckets;
        private Slot[] m_slots;
        private int m_lastIndex;
        private int m_freeList;
        private readonly Generic.IEqualityComparer<T> m_comparer;

        public int Count { get; private set; }

        public HashSet(int c, Generic.IEqualityComparer<T> comparer) {
#if DEBUG
            if (comparer == null) {
                throw new ArgumentNullException("comparer should not be null!");
            }
#endif
            m_comparer = comparer ?? ComparerHelper.GetComparer<T>();
            m_lastIndex = 0;
            Count = 0;
            m_freeList = -1;

            int prime = HashHelpers.GetPrime(c);
            m_buckets = new int[prime];
            m_slots = new Slot[prime];
        }

        #region Enumerator

        public struct Enumerator {
            private readonly HashSet<T> set;
            private int index;
            public T Current { get; private set; }

            internal Enumerator(HashSet<T> set) {
                this.set = set;
                index = 0;
                Current = default;
            }

            public bool MoveNext() {
                while (index < set.m_lastIndex) {
                    if (set.m_slots[index].hashCode >= 0) {
                        Current = set.m_slots[index].value;
                        ++index;
                        return true;
                    }
                    ++index;
                }
                index = set.m_lastIndex + 1;
                Current = default;
                return false;
            }

            public void Reset() {
                index = 0;
                Current = default;
            }
        }

        public Enumerator GetEnumerator() => new Enumerator(this);

        #endregion Enumerator

        public void Clear() {
            if (m_lastIndex > 0) {
                Array.Clear(m_slots, 0, m_lastIndex);
                Array.Clear(m_buckets, 0, m_buckets.Length);
                m_lastIndex = 0;
                Count = 0;
                m_freeList = -1;
            }
        }

        public bool Contains(T item) {
            if (m_buckets != null) {
                int num = InternalGetHashCode(item);
                for (int i = m_buckets[num % m_buckets.Length] - 1; i >= 0; i = m_slots[i].next) {
                    if (m_slots[i].hashCode == num && m_comparer.Equals(m_slots[i].value, item)) {
                        return true;
                    }
                }
            }
            return false;
        }

        public bool Remove(T item) {
            if (m_buckets != null) {
                int num = InternalGetHashCode(item);
                int num2 = num % m_buckets.Length;
                int num3 = -1;
                for (int i = m_buckets[num2] - 1; i >= 0; i = m_slots[i].next) {
                    if (m_slots[i].hashCode == num && m_comparer.Equals(m_slots[i].value, item)) {
                        if (num3 < 0) {
                            m_buckets[num2] = m_slots[i].next + 1;
                        }
                        else {
                            m_slots[num3].next = m_slots[i].next;
                        }
                        m_slots[i].hashCode = -1;
                        m_slots[i].value = default;
                        m_slots[i].next = m_freeList;
                        m_freeList = i;
                        --Count;
                        return true;
                    }
                    num3 = i;
                }
            }
            return false;
        }

        public void TrimExcess() {
            if (Count == 0) {
                m_buckets = null;
                m_slots = null;
                return;
            }
            int prime = HashHelpers.GetPrime(Count);
            Slot[] array = new Slot[prime];
            int[] array2 = new int[prime];
            int num = 0;
            for (int i = 0; i < m_lastIndex; i++) {
                if (m_slots[i].hashCode >= 0) {
                    array[num] = m_slots[i];
                    int num2 = array[num].hashCode % prime;
                    array[num].next = array2[num2] - 1;
                    array2[num2] = num + 1;
                    num++;
                }
            }
            m_lastIndex = num;
            m_slots = array;
            m_buckets = array2;
            m_freeList = -1;
        }

        private void IncreaseCapacity() {
            int num = Count * 2;
            if (num < 0) {
                num = Count;
            }
            int prime = HashHelpers.ExpandPrime(num);
            if (prime <= Count) {
                throw new ArgumentException("Arg_HSCapacityOverflow");
            }
            Slot[] array = new Slot[prime];
            if (m_slots != null) {
                Array.Copy(m_slots, 0, array, 0, m_lastIndex);
            }
            int[] array2 = new int[prime];
            for (int i = 0; i < m_lastIndex; i++) {
                int num2 = array[i].hashCode % prime;
                array[i].next = array2[num2] - 1;
                array2[num2] = i + 1;
            }
            m_slots = array;
            m_buckets = array2;
        }

        public bool Add(T value) {
            int num = InternalGetHashCode(value);
            int num2 = num % m_buckets.Length;
            for (int i = m_buckets[num % m_buckets.Length] - 1; i >= 0; i = m_slots[i].next) {
                if (m_slots[i].hashCode == num && m_comparer.Equals(m_slots[i].value, value)) {
                    return false;
                }
            }
            int num3;
            if (m_freeList >= 0) {
                num3 = m_freeList;
                m_freeList = m_slots[num3].next;
            }
            else {
                if (m_lastIndex == m_slots.Length) {
                    IncreaseCapacity();
                    num2 = num % m_buckets.Length;
                }
                num3 = m_lastIndex;
                m_lastIndex++;
            }
            m_slots[num3].hashCode = num;
            m_slots[num3].value = value;
            m_slots[num3].next = m_buckets[num2] - 1;
            m_buckets[num2] = num3 + 1;
            Count++;
            return true;
        }

        private int InternalGetHashCode(T item) {
            if (item == null) {
                return 0;
            }
            return m_comparer.GetHashCode(item) & 2147483647;
        }
    }

    public static class ComparerHelper {
        public static Generic.IEqualityComparer<T> GetComparer<T>() {
            var type = typeof(T);
            if (type == typeof(int)) {
                return (Generic.IEqualityComparer<T>)(object)IntComparer.Default;
            }
#if DEBUG
            if (type.IsValueType) {
                Log.Warning($"using object comparer on value type {type.Name}");
            }
#endif
            return ObjectComparer<T>.Default;
        }
    }

    public sealed class ObjectComparer<T> : Generic.IEqualityComparer<T> {
        public static ObjectComparer<T> Default = new ObjectComparer<T>();
        private ObjectComparer() { }
        public bool Equals(T x, T y) {
            return object.Equals(x, y);
        }
        public int GetHashCode(T obj) {
            return obj.GetHashCode();
        }
    }

    public sealed class IntComparer : Generic.IEqualityComparer<int> {
        public static IntComparer Default = new IntComparer();
        private IntComparer() { }
        public bool Equals(int x, int y) {
            return x == y;
        }
        public int GetHashCode(int a) {
            return a;
        }
    }

    //trimed from (4.0) System.Collections.Generic.Dictionary<TKey, TValue>
    public sealed class Dictionary<TKey, TValue> {
        private struct Entry {
            public int hashCode;
            public int next;
            public TKey key;
            public TValue value;
        }

        public struct Enumerator {
            private readonly Dictionary<TKey, TValue> dictionary;
            private int index;
            public (TKey key, TValue value) Current { get; private set; }

            internal Enumerator(Dictionary<TKey, TValue> dictionary) {
                this.dictionary = dictionary;
                index = 0;
                Current = default;
            }

            public bool MoveNext() {
                while (index < dictionary.count) {
                    if (dictionary.entries[index].hashCode >= 0) {
                        Current = (dictionary.entries[index].key, dictionary.entries[index].value);
                        ++index;
                        return true;
                    }
                    ++index;
                }
                index = dictionary.count + 1;
                Current = default;
                return false;
            }

            public void Reset() {
                index = 0;
                Current = default;
            }
        }

        private int[] buckets;
        private Entry[] entries;
        private int count;
        private int freeList;
        private int freeCount;
        private readonly Generic.IEqualityComparer<TKey> comparer;

        /// <summary>Gets the number of key/value pairs contained in the <see cref="T:System.Collections.Generic.Dictionary`2" />.</summary>
        /// <returns>The number of key/value pairs contained in the <see cref="T:System.Collections.Generic.Dictionary`2" />.</returns>
        public int Count => count - freeCount;

        /// <summary>Gets or sets the value associated with the specified key.</summary>
        /// <returns>The value associated with the specified key. If the specified key is not found, a get operation throws a <see cref="T:System.Collections.Generic.KeyNotFoundException" />, and a set operation creates a new element with the specified key.</returns>
        /// <param name="key">The key of the value to get or set.</param>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="key" /> is null.</exception>
        /// <exception cref="T:System.Collections.Generic.KeyNotFoundException">The property is retrieved and <paramref name="key" /> does not exist in the collection.</exception>
        public TValue this[TKey key] {
            get => ValueRef(key);
            set => Insert(key, value, false);
        }

        public Dictionary(int capacity) {
            Initialize(capacity);
            comparer = ComparerHelper.GetComparer<TKey>();
        }

        /// <summary>Initializes a new instance of the <see cref="T:System.Collections.Generic.Dictionary`2" /> class that is empty, has the specified initial capacity, and uses the specified <see cref="T:System.Collections.Generic.IEqualityComparer`1" />.</summary>
        /// <param name="capacity">The initial number of elements that the <see cref="T:System.Collections.Generic.Dictionary`2" /> can contain.</param>
        /// <param name="comparer">The <see cref="T:System.Collections.Generic.IEqualityComparer`1" /> implementation to use when comparing keys, or null to use the default <see cref="T:System.Collections.Generic.EqualityComparer`1" /> for the type of the key.</param>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        ///   <paramref name="capacity" /> is less than 0.</exception>
        public Dictionary(int capacity, Generic.IEqualityComparer<TKey> comparer) {
            Initialize(capacity);
            this.comparer = comparer ?? ComparerHelper.GetComparer<TKey>();
        }

        /// <summary>Adds the specified key and value to the dictionary.</summary>
        /// <param name="key">The key of the element to add.</param>
        /// <param name="value">The value of the element to add. The value can be null for reference types.</param>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="key" /> is null.</exception>
        /// <exception cref="T:System.ArgumentException">An element with the same key already exists in the <see cref="T:System.Collections.Generic.Dictionary`2" />.</exception>
        public void Add(TKey key, TValue value) => Insert(key, value, true);

        /// <summary>Removes all keys and values from the <see cref="T:System.Collections.Generic.Dictionary`2" />.</summary>
        public void Clear() {
            if (count > 0) {
                for (int i = 0; i < buckets.Length; ++i) {
                    buckets[i] = -1;
                }
                Array.Clear(entries, 0, count);
                freeList = -1;
                count = 0;
                freeCount = 0;
            }
        }

        /// <summary>Determines whether the <see cref="T:System.Collections.Generic.Dictionary`2" /> contains the specified key.</summary>
        /// <returns>true if the <see cref="T:System.Collections.Generic.Dictionary`2" /> contains an element with the specified key; otherwise, false.</returns>
        /// <param name="key">The key to locate in the <see cref="T:System.Collections.Generic.Dictionary`2" />.</param>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="key" /> is null.</exception>
        public bool ContainsKey(TKey key) => FindEntry(key) >= 0;

        /// <summary>Determines whether the <see cref="T:System.Collections.Generic.Dictionary`2" /> contains a specific value.</summary>
        /// <returns>true if the <see cref="T:System.Collections.Generic.Dictionary`2" /> contains an element with the specified value; otherwise, false.</returns>
        /// <param name="value">The value to locate in the <see cref="T:System.Collections.Generic.Dictionary`2" />. The value can be null for reference types.</param>
        public bool ContainsValue(TValue value, Generic.EqualityComparer<TValue> comparer) {
            if (value == null) {
                for (int i = 0; i < count; ++i) {
                    if (entries[i].hashCode >= 0 && entries[i].value == null) {
                        return true;
                    }
                }
            }
            else {
                for (int j = 0; j < count; ++j) {
                    if (entries[j].hashCode >= 0 && comparer.Equals(entries[j].value, value)) {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>Returns an enumerator that iterates through the <see cref="T:System.Collections.Generic.Dictionary`2" />.</summary>
        /// <returns>A <see cref="T:System.Collections.Generic.Dictionary`2.Enumerator" /> structure for the <see cref="T:System.Collections.Generic.Dictionary`2" />.</returns>
        public Enumerator GetEnumerator() => new Enumerator(this);

        private int FindEntry(TKey key) {
            int num = comparer.GetHashCode(key) & 2147483647;
            for (int i = buckets[num % buckets.Length]; i >= 0; i = entries[i].next) {
                if (entries[i].hashCode == num && comparer.Equals(entries[i].key, key)) {
                    return i;
                }
            }
            return -1;
        }

        private void Initialize(int capacity) {
            int prime = HashHelpers.GetPrime(capacity);
            buckets = new int[prime];
            for (int i = 0; i < buckets.Length; ++i) {
                buckets[i] = -1;
            }
            entries = new Entry[prime];
            freeList = -1;
        }

        private void Insert(TKey key, TValue value, bool add) {
            int num = comparer.GetHashCode(key) & 2147483647;
            int num2 = num % buckets.Length;
            //int num3 = 0;
            for (int i = buckets[num2]; i >= 0; i = entries[i].next) {
                if (entries[i].hashCode == num && comparer.Equals(entries[i].key, key)) {
                    if (add) {
                        throw new InvalidOperationException("duplicate adding " + key);
                    }
                    entries[i].value = value;
                    return;
                }
                //num3++;
            }
            int num4;
            if (freeCount > 0) {
                num4 = freeList;
                freeList = entries[num4].next;
                --freeCount;
            }
            else {
                if (count == entries.Length) {
                    Resize();
                    num2 = num % buckets.Length;
                }
                num4 = count;
                ++count;
            }
            entries[num4].hashCode = num;
            entries[num4].next = buckets[num2];
            entries[num4].key = key;
            entries[num4].value = value;
            buckets[num2] = num4;
            /*replace bad comparer with randomized default one
                if (num3 > 100 && HashHelpers.IsWellKnownEqualityComparer(this.comparer))
                {
                    this.comparer = (IEqualityComparer<TKey>)HashHelpers.GetRandomizedEqualityComparer(this.comparer);
                    this.Resize(this.entries.Length, true);
                }
                */
        }

        public ref TValue ValueRef(TKey key) {
            int num = FindEntry(key);
            if (num >= 0) {
                return ref entries[num].value;
            }
            throw new InvalidOperationException("key " + key + " not found");
        }

        private void Resize() {
            Resize(HashHelpers.ExpandPrime(count), false);
        }

        private void Resize(int newSize, bool forceNewHashCodes) {
            var array = new int[newSize];
            for (int i = 0; i < array.Length; ++i) {
                array[i] = -1;
            }
            var array2 = new Entry[newSize];
            Array.Copy(entries, 0, array2, 0, count);
            if (forceNewHashCodes) {
                for (int j = 0; j < count; ++j) {
                    if (array2[j].hashCode != -1) {
                        array2[j].hashCode = (comparer.GetHashCode(array2[j].key) & 2147483647);
                    }
                }
            }
            for (int k = 0; k < count; ++k) {
                if (array2[k].hashCode >= 0) {
                    int num = array2[k].hashCode % newSize;
                    array2[k].next = array[num];
                    array[num] = k;
                }
            }
            buckets = array;
            entries = array2;
        }

        /// <summary>Removes the value with the specified key from the <see cref="T:System.Collections.Generic.Dictionary`2" />.</summary>
        /// <returns>true if the element is successfully found and removed; otherwise, false.  This method returns false if <paramref name="key" /> is not found in the <see cref="T:System.Collections.Generic.Dictionary`2" />.</returns>
        /// <param name="key">The key of the element to remove.</param>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="key" /> is null.</exception>
        public bool Remove(TKey key) {
            if (buckets != null) {
                int num = comparer.GetHashCode(key) & 2147483647;
                int num2 = num % buckets.Length;
                int num3 = -1;
                for (int i = buckets[num2]; i >= 0; i = entries[i].next) {
                    if (entries[i].hashCode == num && comparer.Equals(entries[i].key, key)) {
                        if (num3 < 0) {
                            buckets[num2] = entries[i].next;
                        }
                        else {
                            entries[num3].next = entries[i].next;
                        }
                        entries[i].hashCode = -1;
                        entries[i].next = freeList;
                        entries[i].key = default;
                        entries[i].value = default;
                        freeList = i;
                        freeCount++;
                        return true;
                    }
                    num3 = i;
                }
            }
            return false;
        }

        /// <summary>Gets the value associated with the specified key.</summary>
        /// <returns>true if the <see cref="T:System.Collections.Generic.Dictionary`2" /> contains an element with the specified key; otherwise, false.</returns>
        /// <param name="key">The key of the value to get.</param>
        /// <param name="value">When this method returns, contains the value associated with the specified key, if the key is found; otherwise, the default value for the type of the <paramref name="value" /> parameter. This parameter is passed uninitialized.</param>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="key" /> is null.</exception>
        public bool TryGetValue(TKey key, out TValue value) {
            int num = FindEntry(key);
            if (num >= 0) {
                value = entries[num].value;
                return true;
            }
            value = default;
            return false;
        }

        internal TValue GetValueOrDefault(TKey key) {
            int num = FindEntry(key);
            if (num >= 0) {
                return entries[num].value;
            }
            return default;
        }
    }
}

namespace Transient.SimpleContainer {
    public static class ArrayExtensions {
        //minimum allowed length = 64, reference from unity A* Pathfinding project AStarMemory.cs
        //modified to remove Math.Min() call, and use Array.Copy to remove the byteSize
        //slightly better performance after eliminated Math.Min()
        public static void ArrayFill<T>(this T[] array, T value) where T : struct {
            int block = 64;
            for (int o = 0; o < block; ++o) {
                array[o] = value;
            }
            while (block * 2 < array.Length) {
                Array.Copy(array, 0, array, block, block);
                block *= 2;
            }
            Array.Copy(array, 0, array, block, array.Length - block);
        }

        public static T[] Convert<TSource, T>(this TSource[] source_, Func<TSource, T> converter_) {
            var ret = new T[source_.Length];
            int l = -1;
            foreach (var t in source_) {
                ret[++l] = converter_(t);
            }
            return ret;
        }
    }
}

namespace Transient.SimpleContainer {
    public static class LinqAlikeExtension {
        public static Dictionary<TKey, TSource> ToDictionary<TSource, TKey>(this Generic.IEnumerable<TSource> source, Func<TSource, TKey> keySelector, int capacity_ = 8) {
            var dict = new Dictionary<TKey, TSource>(capacity_);
            foreach (var elem in source) {
                dict.Add(keySelector(elem), elem);
            }
            return dict;
        }

        public static Generic.IEnumerable<T> Map<TSource, T>(this List<TSource> source_, Func<TSource, T> converter_) {
            foreach (var t in source_) {
                yield return converter_(t);
            }
        }

        public static Generic.IEnumerable<T> MapMany<TSource, T>(this List<TSource> source_, Func<TSource, Generic.IEnumerable<T>> converter_) {
            foreach (var s in source_) {
                foreach (var t in converter_(s)) {
                    yield return t;
                }
            }
        }

        public static Generic.IEnumerable<T> Map<TSource, T>(this TSource[] source_, Func<TSource, T> converter_) {
            foreach (var t in source_) {
                yield return converter_(t);
            }
        }

        public static Generic.IEnumerable<T> MapMany<TSource, T>(this TSource[] source_, Func<TSource, Generic.IEnumerable<T>> converter_) {
            foreach (var s in source_) {
                foreach (var t in converter_(s)) {
                    yield return t;
                }
            }
        }
    }
}