using System.Collections.Generic;

namespace WpfApplication1
{
    /// <summary>
    /// Приоритетная очередь
    /// </summary>
    public class PriorityQueue<T>
    {
        private List<PriorityObject<T>> array;

        public PriorityQueue(int c)
        {
            array = new List<PriorityObject<T>>(c);
        }

        public List<PriorityObject<T>> Array { get { return array; } }

        /// <summary>
        /// Количество объектов
        /// </summary>
        public int Count
        {
            get { return array.Count; }
        }

        /// <summary>
        /// Проверяет, пустая ли очередь
        /// </summary>
        /// <returns></returns>
        public bool Empty()
        {
            return array.Count == 0;
        }

        /// <summary>
        /// Очистить очередь
        /// </summary>
        public void Clear()
        {
            array.Clear();
        }

        /// <summary>
        /// Добавить объект в очередь
        /// </summary>
        /// <param name="t"></param>
        /// <param name="priority"></param>
        public void Add(T t, float priority)
        {
            array.Add(new PriorityObject<T>(t, priority));
            //buildHeap();
        }

        /// <summary>
        /// Взять объект с заданным индексом
        /// </summary>
        /// <param name="ind"></param>
        /// <returns></returns>
        public T GetAt(int ind)
        {
            return array[ind].obj;
        }

        /// <summary>
        /// Получить очередной объект из очередь
        /// </summary>
        /// <returns></returns>
        public T Get()
        {
            PriorityObject<T> d = array[array.Count - 1];
            array.RemoveAt(array.Count - 1);
            //if (array.Count > 0)
            //     buildHeap();
            return d.obj;
        }

        /// <summary>
        /// Получить очередной объект в виде PriorityObject
        /// </summary>
        /// <returns></returns>
        public PriorityObject<T> GetPriorityObject()
        {
            PriorityObject<T> d = array[array.Count - 1];
            array.RemoveAt(array.Count - 1);
            //  if (array.Count > 0)
            //     buildHeap();
            return d;
        }

        /// <summary>
        /// Протолкнуть элемент кучи
        /// </summary>
        /// <param name="l"></param>
        /// <param name="r"></param>
        private void down(int l, int r)
        {
            PriorityObject<T> temp = array[l];
            while (l * 2 <= r)
            {
                int child = l * 2;
                if (child + 1 <= r && array[child + 1] > array[child]) child++;
                if (temp >= array[child]) break;
                array[l] = array[child];
                l = child;
            }
            array[l] = temp;
        }

        /// <summary>
        /// Собрать кучу
        /// </summary>
        public void buildHeap()
        {
            for (int i = array.Count / 2; i >= 0; i--)
            {
                down(i, array.Count - 1);
            }
            for (int i = array.Count - 1; i > 0; i--)
            {
                PriorityObject<T> t = array[i];
                array[i] = array[0];
                array[0] = t;

                down(0, i - 1);
            }
        }
    }

    /// <summary>
    /// Объект с приоритетом
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class PriorityObject<T>
    {
        public float v;
        public T obj;

        public PriorityObject(T obj, float v)
        {
            this.v = v;
            this.obj = obj;
        }

        /// <summary>
        /// Задание линейного порядка
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator >(PriorityObject<T> a, PriorityObject<T> b)
        {
            return a.v > b.v;
        }

        public static bool operator <(PriorityObject<T> a, PriorityObject<T> b)
        {
            return a.v < b.v;
        }

        public static bool operator >=(PriorityObject<T> a, PriorityObject<T> b)
        {
            return a.v >= b.v;
        }

        public static bool operator <=(PriorityObject<T> a, PriorityObject<T> b)
        {
            return a.v <= b.v;
        }
    }
}
