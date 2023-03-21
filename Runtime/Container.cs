using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Collections;

namespace Transient.Container {
    public sealed class StorageList<E> : IEnumerable<E>
            where E : new() {
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

        public struct Enumerator : IDisposable, IEnumerator<E>, IEnumerator {
            private readonly StorageList<E> list;
            private int index;

            public E Current { get; private set; }
            object IEnumerator.Current => Current;

            internal Enumerator(StorageList<E> list_) {
                list = list_;
                index = 0;
                Current = default;
            }

            public void Dispose() { }

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
        IEnumerator<E> IEnumerable<E>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

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
}

namespace Transient {
    public static class Extension {
        public static void OutOfOrderRemove<E>(this IList<E> list, E v) {
            int vi = list.IndexOf(v);
            if (vi >= 0) {
                OutOfOrderRemoveAt(list, vi);
            }
        }

        public static void OutOfOrderRemoveAt<E>(this IList<E> list, int r) {
            var last = list.Count - 1;
            list[r] = list[last];
            list.RemoveAt(last);
        }

        public static void OutOfOrderRemove<E>(this IList<E> list, Func<E, bool> condition_) {
            int b = 0;
            for (var a = 0; a < list.Count; ++a) {
                if (condition_(list[a])) {
                    list[a] = default;
                }
                else {
                    list[b] = list[a];
                    ++b;
                }
            }
            for (var k = list.Count - 1; k >= b; --k) {
                list.RemoveAt(k);
            }
        }

        public static void Push<E>(this IList<E> list, E e) {
            list.Add(e);
        }

        public static E Pop<E>(this IList<E> list) {
            var last = list.Count - 1;
            E rval = list[last];
            list.RemoveAt(last);
            return rval;
        }

        public static E Peek<E>(this IList<E> list) => list[list.Count - 1];

        public static void BubbleSort<E>(this IList<E> data, Comparison<E> Compare) {
            int a = 0;
            E t;
            while (++a < data.Count) {
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