using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotSharp.Models.CRFLite.Utils
{
    /// <summary>
    /// Represents a generic interface of an ordered collection.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    public interface ISortedCollection<T> : ICollection<T>
    {
        /// <summary>
        /// Gets the comparer used to order items in the collection.
        /// </summary>
        IComparer<T> Comparer
        {
            get;
        }

        /// <summary>
        /// Get all items equal to or greater than the specified value, starting with the lowest index and moving forwards.
        /// </summary>
        IEnumerable<T> WhereGreaterOrEqual(T value);

        /// <summary>
        /// Get all items less than or equal to the specified value, starting with the highest index and moving backwards.
        /// </summary>
        IEnumerable<T> WhereLessOrEqualBackwards(T value);

        /// <summary>
        /// Gets the index of the first item greater than the specified value.
        /// /// </summary>
        int FirstIndexWhereGreaterThan(T value);

        /// <summary>
        /// Gets the index of the last item less than the specified key.
        /// </summary>
        int LastIndexWhereLessThan(T value);

        /// <summary>
        /// Gets the item at the specified index.
        /// </summary>
        T At(int index);

        /// <summary>
        /// Removes the item at the specified index.
        /// </summary>
        void RemoveAt(int index);

        /// <summary>
        /// Get all items starting at the index, and moving forward.
        /// </summary>
        IEnumerable<T> ForwardFromIndex(int index);

        /// <summary>
        /// Get all items starting at the index, and moving backward.
        /// </summary>
        IEnumerable<T> BackwardFromIndex(int index);
    }

    /// <summary>
    /// Represents a generic interface of ordered key/value pairs.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    public interface ISortedDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        /// <summary>
        /// Get all items having a key equal to or greater than the specified key, starting with the lowest index and moving forwards.
        /// </summary>
        IEnumerable<KeyValuePair<TKey, TValue>> WhereGreaterOrEqual(TKey key);

        /// <summary>
        /// Get all items less than or equal to the specified value, starting with the highest index and moving backwards.
        /// </summary>
        IEnumerable<KeyValuePair<TKey, TValue>> WhereLessOrEqualBackwards(TKey keyUpperBound);

        /// <summary>
        /// Gets the sorted collection of keys.
        /// </summary>
        new ISortedCollection<TKey> Keys
        {
            get;
        }

        /// <summary>
        /// Gets the item at the specified index.
        /// </summary>
        KeyValuePair<TKey, TValue> At(int index);

        /// <summary>
        /// Removes the item at the specified index.
        /// </summary>
        void RemoveAt(int index);

        /// <summary>
        /// Sets the value at the specified index.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="value"></param>
        void SetValueAt(int index, TValue value);

        /// <summary>
        /// Get all items starting at the index, and moving forward.
        /// </summary>
        IEnumerable<KeyValuePair<TKey, TValue>> ForwardFromIndex(int index);

        /// <summary>
        /// Get all items starting at the index, and moving backward.
        /// </summary>
        IEnumerable<KeyValuePair<TKey, TValue>> BackwardFromIndex(int index);
    }

    /// <summary>
    /// An O(log N) implementation of the ISortedDictionary interface.
    /// </summary>
    /// <typeparam name="TKey">The type for the sorted keys.</typeparam>
    /// <typeparam name="TValue">The type for the associated values.</typeparam>
    public class BTreeDictionary<TKey, TValue> : ISortedDictionary<TKey, TValue>
    {
        #region Fields

        Node root;
        readonly Node first;
        readonly KeyCollection keys;
        readonly ValueCollection values;
        readonly IComparer<TKey> keyComparer;

        private void ObjectInvariant()
        {

        }

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new BTreeDictionary instance optimized for the specified node capacity.
        /// </summary>
        /// <param name="nodeCapacity">The capacity in keys for each node in the tree structure.</param>
        public BTreeDictionary(int nodeCapacity = 128)
            : this(Comparer<TKey>.Default, nodeCapacity)
        {

        }

        /// <summary>
        /// Initializes a new BTreeDictionary instance.
        /// </summary>
        /// <param name="keyComparer">The comparer for ordering keys in the structure.</param>
        /// <param name="nodeCapacity">The capacity in keys for each node in the tree structure.</param>
        public BTreeDictionary(IComparer<TKey> keyComparer, int nodeCapacity)
        {


            this.keyComparer = keyComparer;
            this.first = new Node(nodeCapacity);
            this.root = this.first;

            this.keys = new KeyCollection(this);
            this.values = new ValueCollection(this);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the value associated with the specified key.  An arbitrary value will be chosen if key is a duplicate.
        /// </summary>
        /// <param name="key">The key for which to retrieve the associated value.</param>
        /// <returns>The value associated with the key.</returns>
        public TValue this[TKey key]
        {
            get
            {
                TValue result;
                if (this.TryGetValue(key, out result))
                    return result;
                throw new InvalidOperationException("error");
            }
            set
            {

                Node leaf;
                int pos;
                if (Node.Find(root, key, KeyComparer, 0, out leaf, out pos))
                    leaf.SetValue(pos, value);
                else
                {
                    Node.Insert(key, ref leaf, ref pos, ref root);
                    leaf.SetValue(pos, value);
                }
            }
        }

        /// <summary>
        /// Gets the number of key value pairs in the dictionary.
        /// </summary>
        public int Count
        {
            get
            {

                return this.root.TotalCount;
            }
        }

        /// <summary>
        /// Gets the key comparer.
        /// </summary>
        public IComparer<TKey> KeyComparer
        {
            get
            {

                return this.keyComparer;
            }
        }

        /// <summary>
        /// Gets the collection of keys in the dictionary.
        /// </summary>
        public ISortedCollection<TKey> Keys
        {
            get
            {

                return this.keys;
            }
        }

        public IList<TKey> KeyList
        {
            get
            {
                return this.keys;
            }
        }

        /// <summary>
        /// Gets the collection of values in the dictionary.
        /// </summary>
        public ICollection<TValue> Values
        {
            get
            {

                return this.values;
            }
        }

        public IList<TValue> ValueList
        {
            get
            {
                return this.values;
            }
        }

        /// <summary>
        /// Gets or sets indication whether this dictionary is readonly or mutable.
        /// </summary>
        public bool IsReadOnly
        {
            get;
            set;
        }


        #endregion

        #region Methods

        /// <summary>
        /// Gets indication of whether the dictionary contains an entry for the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>True if the dictionary contains the key; otherwise, false.</returns>
        public bool ContainsKey(TKey key)
        {
            Node leaf;
            int pos;
            var found = Node.Find(root, key, KeyComparer, 0, out leaf, out pos);
            return found;
        }

        /// <summary>
        /// Tries to get the value for the specified key.  An arbitrary value will be chosen if key is a duplicate.
        /// </summary>
        /// <param name="key">The key for which to try to get the value.</param>
        /// <param name="value">The value found for the specified key, or a default value if not found.</param>
        /// <returns>True if the value was found; otherwise, false.</returns>
        public bool TryGetValue(TKey key, out TValue value)
        {
            try
            {
                Node leaf;
                int pos;
                var found = Node.Find(root, key, KeyComparer, 0, out leaf, out pos);
                value = found ? leaf.GetValue(pos) : default(TValue);
                return found;
            }
            catch (System.Exception)
            {
                value = default(TValue);
                return false;
            }

        }

        /// <summary>
        /// Adds the specified key and value to the dictionary.
        /// </summary>
        /// <param name="key">The key to add.</param>
        /// <param name="value">The value to associate with the key.</param>
        public void Add(TKey key, TValue value)
        {

            Node leaf;
            int pos;
            var found = Node.Find(root, key, KeyComparer, 0, out leaf, out pos);
            if (found)
            {
                //The key is already in the dictionary, throw exception out
                throw new InvalidOperationException("The key is already in the dictionary");
            }

            Node.Insert(key, ref leaf, ref pos, ref root);
            leaf.SetValue(pos, value);
        }

        /// <summary>
        /// Gets the key value pair at the specified index.
        /// </summary>
        /// <param name="index">The index at which to get the key value pair.</param>
        /// <returns>The key value pair at the specified index.</returns>
        public KeyValuePair<TKey, TValue> At(int index)
        {

            var leaf = Node.LeafAt(root, ref index);
            return new KeyValuePair<TKey, TValue>(leaf.GetKey(index), leaf.GetValue(index));
        }

        /// <summary>
        /// Clears the dictionary of all items.
        /// </summary>
        public void Clear()
        {

            Node.Clear(first);
            root = first;
        }

        /// <summary>
        /// Remove the key and associated value from the dictionary.
        /// </summary>
        /// <param name="key">The key to remove.</param>
        /// <returns>True if the key was removed; otherwise, false if key was not found.</returns>
        public bool Remove(TKey key)
        {

            Node leaf;
            int pos;
            if (!Node.Find(root, key, KeyComparer, 0, out leaf, out pos))
                return false;

            Node.Remove(leaf, pos, ref root);
            return true;
        }

        /// <summary>
        /// Removes the key and associated value from the dictionary at the specified index.
        /// </summary>
        /// <param name="index">The index at which to remove the key value pair.</param>
        public void RemoveAt(int index)
        {

            var leaf = Node.LeafAt(root, ref index);
            Node.Remove(leaf, index, ref root);
        }


        /// <summary>
        /// Get all items starting at the index, and moving forward.
        /// </summary>
        public IEnumerable<KeyValuePair<TKey, TValue>> ForwardFromIndex(int index)
        {

            var node = Node.LeafAt(root, ref index);
            return Node.ForwardFromIndex(node, index);
        }

        /// <summary>
        /// Get all items starting at the index, and moving backward.
        /// </summary>
        public IEnumerable<KeyValuePair<TKey, TValue>> BackwardFromIndex(int index)
        {

            var node = Node.LeafAt(root, ref index);
            return Node.BackwardFromIndex(node, index);
        }

        /// <summary>
        /// Sets the value at the specified index, leaving the key unchanged.
        /// </summary>
        /// <param name="index">The index at which to set the value.</param>
        /// <param name="value">The value to associate at the specified index.</param>
        public void SetValueAt(int index, TValue value)
        {

            var leaf = Node.LeafAt(root, ref index);
            leaf.SetValue(index, value);
        }

        /// <summary>
        /// Gets an enumerator of key value pairs for the entire collection in sorted ascending order.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {

            return Node.ForwardFromIndex(first, 0).GetEnumerator();
        }

        /// <summary>
        /// Gets an enumerator of key value pairs in ascending order by key, for all items with a key 
        /// equal or greater than the specified key lower bound.
        /// </summary>
        /// <param name="keyLowerBound">The value at which to start returning keys.</param>
        /// <returns>All key value pairs having a key equal or greater than the lower bound, in key ascending order.</returns>
        public IEnumerable<KeyValuePair<TKey, TValue>> WhereGreaterOrEqual(TKey keyLowerBound)
        {

            Node leaf;
            int leafPos;
            Node.Find(root, keyLowerBound, KeyComparer, 0, out leaf, out leafPos);
            return Node.ForwardFromIndex(leaf, leafPos);
        }

        /// <summary>
        /// Gets an enumerator of key value pairs in descending order by key, for all items with a key
        /// less than or equal than the specified key upper bound.
        /// </summary>
        /// <param name="keyUpperBound">The value at which to start returning keys.</param>
        /// <returns>All key value pairs having a key equal or less than the upper bound, in key descending order.</returns>
        public IEnumerable<KeyValuePair<TKey, TValue>> WhereLessOrEqualBackwards(TKey keyUpperBound)
        {

            Node leaf;
            int leafPos;
            var found = Node.Find(root, keyUpperBound, KeyComparer, 0, out leaf, out leafPos);
            if (!found)
                --leafPos;
            return Node.BackwardFromIndex(leaf, leafPos);
        }

        /// <summary>
        /// Copy the entire dictionary to the specified array, starting at the specified array index.
        /// </summary>
        /// <param name="array">The array into which to copy.</param>
        /// <param name="arrayIndex">The index at which to start copying.</param>
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            foreach (var item in this)
                array[arrayIndex++] = item;
        }

        #endregion

        #region Implementation - Nested Types

        sealed class Node
        {
            #region Fields

            readonly TKey[] keys;
            readonly TValue[] values;
            readonly Node[] nodes;

            int nodeCount;
            int totalCount;

            Node parent;
            Node next;
            Node prev;
            private void ObjectInvariant()
            {

            }

            #endregion

            #region Construction

            /// <summary>
            /// Initialize the first node in the BTree structure.
            /// </summary>
            public Node(int nodeCapacity)
                : this(nodeCapacity, true)
            {
            }

            #endregion

            #region Properties

            public int TotalCount
            {
                get
                {
                    return this.totalCount;
                }
            }

            public bool IsRoot
            {
                get
                {
                    return this.parent == null;
                }
            }

            public bool IsLeaf
            {
                get
                {
                    return nodes == null;
                }
            }

            public int NodeCount
            {
                get
                {
                    return this.nodeCount;
                }
            }

            #endregion

            #region Methods

            /// <summary>
            /// Gets the key at the specified position.
            /// </summary>
            public TKey GetKey(int pos)
            {
                return this.keys[pos];
            }

            /// <summary>
            /// Gets the value at the specified position.
            /// </summary>
            public TValue GetValue(int pos)
            {
                return this.values[pos];
            }

            /// <summary>
            /// Sets the value at the specified position.
            /// </summary>
            public void SetValue(int pos, TValue value)
            {
                this.values[pos] = value;
            }

            /// <summary>
            /// Get the leaf node at the specified index in the tree defined by the specified root.
            /// </summary>
            public static Node LeafAt(Node root, ref int pos)
            {

                int nodeIndex = 0;
                while (true)
                {
                    if (root.nodes == null)
                    {
                        return root;
                    }

                    var node = root.nodes[nodeIndex];
                    if (pos < node.totalCount)
                    {
                        root = node;
                        nodeIndex = 0;
                    }
                    else
                    {
                        pos -= node.totalCount;
                        ++nodeIndex;
                    }
                }
            }

            /// <summary>
            /// Find the node and index in the tree defined by the specified root.
            /// </summary>
            public static bool Find(Node root, TKey key, IComparer<TKey> keyComparer, int duplicatesBias, out Node leaf, out int pos)
            {

                pos = Array.BinarySearch(root.keys, 0, root.nodeCount, key, keyComparer);
                while (root.nodes != null)
                {
                    if (pos >= 0)
                    {
                        if (duplicatesBias != 0)
                            MoveToDuplicatesBoundary(key, keyComparer, duplicatesBias, ref root, ref pos);

                        // Found an exact match.  Move down one level.
                        root = root.nodes[pos];
                    }
                    else
                    {
                        // No exact match.  Find greatest lower bound.
                        pos = ~pos;
                        if (pos > 0)
                            --pos;
                        root = root.nodes[pos];
                    }

                    pos = Array.BinarySearch(root.keys, 0, root.nodeCount, key, keyComparer);
                }

                leaf = root;
                if (pos < 0)
                {
                    pos = ~pos;
                    return false;
                }

                if (duplicatesBias != 0)
                    MoveToDuplicatesBoundary(key, keyComparer, duplicatesBias, ref leaf, ref pos);

                return true;
            }

            /// <summary>
            /// Insert a new key into the leaf node at the specified position.
            /// </summary>
            public static void Insert(TKey key, ref Node leaf, ref int pos, ref Node root)
            {
                // Make sure there is space for the new key.
                if (EnsureSpace(leaf, ref root) && pos > leaf.nodeCount)
                {
                    pos -= leaf.nodeCount;
                    leaf = leaf.next;
                }

                // Insert the key.
                int moveCount = leaf.nodeCount - pos;
                Array.Copy(leaf.keys, pos, leaf.keys, pos + 1, moveCount);
                leaf.keys[pos] = key;
                ++leaf.nodeCount;
                EnsureParentKey(leaf, pos);

                // Insert space for the value.  Caller is responsible for filling in value.
                Array.Copy(leaf.values, pos, leaf.values, pos + 1, moveCount);

                // Update total counts.
                for (var node = leaf; node != null; node = node.parent)
                    ++node.totalCount;
            }

            /// <summary>
            /// Remove the item from the node at the specified position.
            /// </summary>
            public static bool Remove(Node leaf, int pos, ref Node root)
            {

                // Update total counts.
                for (var node = leaf; node != null; node = node.parent)
                    --node.totalCount;

                // Remove the key and value from the node.
                --leaf.nodeCount;
                Array.Copy(leaf.keys, pos + 1, leaf.keys, pos, leaf.nodeCount - pos);
                Array.Copy(leaf.values, pos + 1, leaf.values, pos, leaf.nodeCount - pos);
                leaf.keys[leaf.nodeCount] = default(TKey);
                leaf.values[leaf.nodeCount] = default(TValue);

                // Make sure parent keys index correctly into this node.
                if (leaf.nodeCount > 0)
                    EnsureParentKey(leaf, pos);

                // Merge this node with others if it is below the node capacity threshold.
                Merge(leaf, ref root);
                return true;
            }

            /// <summary>
            /// Get an ascending enumerable for the collection, starting an the index in the specified leaf node.
            /// </summary>
            public static IEnumerable<KeyValuePair<TKey, TValue>> ForwardFromIndex(Node leaf, int pos)
            {

                while (leaf != null)
                {
                    while (pos < leaf.nodeCount)
                    {
                        yield return new KeyValuePair<TKey, TValue>(leaf.GetKey(pos), leaf.GetValue(pos));
                        ++pos;
                    }
                    pos -= leaf.nodeCount;
                    leaf = leaf.next;
                }
            }

            /// <summary>
            /// Get a descending enumerable, starting at the index in the specified leaf node. 
            /// </summary>
            public static IEnumerable<KeyValuePair<TKey, TValue>> BackwardFromIndex(Node leaf, int pos)
            {

                if (pos == -1)
                {
                    // Handle special case to start moving in the previous node.
                    leaf = leaf.prev;
                    if (leaf != null)
                        pos = leaf.nodeCount - 1;
                    else
                        pos = 0;
                }
                else if (pos == leaf.NodeCount)
                {
                    // Handle special case to start moving in the next node.
                    if (leaf.next == null)
                        --pos;
                    else
                    {
                        leaf = leaf.next;
                        pos = 0;
                    }
                }

                // Loop thru collection, yielding each value in sequence.
                while (leaf != null)
                {
                    while (pos >= 0)
                    {
                        yield return new KeyValuePair<TKey, TValue>(leaf.GetKey(pos), leaf.GetValue(pos));
                        --pos;
                    }
                    leaf = leaf.prev;
                    if (leaf != null)
                        pos += leaf.nodeCount;
                }
            }

            /// <summary>
            /// Clear all keys and values from the specified node.
            /// </summary>
            public static void Clear(Node firstNode)
            {

                int clearCount = firstNode.nodeCount;

                Array.Clear(firstNode.keys, 0, clearCount);
                Array.Clear(firstNode.values, 0, clearCount);
                firstNode.nodeCount = 0;
                firstNode.totalCount = 0;

                firstNode.parent = null;
                firstNode.next = null;
            }

            /// <summary>
            /// Get the index relative to the root node, for the position in the specified leaf.
            /// </summary>
            public static int GetRootIndex(Node leaf, int pos)
            {
                var node = leaf;
                var rootIndex = pos;
                while (node.parent != null)
                {
                    int nodePos = Array.IndexOf(node.parent.nodes, node, 0, node.parent.nodeCount);
                    for (int i = 0; i < nodePos; ++i)
                        rootIndex += node.parent.nodes[i].totalCount;
                    node = node.parent;
                }
                return rootIndex;
            }

            #endregion

            #region Implementation

            Node(int nodeCapacity, bool leaf)
            {
                this.keys = new TKey[nodeCapacity];

                if (leaf)
                {
                    this.values = new TValue[nodeCapacity];
                    this.nodes = null;
                }
                else
                {
                    this.values = null;
                    this.nodes = new Node[nodeCapacity];
                }

                this.nodeCount = 0;
                this.totalCount = 0;
                this.parent = null;
                this.next = null;
                this.prev = null;
            }

            /// <summary>
            /// (Assumes: key is a duplicate in node at pos) Move to the side on the range of duplicates,
            /// as indicated by the sign of duplicatesBias.
            /// </summary>
            /// <param name="key"></param>
            /// <param name="keyComparer"></param>
            /// <param name="duplicatesBias"></param>
            /// <param name="node"></param>
            /// <param name="pos"></param>
            static void MoveToDuplicatesBoundary(TKey key, IComparer<TKey> keyComparer, int duplicatesBias, ref Node node, ref int pos)
            {
                // Technically, we could adjust the binary search to perform most of this step, but duplicates
                // are usually unexpected.. algorithm is still O(log N), because scan include at most a scan thru two nodes
                // worth of keys, for each level.
                // Also, the binary search option would still need the ugliness of the special case for moving into the 
                // previous node; it would only be a little faster, on average, assuming large numbers of duplicates were common.

                if (duplicatesBias < 0)
                {
                    // Move backward over duplicates.
                    while (pos > 0 && 0 == keyComparer.Compare(node.keys[pos - 1], key))
                        --pos;

                    // Special case: duplicates can span backwards into the previous node because the parent
                    // key pivot might be in the center for the duplicates.
                    if (pos == 0 && node.prev != null)
                    {
                        var prev = node.prev;
                        var prevPos = prev.NodeCount;
                        while (prevPos > 0 && 0 == keyComparer.Compare(prev.keys[prevPos - 1], key))
                        {
                            --prevPos;
                        }
                        if (prevPos < prev.NodeCount)
                        {
                            node = prev;
                            pos = prevPos;
                        }
                    }
                }
                else
                {
                    // Move forward over duplicates.
                    while (pos < node.NodeCount - 1 && 0 == keyComparer.Compare(node.keys[pos + 1], key))
                        ++pos;
                }
            }

            static bool EnsureSpace(Node node, ref Node root)
            {
                if (node.nodeCount < node.keys.Length)
                    return false;

                EnsureParent(node, ref root);
                EnsureSpace(node.parent, ref root);

                var sibling = new Node(node.keys.Length, node.nodes == null);
                sibling.next = node.next;
                sibling.prev = node;
                sibling.parent = node.parent;

                if (node.next != null)
                    node.next.prev = sibling;
                node.next = sibling;

                int pos = Array.IndexOf(node.parent.nodes, node, 0, node.parent.nodeCount);
                int siblingPos = pos + 1;

                Array.Copy(node.parent.keys, siblingPos, node.parent.keys, siblingPos + 1, node.parent.nodeCount - siblingPos);
                Array.Copy(node.parent.nodes, siblingPos, node.parent.nodes, siblingPos + 1, node.parent.nodeCount - siblingPos);
                ++node.parent.nodeCount;
                node.parent.nodes[siblingPos] = sibling;

                int half = node.nodeCount / 2;
                int halfCount = node.nodeCount - half;
                Move(node, half, sibling, 0, halfCount);
                return true;
            }

            static void Move(Node source, int sourceIndex, Node target, int targetIndex, int moveCount)
            {
                Move(source.keys, sourceIndex, source.nodeCount, target.keys, targetIndex, target.nodeCount, moveCount);
                if (source.values != null)
                    Move(source.values, sourceIndex, source.nodeCount, target.values, targetIndex, target.nodeCount, moveCount);

                int totalMoveCount;
                if (source.nodes == null)
                {
                    totalMoveCount = moveCount;
                }
                else
                {
                    Move(source.nodes, sourceIndex, source.nodeCount, target.nodes, targetIndex, target.nodeCount, moveCount);
                    totalMoveCount = 0;
                    for (int i = 0; i < moveCount; ++i)
                    {
                        var child = target.nodes[targetIndex + i];
                        child.parent = target;
                        totalMoveCount += child.totalCount;
                    }
                }

                source.nodeCount -= moveCount;
                target.nodeCount += moveCount;

                var sn = source;
                var tn = target;
                while (sn != null && sn != tn)
                {
                    sn.totalCount -= totalMoveCount;
                    tn.totalCount += totalMoveCount;
                    sn = sn.parent;
                    tn = tn.parent;
                }

                EnsureParentKey(source, sourceIndex);
                EnsureParentKey(target, targetIndex);
            }

            static void Move<TItem>(TItem[] source, int sourceIndex, int sourceTotal, TItem[] target, int targetIndex, int targetTotal, int count)
            {
                Array.Copy(target, targetIndex, target, targetIndex + count, targetTotal - targetIndex);
                Array.Copy(source, sourceIndex, target, targetIndex, count);
                Array.Copy(source, sourceIndex + count, source, sourceIndex, sourceTotal - sourceIndex - count);
                Array.Clear(source, sourceTotal - count, count);
            }

            static void EnsureParent(Node node, ref Node root)
            {
                if (node.parent != null)
                    return;

                var parent = new Node(node.keys.Length, false);
                parent.totalCount = node.totalCount;
                parent.nodeCount = 1;
                parent.keys[0] = node.keys[0];
                parent.nodes[0] = node;

                node.parent = parent;
                root = parent;
            }

            static void EnsureParentKey(Node node, int pos)
            {
                while (pos == 0 && node.parent != null)
                {
                    pos = Array.IndexOf(node.parent.nodes, node, 0, node.parent.nodeCount);
                    node.parent.keys[pos] = node.keys[0];
                    node = node.parent;
                }
            }

            static void Merge(Node node, ref Node root)
            {
                if (node.nodeCount == 0)
                {
                    // Handle special case: Empty node.
                    if (node.parent == null)
                        return;

                    // Remove the node from the parent nodes.
                    int pos = Array.IndexOf(node.parent.nodes, node, 0, node.parent.nodeCount);
                    --node.parent.nodeCount;
                    Array.Copy(node.parent.keys, pos + 1, node.parent.keys, pos, node.parent.nodeCount - pos);
                    Array.Copy(node.parent.nodes, pos + 1, node.parent.nodes, pos, node.parent.nodeCount - pos);
                    node.parent.keys[node.parent.nodeCount] = default(TKey);
                    node.parent.nodes[node.parent.nodeCount] = null;

                    // Make sure parent (of the parent) keys link down correctly.
                    if (node.parent.nodeCount > 0)
                        EnsureParentKey(node.parent, pos);

                    // Delete the node from the next/prev linked list.
                    node.prev.next = node.next;
                    if (node.next != null)
                        node.next.prev = node.prev;

                    // Merge the parent node.
                    Merge(node.parent, ref root);
                    return;
                }

                if (node.next == null)
                {
                    if (node.parent == null && node.nodeCount == 1 && node.nodes != null)
                    {
                        root = node.nodes[0];
                        root.parent = null;
                    }

                    return;
                }

                if (node.nodeCount >= node.keys.Length / 2)
                    return;

                int count = node.next.nodeCount;
                if (node.nodeCount + count > node.keys.Length)
                    count -= (node.nodeCount + count) / 2;

                Move(node.next, 0, node, node.nodeCount, count);
                Merge(node.next, ref root);
            }

            #endregion
        }

        abstract class KeyValueCollectionBase<T> : ICollection<T>
        {
            #region Fields

            protected readonly BTreeDictionary<TKey, TValue> tree;

            #endregion

            #region Construction

            public KeyValueCollectionBase(BTreeDictionary<TKey, TValue> tree)
            {
                this.tree = tree;
            }

            #endregion

            #region Properties

            public int Count
            {
                get
                {
                    return tree.Count;
                }
            }

            #endregion

            #region Methods

            public abstract bool Contains(T item);

            public void CopyTo(T[] array, int arrayIndex)
            {
                foreach (var item in this)
                    array[arrayIndex++] = item;
            }

            public abstract IEnumerator<T> GetEnumerator();

            #endregion

            #region ICollection<> members

            void ICollection<T>.Add(T item)
            {
                throw new NotSupportedException();
            }

            void ICollection<T>.Clear()
            {
                throw new NotSupportedException();
            }

            bool ICollection<T>.IsReadOnly
            {
                get
                {
                    return true;
                }
            }

            public bool Remove(T item)
            {
                throw new NotSupportedException();
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }

            #endregion
        }

        sealed class ValueCollection : KeyValueCollectionBase<TValue>, IList<TValue>
        {
            #region Construction

            public ValueCollection(BTreeDictionary<TKey, TValue> tree)
                : base(tree)
            {
            }

            #endregion

            #region Methods

            public override bool Contains(TValue item)
            {
                return this.tree.Any(keyValue => object.Equals(item, keyValue.Value));
            }

            public override IEnumerator<TValue> GetEnumerator()
            {
                return this.tree.Select(keyValue => keyValue.Value).GetEnumerator();
            }

            #endregion

            int IList<TValue>.IndexOf(TValue item)
            {
                throw new NotImplementedException();
            }

            void IList<TValue>.Insert(int index, TValue item)
            {
                throw new NotImplementedException();
            }

            void IList<TValue>.RemoveAt(int index)
            {
                throw new NotImplementedException();
            }

            public TValue this[int index]
            {
                get
                {
                    return this.tree.At(index).Value;
                }
                set
                {
                    throw new NotImplementedException();
                }
            }
        }

        sealed class KeyCollection : KeyValueCollectionBase<TKey>, ISortedCollection<TKey>, IList<TKey>
        {
            #region Construction

            public KeyCollection(BTreeDictionary<TKey, TValue> tree)
                : base(tree)
            {
            }

            #endregion

            #region Properties

            public IComparer<TKey> Comparer
            {
                get
                {
                    return tree.KeyComparer;
                }
            }

            #endregion

            #region Methods

            public int FirstIndexWhereGreaterThan(TKey value)
            {
                Node leaf;
                int pos;
                var found = Node.Find(tree.root, value, tree.KeyComparer, 0, out leaf, out pos);
                int result = Node.GetRootIndex(leaf, pos);
                if (found)
                    ++result;
                return result;
            }

            public int LastIndexWhereLessThan(TKey value)
            {
                Node leaf;
                int pos;
                var found = Node.Find(tree.root, value, tree.KeyComparer, 0, out leaf, out pos);
                int result = Node.GetRootIndex(leaf, pos);
                if (found)
                    --result;
                return result;
            }

            public TKey At(int index)
            {
                return this.tree.At(index).Key;
            }

            public override bool Contains(TKey item)
            {
                return tree.ContainsKey(item);
            }

            public override IEnumerator<TKey> GetEnumerator()
            {
                return tree.Select(keyValue => keyValue.Key).GetEnumerator();
            }

            public IEnumerable<TKey> WhereGreaterOrEqual(TKey lowerBound)
            {
                return tree.WhereGreaterOrEqual(lowerBound).Select(keyValue => keyValue.Key);
            }

            public IEnumerable<TKey> WhereLessOrEqualBackwards(TKey upperBound)
            {
                return tree.WhereLessOrEqualBackwards(upperBound).Select(keyValue => keyValue.Key);
            }

            public IEnumerable<TKey> ForwardFromIndex(int index)
            {
                return this.tree.ForwardFromIndex(index).Select(item => item.Key);
            }

            public IEnumerable<TKey> BackwardFromIndex(int index)
            {
                return this.tree.BackwardFromIndex(index).Select(item => item.Key);
            }

            #endregion

            #region IEnumerable members

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }

            #endregion

            #region ISortedCollection<> members

            void ISortedCollection<TKey>.RemoveAt(int index)
            {
                throw new NotSupportedException();
            }

            #endregion

            int IList<TKey>.IndexOf(TKey item)
            {
                throw new NotImplementedException();
            }

            void IList<TKey>.Insert(int index, TKey item)
            {
                throw new NotImplementedException();
            }

            void IList<TKey>.RemoveAt(int index)
            {
                throw new NotImplementedException();
            }

            public TKey this[int index]
            {
                get
                {
                    return this.tree.At(index).Key;
                }
                set
                {
                    throw new NotImplementedException();
                }
            }

        }

        #endregion

        #region IDictionary<> members

        ICollection<TKey> IDictionary<TKey, TValue>.Keys
        {
            get
            {
                return this.Keys;
            }
        }

        #endregion

        #region ICollection<> members

        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
        {
            this.Add(item.Key, item.Value);
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
        {
            TValue value;
            return this.TryGetValue(item.Key, out value) && object.Equals(item.Value, value);
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly
        {
            get
            {
                return false;
            }
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
        {
            TValue value;
            if (this.TryGetValue(item.Key, out value) && object.Equals(item.Value, value))
            {
                this.Remove(item.Key);
                return true;
            }
            return false;
        }

        #endregion

        #region IEnumerable members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        #endregion
    }
}
