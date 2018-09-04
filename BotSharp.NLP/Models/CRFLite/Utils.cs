using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotSharp.Models.CRFLite
{
    public class QueueElement
    {
        public Node node;
        public QueueElement next;
        public double fx;
        public double gx;
    };

    public class Heap
    {
        public int capacity;
        public int elem_size; //size of elem_list
        public int size;	// size of elem_ptr_list
        public List<QueueElement> elem_ptr_list;
        public List<QueueElement> elem_list;
    };

    public class BaseUtils
    {
        public const double eps = 1e-7;


        public const int MINUS_LOG_EPSILON = 13;
        public const int DEFAULT_CRF_MAX_WORD_NUM = 100;

        public const int MODEL_TYPE_NORM = 100;

        public const int RETURN_INVALIDATED_FEATURE = -8;
        public const int RETURN_HEAP_SIZE_TOO_BIG = -7;
        public const int RETURN_INSERT_HEAP_FAILED = -6;
        public const int RETURN_EMPTY_FEATURE = -5;
        public const int RETURN_INVALIDATED_PARAMETER = -4;
        public const int RETURN_WRONG_STATUS = -3;
        public const int RETURN_TOO_LONG_WORD = -2;
        public const int RETURN_UNKNOWN = -1;
        public const int RETURN_SUCCESS = 0;

        public static Heap heap_init(int max_size)
        {
            Heap H;

            H = new Heap();
            H.capacity = max_size;
            H.size = 0;
            H.elem_size = 0;

            H.elem_ptr_list = new List<QueueElement>(max_size + 1);
            H.elem_list = new List<QueueElement>(max_size + 1);

            for (var z = 0; z < max_size; z++)
            {
                H.elem_list.Add(new QueueElement());
                H.elem_ptr_list.Add(null);
            }
            H.elem_list[0].fx = double.MinValue;
            H.elem_ptr_list.Add(H.elem_list[0]);

            return H;
        }

        public static QueueElement allc_from_heap(Heap H)
        {
            if (H.elem_size >= H.capacity)
            {
                return null;
            }
            else
            {
                return H.elem_list[++H.elem_size];
            }
        }

        public static int heap_insert(QueueElement qe, Heap H)
        {
            if (H.size >= H.capacity)
            {
                return BaseUtils.RETURN_HEAP_SIZE_TOO_BIG;
            }
            var i = ++H.size;
            while (i != 1 && H.elem_ptr_list[i / 2].fx > qe.fx)
            {
                H.elem_ptr_list[i] = H.elem_ptr_list[i / 2];  //此时i还没有进行i/2操作		
                i /= 2;
            }
            H.elem_ptr_list[i] = qe;
            return 0;
        }

        public static QueueElement heap_delete_min(Heap H)
        {
            var min_elem = H.elem_ptr_list[1];  //堆是从第1号元素开始的
            var last_elem = H.elem_ptr_list[H.size--];
            int i = 1, ci = 2;
            while (ci <= H.size)
            {
                if (ci < H.size && H.elem_ptr_list[ci].fx > H.elem_ptr_list[ci + 1].fx)
                {
                    ci++;
                }
                if (last_elem.fx <= H.elem_ptr_list[ci].fx)
                {
                    break;
                }
                H.elem_ptr_list[i] = H.elem_ptr_list[ci];
                i = ci;
                ci *= 2;
            }
            H.elem_ptr_list[i] = last_elem;
            return min_elem;
        }

        public static bool is_heap_empty(Heap H)
        {
            return H.size == 0;
        }

        public static void heap_reset(Heap H)
        {
            if (H != null)
            {
                H.size = 0;
                H.elem_size = 0;
            }
        }

        public static double logsumexp(double x, double y, bool flg)
        {
            if (flg)
            {
                return y;  // init mode
            }
            double vmin;
            double vmax;
            if (x > y)
            {
                vmin = y;
                vmax = x;
            }
            else
            {
                vmin = x;
                vmax = y;
            }

            if (vmax > vmin + MINUS_LOG_EPSILON)
            {
                return vmax;
            }
            return vmax + Math.Log(Math.Exp(vmin - vmax) + 1.0);
        }
    }
}
