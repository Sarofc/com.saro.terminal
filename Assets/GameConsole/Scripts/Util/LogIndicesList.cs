using System;

namespace Saro.Console
{
    /* 
     * simple list
     * could be replaced by Syste.Collections.List
     */
    public class LogIndicesList
    {
        public int Count { get; private set; }
        public int Capacity => m_indices.Length;
        public int this[int idx] => m_indices[idx];


        private int[] m_indices;

        public LogIndicesList(int capacity = 64)
        {
            m_indices = new int[capacity];
            Count = 0;
        }

        public void Add(int idx)
        {
            if (Count == Capacity)
            {
                ExpandCapacity();
            }
            m_indices[Count++] = idx;
        }

        public void Clear()
        {
            Count = 0;
        }

        private void ExpandCapacity()
        {
            int[] newArr = new int[Count * 2];
            Array.Copy(m_indices, 0, newArr, 0, Count);
            m_indices = newArr;
        }
    }
}
