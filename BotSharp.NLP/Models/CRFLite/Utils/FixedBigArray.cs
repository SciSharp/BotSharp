using System;
using System.Collections.Generic;
using System.Threading;
using System.Text;

namespace BotSharp.Models.CRFLite.Utils
{
    public sealed class FixedBigArray<T> : BigArray<T> where T : IComparable<T>
    {
        public int lowBounding_;

        public override T this[long i]
        {
            get
            {
                long offset = (i - lowBounding_);
                int nBlock = (int)(offset >> moveBit);
                return arrList[nBlock][offset & (sizePerBlock - 1)];
            }
            set
            {
                long offset = (i - lowBounding_);
                int nBlock = (int)(offset >> moveBit);
                arrList[nBlock][offset & (sizePerBlock - 1)] = value;
            }
        }

        

        //construct big array
        //size is array's default length
        //lowBounding is the lowest bounding of the array
        public FixedBigArray(long size, int lowBounding)
        {
            size_ = size;
            lowBounding_ = lowBounding;
            arrList = new List<T[]>();

            for (long i = 0; i < size_; i += sizePerBlock)
            {
                if (i + sizePerBlock < size_)
                {
                    arrList.Add(new T[sizePerBlock]);
                }
                else
                {
                    arrList.Add(new T[size_ - i]);
                }
            }
        }

        
    }
}
