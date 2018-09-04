/*
 * SVM.NET Library
 * Copyright (C) 2008 Matthew Johnson
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */


using System;

namespace SVM.BotSharp.MachineLearning
{
    internal class Cache
    {
        private int _count;
        private long _size;

        private sealed class head_t
        {
            public head_t(Cache enclosingInstance)
            {
                _enclosingInstance = enclosingInstance;
            }
            private Cache _enclosingInstance;
            public Cache EnclosingInstance
            {
                get
                {
                    return _enclosingInstance;
                }
            }
            internal head_t prev, next; // a cicular list
            internal float[] data;
            internal int len; // data[0,len) is cached in this entry
        }

        private head_t[] head;
        private head_t lru_head;

        public Cache(int count, long size)
        {
            _count = count;
            _size = size;
            head = new head_t[_count];
            for (int i = 0; i < _count; i++)
                head[i] = new head_t(this);
            _size /= 4;
            _size -= _count * (16 / 4); // sizeof(head_t) == 16
            lru_head = new head_t(this);
            lru_head.next = lru_head.prev = lru_head;
        }

        private void lru_delete(head_t h)
        {
            // delete from current location
            h.prev.next = h.next;
            h.next.prev = h.prev;
        }

        private void lru_insert(head_t h)
        {
            // insert to last position
            h.next = lru_head;
            h.prev = lru_head.prev;
            h.prev.next = h;
            h.next.prev = h;
        }

        private static void swap<T>(ref T lhs, ref T rhs)
        {
            T tmp = lhs;
            lhs = rhs;
            rhs = tmp;
        }

        // request data [0,len)
        // return some position p where [p,len) need to be filled
        // (p >= len if nothing needs to be filled)
        // java: simulate pointer using single-element array
        public int GetData(int index, out float[] data, int len)
        {
            head_t h = head[index];
            if (h.len > 0)
                lru_delete(h);
            int more = len - h.len;

            if (more > 0)
            {
                // free old space
                while (_size < more)
                {
                    head_t old = lru_head.next;
                    lru_delete(old);
                    _size += old.len;
                    old.data = null;
                    old.len = 0;
                }

                // allocate new space
                float[] new_data = new float[len];
                if (h.data != null)
                    Array.Copy(h.data, 0, new_data, 0, h.len);
                h.data = new_data;
                _size -= more;
                swap(ref h.len, ref len);
            }

            lru_insert(h);
            data = h.data;
            return len;
        }

        public void SwapIndex(int i, int j)
        {
            if (i == j)
                return;

            if (head[i].len > 0)
                lru_delete(head[i]);
            if (head[j].len > 0)
                lru_delete(head[j]);
            swap(ref head[i].data, ref head[j].data);
            swap(ref head[i].len, ref head[j].len);
            if (head[i].len > 0)
                lru_insert(head[i]);
            if (head[j].len > 0)
                lru_insert(head[j]);

            if (i > j)
                swap(ref i, ref j);

            for (head_t h = lru_head.next; h != lru_head; h = h.next)
            {
                if (h.len > i)
                {
                    if (h.len > j)
                        swap(ref h.data[i], ref h.data[j]);
                    else
                    {
                        // give up
                        lru_delete(h);
                        _size += h.len;
                        h.data = null;
                        h.len = 0;
                    }
                }
            }
        }
    }
}
