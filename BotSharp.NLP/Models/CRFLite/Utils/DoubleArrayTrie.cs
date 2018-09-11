using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;

#if NO_SUPPORT_PARALLEL_LIB
#else
using System.Threading.Tasks;
#endif

namespace BotSharp.Models.CRFLite.Utils
{
    public class unit_t : IComparable<unit_t>
    {
        public int base1;
        public int check;

        public int CompareTo(unit_t obj)
        {
            throw new NotImplementedException();
        }
    }

    public struct sunit_t : IComparable<sunit_t>
    {
        public int base1;
        public int check;

        public sunit_t(int b, int c)
        {
            base1 = b;
            check = c;
        }

        public int CompareTo(sunit_t obj)
        {
            throw new NotImplementedException();
        }
    }

    public class DoubleArrayTrieSearch
    {
#if NO_SUPPORT_VERY_BIG_OBJECT
        private VarBigArray<sunit_t> array;
#else
        private sunit_t[] array;
#endif

        /// <summary>
        /// Loads ArrayTrie from file
        /// </summary>
        /// <param name="fileName">path to file</param>
        /// <param name="numberOfElementsInChunk">
        /// Number of elements (2 int32) in read buffer.
        /// Default is 2048 (16K buffer size)
        /// </param>
        public void Load(string fileName, int numberOfElementsInChunk = 2048)
        {
            if(!File.Exists(fileName))
                throw new FileNotFoundException(
                    "Please check that the specified file exists", fileName);
            using (var stream = File.OpenRead(fileName))
                Load(stream, numberOfElementsInChunk);
        }

        /// <summary>
        /// Loads ArrayTrie from an arbitrary <see cref="Stream"/>.
        /// <paramref name="sourceStream"/> is closed and
        /// disposed once this method completes.
        /// </summary>
        /// <param name="sourceStream">
        /// A <see cref="Stream"/> containing the model.
        /// </param>
        /// <param name="numberOfElementsInChunk">
        /// Number of elements (2 int32) in read buffer. 
        /// Default is 2048 (16K buffer size)</param>
        public void Load(Stream sourceStream, int numberOfElementsInChunk = 2048)
        {
            const int int32Size = sizeof(int);
            const int elementSize = int32Size * 2;
            var fileSizeInBytes = sourceStream.Length;
            var numberOfElements = fileSizeInBytes / elementSize;
#if NO_SUPPORT_VERY_BIG_OBJECT
            array = new VarBigArray<sunit_t>(numberOfElements);
#else
            array = new sunit_t[numberOfElements];
#endif
            using (var sr = new StreamReader(sourceStream))
            using (var br = new BinaryReader(sr.BaseStream))
            {
                var buffersize = elementSize * numberOfElementsInChunk;
                var buffer = new byte[elementSize * numberOfElementsInChunk];
                var index = 0;
                for (long j = 0; j <= numberOfElements / numberOfElementsInChunk; j++)
                {
                    var numberOfReadBytes = br.Read(buffer, 0, buffersize);
                    if (numberOfReadBytes == buffersize)
                    {
                        for (int i = 0; i < numberOfElementsInChunk; i++, index++)
                        {
                            var base1 = BitConverter.ToInt32(buffer, elementSize * i);
                            var check = BitConverter.ToInt32(buffer, (elementSize * i) + int32Size);
                            array[index] = new sunit_t(base1, check);
                        }
                    }
                    else
                    {
                        for (int i = 0; i < numberOfReadBytes / elementSize; i++)
                        {
                            var base1 = BitConverter.ToInt32(buffer, elementSize * i);
                            var check = BitConverter.ToInt32(buffer, (elementSize * i) + int32Size);
                            array[index++] = new sunit_t(base1, check);
                        }
                        break;
                    }
                }
            }
        }

        //Match indexed key which is perfect matched with given string
        public int SearchByPerfectMatch(string key)
        {
            int b = array[0].base1;
            int p;
            for (int index = 0; index < key.Length; index++)
            {
                char ch = key[index];
                p = b + ch + 1;
                if (p >= array.Length)
                {
                    return -1;
                }

                if (b == array[p].check)
                {
                    b = array[p].base1;
                }
                else
                {
                    return -1;
                }
            }

            if (b >= array.Length)
            {
                return -1;
            }

            int n = array[b].base1;
            if (b == array[b].check && n < 0)
            {
                return -n - 1;
            }
            return -1;
        }

        /// <summary>
        /// Match all indexed keys which is prefix of the given string.
        /// </summary>
        public int SearchAsKeyPrefix(string key, List<int> result)
        {
            int len = key.Length;
            int b = array[0].base1;
            int n, p;
            result.Clear();

            for (int i = 0; i < len; i++)
            {
                p = b;
                if (p >= array.Length)
                {
                    return result.Count;
                }

                n = array[p].base1;

                if (b == array[p].check && n < 0)
                {
                    result.Add(-n - 1);
                }

                p = b + (int)key[i] + 1;
                if (p >= array.Length)
                {
                    return result.Count;
                }

                if (b == array[p].check)
                {
                    b = array[p].base1;
                }
                else
                {
                    return result.Count;
                }
            }

            p = b;
            if (p >= array.Length)
            {
                return result.Count;
            }
            n = array[p].base1;

            if (b == array[p].check && n < 0)
            {
                result.Add(-n - 1);
            }

            return result.Count;
        }


        /// <summary>
        /// Search keys by their prefix string
        /// </summary>
        public int SearchByPrefix(string strKeyPrefx, List<int> result)
        {
            int b = array[0].base1;
            int p;

            result.Clear();
            for (int index = 0; index < strKeyPrefx.Length; index++)
            {
                var ch = strKeyPrefx[index];
                p = b + (int)ch + 1;
                if (p >= array.Length)
                {
                    //The given string isn't existed in the DART
                    return -1;
                }

                if (b == array[p].check)
                {
                    b = array[p].base1;
                }
                else
                {
                    return -1;
                }
            }

            Queue<int> queue = new Queue<int>();
            queue.Enqueue(b);

            while (queue.Count > 0)
            {
                b = queue.Dequeue();
                if (b >= array.Length)
                {
                    //Invalidated base, skip it
                    continue;
                }

                if (b == array[b].check && array[b].base1 < 0)
                {
                    result.Add(-array[b].base1 - 1);
                }

                for (int i = 0; i <= 65535; i++)
                {
                    p = b + i + 1;
                    if (p >= array.Length)
                    {
                        //Out of the size of array, skip current search
                        break;
                    }

                    if (b == array[p].check)
                    {
                        queue.Enqueue(array[p].base1);
                    }
                }
            }

            return result.Count;
        }
    }

    public class DoubleArrayTrieBuilder
    {
        private VarBigArray<unit_t> array;
        private VarBigArray<int> used;
        private IList<string> key_;
        private IList<int> val_;
        private int next_chk_pos_;
        private int progress_;
        private int thread_num_;
        private static double MAX_SLOT_USAGE_RATE_THRESHOLD = 0.95;
        private static double MIN_SLOT_USAGE_RATE_THRESHOLD = 0.05;
        private double slot_usage_rate_threshold_ = MAX_SLOT_USAGE_RATE_THRESHOLD;

#if NO_SUPPORT_PARALLEL_LIB
#else
        private ParallelOptions parallelOption;
        private LimitedConcurrencyLevelTaskScheduler lcts;
#endif

        private DateTime startDT;
        private double lastQPS;
        private double lastQPSDelta;

        public class Node
        {
            public int code;
            public int depth;
            public int left;
            public int right;
        };

        public DoubleArrayTrieBuilder(int thread_num)
        {
            array = null;
            thread_num_ = thread_num;
#if NO_SUPPORT_PARALLEL_LIB
#else
            lcts = new LimitedConcurrencyLevelTaskScheduler(thread_num_ * 2);
            parallelOption = new ParallelOptions();
            parallelOption.TaskScheduler = lcts;
#endif
            lastQPS = 0.0;
            lastQPSDelta = 0.0;
        }

        int fetch(Node parent, List<Node> siblings)
        {
            int prev = 0;

            int i = parent.left;
            for (int j = parent.left; j < parent.right; j++)
            {
                string key = key_[j];
                if (key.Length < parent.depth)
                    continue;
                int cur = 0;
                if (key.Length != parent.depth)
                {
                    cur = ((int)key[parent.depth]) + 1;
                }
                if (prev > cur)
                {
                    throw new Exception("Fatal: given strings are not sorted.\n");
                }
                if (cur != prev || siblings.Count == 0)
                {
                    Node tmp_node = new Node();
                    tmp_node.depth = parent.depth + 1;
                    tmp_node.code = cur;
                    tmp_node.left = i;
                    if (siblings.Count != 0)
                        siblings[siblings.Count - 1].right = i;
                    siblings.Add(tmp_node);
                }
                prev = cur;
                i++;
            }
            if (siblings.Count != 0)
                siblings[siblings.Count - 1].right = parent.right;
            return siblings.Count;
        }

        int insert(List<Node> siblings)
        {
            Random rnd = new Random(DateTime.Now.Millisecond + Thread.CurrentThread.ManagedThreadId);
            int begin = 0;
            bool cont = true;
            int nonzeronum = 0;

            while (used[next_chk_pos_] == 1)
            {
                Interlocked.Increment(ref next_chk_pos_);
            }

            int pos = next_chk_pos_;
            int startpos = pos;

            //search begin position
            pos--;
            while (cont == true)
            {
                pos++;
                if (used[pos] == 0)
                {
                    //Check whether slots are available, if not go on to search,
                    cont = false;
                    foreach (Node n in siblings)
                    {
                        if (used[pos + n.code] == 1 || array[pos + n.code] != null)
                        {
                            cont = true;
                            break;
                        }
                    }
                }
                else
                {
                    nonzeronum++;
                }
            }
            begin = pos;

            //check average slot usage rate. If the rate is no less than the threshold, update next_chk_pos_ to 
            //pos whose slot range has much less conflict.
            //note that, the higher rate threshold, the higher slot space usage rate, however, the timing-cost for tri-tree build
            //will also become more higher.
            if ((double)nonzeronum / (double)(pos - startpos + 1) >= slot_usage_rate_threshold_ &&
                pos > next_chk_pos_)
            {
                System.Threading.Interlocked.Exchange(ref next_chk_pos_, pos);
            }

            //double check whether slots are available
            //the reason why double check is because:
            //1. in entire slots space, conflict rate is different. the conflict rate of array's tail
            //   is much lower than that of its header and body
            //2. roll back cost is heavy. So in high conflict rate range, we just check conflict and no other action (first check)
            //   once we find a availabe range without conflict, we try to allocate memory on this range and double check conflict
            bool bAllNull;
            bool bZeroCode = false;
            foreach (Node n in siblings)
            {
                if (n.code == 0)
                {
                    bZeroCode = true;
                    break;
                }
            }

            if (bZeroCode == false)
            {
                Node sNode = new Node();
                sNode.code = 0;
                siblings.Add(sNode);
            }

            do
            {
                bAllNull = true;
                //Test conflict in multi-threads
                int cnt = 0;
                foreach (Node n in siblings)
                {
                    int nBlock = (begin + n.code) >> VarBigArray<int>.moveBit;
                    long offset = (begin + n.code) & (VarBigArray<int>.sizePerBlock - 1);

                    if (used[begin + n.code] == 1 ||
                        System.Threading.Interlocked.CompareExchange(ref used.arrList[nBlock][offset], 1, 0) != 0)
                    {
                        bAllNull = false;
                        foreach (Node revertNode in siblings.GetRange(0, cnt))
                        {
                            used[begin + revertNode.code] = 0;
                        }
                        begin += rnd.Next(thread_num_) + 1;
                        break;
                    }
                    cnt++;
                }
            } while (bAllNull == false);

            if (bZeroCode == false)
            {
                siblings.RemoveAt(siblings.Count - 1);
            }

#if NO_SUPPORT_PARALLEL_LIB
            for (int i = 0;i < siblings.Count;i++)
#else
            Parallel.For(0, siblings.Count, parallelOption, i =>
#endif
            {
                List<Node> new_siblings = new List<Node>();
                Node sibling = siblings[i];
                int offset = begin + sibling.code;

                array[offset] = new unit_t();
                array[offset].check = begin;
                if (fetch(sibling, new_siblings) == 0)
                {
                    array[offset].base1 = -val_[sibling.left] - 1;
                    if (Interlocked.Increment(ref progress_) % 10000 == 0)
                    {
                        //Try to adjust slot usage rate in order to keep high performance
                        TimeSpan ts = DateTime.Now - startDT;
                        double currQPS = progress_ / (ts.TotalSeconds + 1);
                        double currQPSDelta = currQPS - lastQPS;

                        if (currQPS < lastQPS && currQPSDelta < lastQPSDelta)
                        {
                            //Average QPS becomes slow down, need to reduce slot usage rate
                            slot_usage_rate_threshold_ -= 0.1;
                            if (slot_usage_rate_threshold_ < MIN_SLOT_USAGE_RATE_THRESHOLD)
                            {
                                slot_usage_rate_threshold_ = MIN_SLOT_USAGE_RATE_THRESHOLD;
                            }
                        }
                        else
                        {
                            //Average QPS becomes fast, need to add slot usage rate
                            slot_usage_rate_threshold_ += 0.1;
                            if (slot_usage_rate_threshold_ > MAX_SLOT_USAGE_RATE_THRESHOLD)
                            {
                                slot_usage_rate_threshold_ = MAX_SLOT_USAGE_RATE_THRESHOLD;
                            }
                        }

                        lastQPSDelta = currQPSDelta;
                        lastQPS = currQPS;

                        if (progress_ % 100000 == 0)
                        {
                            //Show current progress on console
                            Console.Write("{0}...", progress_);
                        }
                    }
                }
                else
                {
                    int b = insert(new_siblings);
                    array[offset].base1 = b;
                }
            }
#if NO_SUPPORT_PARALLEL_LIB
#else
);
#endif

            return begin;
        }

        void clear()
        {
            array = null;
        }

        public bool build(IDictionary<string, int> keyvalueList, double max_slot_usage_rate_threshold = 0.95)
        {
            FixedBigArray<string> keyList = new FixedBigArray<string>(keyvalueList.Count, 0);
            FixedBigArray<int> valList = new FixedBigArray<int>(keyvalueList.Count, 0);
            long index = 0;
            foreach (KeyValuePair<string, int> pair in keyvalueList)
            {
                keyList[index] = pair.Key;
                valList[index] = pair.Value;
                index++;
            }

            return build(keyList, valList, max_slot_usage_rate_threshold);
        }

        public bool build(IList<string> keyList, IList<int> valList, double max_slot_usage_rate_threshold = 0.95)
        {
            if (keyList == null)
            {
                Console.WriteLine("Key list is empty");
                return false;
            }
            if (valList == null)
            {
                Console.WriteLine("Value list is empty");
                return false;
            }

            if (keyList.Count != valList.Count)
            {
                Console.WriteLine("The size of key list and value list is not equal");
                return false;
            }

            for (int i = 0; i < valList.Count; i++)
            {
                if (valList[i] <= -1)
                {
                    Console.WriteLine("Invalidated value {0} at index {1}", valList[i], i);
                    return false;
                }
            }

            MAX_SLOT_USAGE_RATE_THRESHOLD = max_slot_usage_rate_threshold;
            slot_usage_rate_threshold_ = max_slot_usage_rate_threshold;
            progress_ = 0;
            key_ = keyList;
            val_ = valList;

            startDT = DateTime.Now;
            array = new VarBigArray<unit_t>(key_.Count * 5);
            used = new VarBigArray<int>(key_.Count * 5);
            array[0] = new unit_t();
            array[0].base1 = 1;
            used[0] = 1;
            next_chk_pos_ = 0;
            Node root_node = new Node();
            root_node.left = 0;
            root_node.right = key_.Count;
            root_node.depth = 0;
            List<Node> siblings = new List<Node>();
            fetch(root_node, siblings);
            insert(siblings);

            return true;
        }

        public void save(string file)
        {
            StreamWriter sw = new StreamWriter(file);
            BinaryWriter bw = new BinaryWriter(sw.BaseStream);

            long r_length = array.LongLength;
            while (array[r_length - 1] == null)
            {
                r_length--;
            }

            for (long i = 0; i < r_length; i++)
            {
                if (array[i] == null)
                {
                    bw.Write(0);
                    bw.Write(0);
                }
                else
                {
                    bw.Write(array[i].base1);
                    bw.Write(array[i].check);
                }
            }
            bw.Close();
        }
    }
}
