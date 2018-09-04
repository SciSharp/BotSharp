using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SVM.BotSharp.MachineLearning
{
    internal interface IQMatrix
    {
        float[] GetQ(int column, int len);
        double[] GetQD();
        void SwapIndex(int i, int j);
    }

    internal abstract class Kernel : IQMatrix
    {
        private Node[][] _x;
        private double[] _xSquare;

        private KernelType _kernelType;
        private int _degree;
        private double _gamma;
        private double _coef0;

        public abstract float[] GetQ(int column, int len);
        public abstract double[] GetQD();

        public virtual void SwapIndex(int i, int j)
        {
            _x.SwapIndex(i, j);

            if (_xSquare != null)
            {
                _xSquare.SwapIndex(i, j);
            }
        }

        private static double powi(double value, int times)
        {
            double tmp = value, ret = 1.0;

            for (int t = times; t > 0; t /= 2)
            {
                if (t % 2 == 1) ret *= tmp;
                tmp = tmp * tmp;
            }
            return ret;
        }

        public double KernelFunction(int i, int j)
        {
            switch (_kernelType)
            {
                case KernelType.LINEAR:
                    return dot(_x[i], _x[j]);
                case KernelType.POLY:
                    return powi(_gamma * dot(_x[i], _x[j]) + _coef0, _degree);
                case KernelType.RBF:
                    return Math.Exp(-_gamma * (_xSquare[i] + _xSquare[j] - 2 * dot(_x[i], _x[j])));
                case KernelType.SIGMOID:
                    return Math.Tanh(_gamma * dot(_x[i], _x[j]) + _coef0);
                case KernelType.PRECOMPUTED:
                    return _x[i][(int)(_x[j][0].Value)].Value;
                default:
                    return 0;
            }
        }

        public Kernel(int l, Node[][] x_, Parameter param)
        {
            _kernelType = param.KernelType;
            _degree = param.Degree;
            _gamma = param.Gamma;
            _coef0 = param.Coefficient0;

            _x = (Node[][])x_.Clone();

            if (_kernelType == KernelType.RBF)
            {
                _xSquare = new double[l];
                for (int i = 0; i < l; i++)
                    _xSquare[i] = dot(_x[i], _x[i]);
            }
            else _xSquare = null;
        }

        private static double dot(Node[] xNodes, Node[] yNodes)
        {
            double sum = 0;
            int xlen = xNodes.Length;
            int ylen = yNodes.Length;
            int i = 0;
            int j = 0;
            Node x = xNodes[0];
            Node y = yNodes[0];
            while (true)
            {
                if (x._index == y._index)
                {
                    sum += x._value * y._value;
                    i++;
                    j++;
                    if (i < xlen && j < ylen)
                    {
                        x = xNodes[i];
                        y = yNodes[j];
                    }
                    else if (i < xlen)
                    {
                        x = xNodes[i];
                        break;
                    }
                    else if (j < ylen)
                    {
                        y = yNodes[j];
                        break;
                    }
                    else break;
                }
                else
                {
                    if (x._index > y._index)
                    {
                        ++j;
                        if (j < ylen)
                            y = yNodes[j];
                        else break;
                    }
                    else
                    {
                        ++i;
                        if (i < xlen)
                            x = xNodes[i];
                        else break;
                    }
                }
            }
            return sum;
        }

        private static double computeSquaredDistance(Node[] xNodes, Node[] yNodes)
        {
            Node x = xNodes[0];
            Node y = yNodes[0];
            int xLength = xNodes.Length;
            int yLength = yNodes.Length;
            int xIndex = 0;
            int yIndex = 0;
            double sum = 0;

            while (true)
            {
                if (x._index == y._index)
                {
                    double d = x._value - y._value;
                    sum += d * d;
                    xIndex++;
                    yIndex++;
                    if (xIndex < xLength && yIndex < yLength)
                    {
                        x = xNodes[xIndex];
                        y = yNodes[yIndex];
                    }
                    else if(xIndex < xLength){
                        x = xNodes[xIndex];
                        break;
                    }
                    else if(yIndex < yLength){
                        y = yNodes[yIndex];
                        break;
                    }else break;
                }
                else if (x._index > y._index)
                {
                    sum += y._value * y._value;
                    if (++yIndex < yLength)
                        y = yNodes[yIndex];
                    else break;
                }
                else
                {
                    sum += x._value * x._value;
                    if (++xIndex < xLength)
                        x = xNodes[xIndex];
                    else break;
                }
            }

            for (; xIndex < xLength; xIndex++)
            {
                double d = xNodes[xIndex]._value;
                sum += d * d;
            }

            for (; yIndex < yLength; yIndex++)
            {
                double d = yNodes[yIndex]._value;
                sum += d * d;
            }

            return sum;
        }

        public static double KernelFunction(Node[] x, Node[] y, Parameter param)
        {
            switch (param.KernelType)
            {
                case KernelType.LINEAR:
                    return dot(x, y);
                case KernelType.POLY:
                    return powi(param.Degree * dot(x, y) + param.Coefficient0, param.Degree);
                case KernelType.RBF:
                    {
                        double sum = computeSquaredDistance(x, y);
                        return Math.Exp(-param.Gamma * sum);
                    }
                case KernelType.SIGMOID:
                    return Math.Tanh(param.Gamma * dot(x, y) + param.Coefficient0);
                case KernelType.PRECOMPUTED:
                    return x[(int)(y[0].Value)].Value;
                default:
                    return 0;
            }
        }
    }
}
