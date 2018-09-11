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
    /// <summary>
    /// Encapsulates a node in a Problem vector, with an index and a value (for more efficient representation
    /// of sparse data.
    /// </summary>
	[Serializable]
	public class Node : IComparable<Node>
	{
        internal int _index;
        internal double _value;

        /// <summary>
        /// Default Constructor.
        /// </summary>
        public Node()
        {
        }
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="index">The index of the value.</param>
        /// <param name="value">The value to store.</param>
        public Node(int index, double value)
        {
            _index = index;
            _value = value;
        }

        /// <summary>
        /// Index of this Node.
        /// </summary>
        public int Index
        {
            get
            {
                return _index;
            }
            set
            {
                _index = value;
            }
        }
        /// <summary>
        /// Value at Index.
        /// </summary>
        public double Value
        {
            get
            {
                return _value;
            }
            set
            {
                _value = value;
            }
        }

        /// <summary>
        /// String representation of this Node as {index}:{value}.
        /// </summary>
        /// <returns>{index}:{value}</returns>
        public override string ToString()
        {
            return string.Format("{0}:{1}", _index, _value.Truncate());
        }

        public override bool Equals(object obj)
        {
            Node other = obj as Node;
            if(other == null)
                return false;

            return _index == other._index && _value.Truncate() == other._value.Truncate();
        }

        public override int GetHashCode()
        {
            return _index.GetHashCode() + _value.GetHashCode();
        }

        #region IComparable<Node> Members

        /// <summary>
        /// Compares this node with another.
        /// </summary>
        /// <param name="other">The node to compare to</param>
        /// <returns>A positive number if this node is greater, a negative number if it is less than, or 0 if equal</returns>
        public int CompareTo(Node other)
        {
            return _index.CompareTo(other._index);
        }

        #endregion
    }
}