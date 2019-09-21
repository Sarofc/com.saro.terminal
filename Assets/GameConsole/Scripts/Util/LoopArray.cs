using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Saro.Console
{
    /*
     *  Warning : 
     *  
     *  1. Can't expand capacity
     *  2. Set a value
     *      - Will override the 'first' value when array is full
     *      - If the array contains the value, move it to the 'last'
     *  
     */
    public class LoopArray<T>
    {
        public int Capacity => m_arr.Length;
        public int Length => m_size;

        public T this[int idx] => m_arr[(m_tail + idx) % m_arr.Length];

        private int m_tail;
        private int m_size;

        private T[] m_arr;

        public LoopArray(int capacity = 8)
        {
            m_arr = new T[capacity];

            m_tail = 0;
            m_size = 0;
        }


        public void AddTail(T value)
        {
            if (m_size == m_arr.Length)
            {
                if (Contains(value, out int idx))
                {
                    var lastIdx = m_tail == 0 ? m_size - 1 : (m_tail - 1) % m_arr.Length;

                    if (idx == lastIdx)
                    {
                        return;
                    }
                    else if (idx < lastIdx)
                    {
                        var cur = m_arr[idx];
                        for (int i = idx + 1; i <= lastIdx; i++)
                        {
                            m_arr[i - 1] = m_arr[i];
                        }
                        m_arr[lastIdx] = cur;
                    }
                    else
                    {
                        var cur = m_arr[idx];
                        for (int i = idx + 1; i < m_size; i++)
                        {
                            m_arr[i - 1] = m_arr[i];
                        }

                        m_arr[m_size - 1] = m_arr[0];

                        for (int i = 1; i <= lastIdx; i++)
                        {
                            m_arr[i - 1] = m_arr[i];
                        }
                        
                        m_arr[lastIdx] = cur;
                    }
                }
                else
                {
                    m_arr[m_tail] = value;
                    m_tail = (m_tail + 1) % m_arr.Length;
                }
            }
            else
            {
                if (Contains(value, out int idx))
                {
                    var cur = m_arr[idx];
                    for (int i = idx + 1; i < m_size; i++)
                    {
                        m_arr[i - 1] = m_arr[i];
                    }
                    m_arr[m_size - 1] = cur;
                }
                else
                    m_arr[m_size++] = value;
            }
        }

        private bool Contains(T value, out int idx)
        {
            for (int i = 0; i < m_size; i++)
            {
                if (m_arr[i].Equals(value))
                {
                    idx = i;
                    return true;
                }
            }
            idx = -1;
            return false;
        }

        private void Swap(ref T i, ref T j)
        {
            var tmp = i;
            i = j;
            j = tmp;
        }
    }
}
