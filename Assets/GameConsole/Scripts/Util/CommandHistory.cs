
namespace Saro.Console
{
    public class CommandHistory<T>
    {
        public T this[int idx] => m_arr[(idx + m_tail) % Capacity];
        public int Count { get; private set; }
        public readonly int Capacity;

        private T[] m_arr;
        private int m_tail;


        public CommandHistory(int capacity)
        {
            m_tail = 0;
            Count = 0;
            Capacity = capacity;

            m_arr = new T[capacity];
        }

        /*
         * if array contains value, move it to tail.
         * otherwise add to tail.
         */
        public void AddTail(T value)
        {
            // contains
            for (int i = 0; i < Count; i++)
            {
                if (m_arr[i].Equals(value))
                {
                    Down(i);
                    return;
                }
            }

            // new one
            if (Count < Capacity)
            {
                m_arr[Count++] = value;
            }
            else
            {
                m_arr[m_tail++] = value;
                if (m_tail >= Capacity) m_tail = 0;
            }
        }

        private void Down(int idx)
        {
            idx = (idx + m_tail) % Capacity;
            var lastIdx = m_tail - 1 < 0 ? Count - 1 : m_tail - 1;

            if (idx != lastIdx)
            {
                var tmp = m_arr[idx];
                if (idx > lastIdx)
                {
                    for (int i = idx + 1; i < Count; i++)
                    {
                        m_arr[i - 1] = m_arr[i];
                    }

                    Swap(Count - 1, 0);

                    for (int i = 1; i <= lastIdx; i++)
                    {
                        m_arr[i - 1] = m_arr[i];
                    }
                }
                else
                {
                    for (int i = idx + 1; i <= lastIdx; i++)
                    {
                        m_arr[i - 1] = m_arr[i];
                    }
                }
                m_arr[lastIdx] = tmp;
            }
        }

        private void Swap(int i, int j)
        {
            var tmp = m_arr[i];
            m_arr[i] = m_arr[j];
            m_arr[j] = tmp;
        }
    }
}
