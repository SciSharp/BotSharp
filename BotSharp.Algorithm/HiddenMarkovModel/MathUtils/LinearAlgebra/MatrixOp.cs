using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace BotSharp.Algorithm.HiddenMarkovModel.MathUtils.LinearAlgebra
{
    public class MatrixOp
    {
        public static double[] ElementWiseAbs(double[] x)
        {
            int n = x.Length;
            double[] result = new double[n];
            for (int i = 0; i < n; ++i)
            {
                result[i] = System.Math.Abs(x[i]);
            }
            return result;
        }

        public static double[][] DiagonalMatrix(double[] x)
        {
            int n = x.Length;
            double[][] matrix = new double[n][];
            for (int i = 0; i < n; ++i)
            {
                matrix[i] = new double[n];
                matrix[i][i] = x[i];
            }
            return matrix;
        }

        public static double[][] Transpose(double[][] x)
        {
            int rowCount = x.Length;
            int colCount = x[0].Length;
            double[][] xt = new double[colCount][];
            for (int i = 0; i < colCount; ++i)
            {
                xt[i] = new double[rowCount];
                for (int j = 0; j < rowCount; ++j)
                {
                    xt[i][j] = x[j][i];
                }
            }

            return xt;
        }

        public static double[] ElementWiseMinus(double[] x, double[] y)
        {
            int n = x.Length;
            Debug.Assert(y.Length == n);

            double[] result = new double[n];
            for (int i = 0; i < n; ++i)
            {
                result[i] = x[i] - y[i];
            }
            return result;
        }

        public static double[] ElementWiseMinus(double x, double[] y)
        {
            int n = y.Length;
            double[] result = new double[n];
            for (int i = 0; i < n; ++i)
            {
                result[i] = x - y[i];
            }
            return result;
        }

        public static double[] ElementWiseMultiply(double x, double[] y, double[] z)
        {
            int n = y.Length;
            Debug.Assert(z.Length == n);

            double[] result = new double[n];
            for (int i = 0; i < n; ++i)
            {
                result[i] = x * y[i] * z[i];
            }

            return result;
        }

        public static double[] ElementWiseMultiply(double x, double[] y)
        {
            int n = y.Length;

            double[] result = new double[n];
            for (int i = 0; i < n; ++i)
            {
                result[i] = x * y[i];
            }
            return result;
        }

        public static double[] ElementWiseDivide(double[] x, double[] y)
        {
            int n = x.Length;
            Debug.Assert(y.Length == n);

            double[] result = new double[n];
            for (int i = 0; i < n; ++i)
            {
                result[i] = x[i] / y[i];
            }

            return result;
        }

        public static double[] ElementWiseAdd(double[] x, double[] y)
        {
            int n = x.Length;
            Debug.Assert(y.Length == n);
            double[] result = new double[n];
            for (int i = 0; i < n; ++i)
            {
                result[i] = x[i] - y[i];
            }
            return result;
        }

        public static double[] ElementWiseAdd(double x, double[] y)
        {
            int n = y.Length;
            double[] result = new double[n];
            for (int i = 0; i < n; ++i)
            {
                result[i] = x + y[i];
            }

            return result;
        }

        public static double[] ElementWiseExp(double[] x)
        {
            int n = x.Length;
            double[] result = new double[n];
            for (int i = 0; i < n; ++i)
            {
                result[i] = System.Math.Exp(x[i]);
            }
            return result;
        }

        public static double[][] Multiply(double[][] A, double[][] B)
        {
            int n = A.Length;
            int k = A[0].Length;
            int m = B[0].Length;

            Debug.Assert(B.Length == k);

            double[][] result = new double[n][];
            for (int row = 0; row < n; ++row)
            {
                result[row] = new double[m];

                double[] rowA = A[row];
                for (int col = 0; col < m; ++col)
                {
                    double sum = 0;
                    for (int num = 0; num < k; ++num)
                    {
                        sum += rowA[num] * B[num][col];
                    }
                    result[row][col] = sum;
                }
            }

            return result;
        }

        public static double[] Multiply(double[][] X, double[] beta)
        {
            Debug.Assert(X[0].Length == beta.Length);
            int n = X.Length;
            int k = beta.Length;
            double[] result = new double[n];

            for (int i = 0; i < n; ++i)
            {
                result[i] = Multiply(X[i], beta);
            }
            return result;
        }

        public static double Multiply(double[] x, double[] y)
        {
            int n = x.Length;
            Debug.Assert(y.Length == n);
            double sum = 0;
            for (int i = 0; i < n; ++i)
            {
                sum += x[i] * y[i];
            }

            return sum;
        }

        /// <summary>
        /// The method works by using Gaussian elimination to covert the matrix A to a upper triangular matrix, U, and computes the
        /// determinant as the product_i(U_ii) * (-1)^c, where c is the number of row exchange operations that coverts A to U
        /// </summary>
        /// <param name="A">The matrix for which to calculate determinant</param>
        /// <returns>The determinant of A</returns>
        public static double GetDeterminant(double[][] A)
        {
            int ColCount = A[0].Length;
            int RowCount = A.Length;
            Debug.Assert(ColCount == RowCount);

            if (RowCount == 2)
            {
                return A[0][0] * A[1][1] - A[0][1] * A[1][0];
            }

            double det = 1;

            int rowExchangeOpCount = 0;
            double[][] C = GetUpperTriangularMatrix(A, out rowExchangeOpCount);
            for (int i = 0; i < RowCount; ++i)
            {
                det *= C[i][i];
            }

            return det * (rowExchangeOpCount % 2 == 0 ? 1 : -1);
        }

        private static double[][] Clone(double[][] A)
        {
            int rowCount = A.Length;
            double[][] clone = new double[rowCount][];
            for (int r = 0; r < rowCount; ++r)
            {
                clone[r] = (double[])A[r].Clone();
            }
            return clone;
        }


        public static void GaussianElimination(double[][] A, double[][] Q, double[][] M)
        {
            int rowCount = A.Length;
            int colCount = A[0].Length;


        }

        public static double[][] GetUpperTriangularMatrix(double[][] A)
        {
            int rowExchangeOpCount = 0;
            return GetUpperTriangularMatrix(A, out rowExchangeOpCount);
        }

        /// <summary>
        /// The method works by using Gaussian elimination to covert the matrix A to a upper triangular matrix
        /// The computational Complexity is O(n^3)
        /// </summary>
        /// <param name="A">The original matrix</param>
        /// <returns>The upper triangular matrix</returns>
        public static double[][] GetUpperTriangularMatrix(double[][] A, out int rowExchangeOpCount)
        {
            double[][] B = Clone(A);

            int colCount = B[0].Length;
            int rowCount = B.Length;

            HashSet<int> rows_left = new HashSet<int>();
            for (int r = 0; r < rowCount; ++r)
            {
                rows_left.Add(r);
            }

            List<int> row_mapping = new List<int>();
            for (int r = 0; r < rowCount; ++r)
            {
                row_mapping.Add(r);
            }

            rowExchangeOpCount = 0;

            List<int> new_rows = new List<int>();
            for (int c = 0; c < colCount; ++c)
            {
                List<int> nonzero_rows = GetRowsWithNonZeroAtColIndex(rows_left, B, c);
                if (nonzero_rows.Count > 0)
                {
                    int pivot_row = GetPivotRow(nonzero_rows, B, c);

                    new_rows.Add(pivot_row);
                    rows_left.Remove(pivot_row);

                    for (int i = 0; i < nonzero_rows.Count; ++i)
                    {
                        int r = nonzero_rows[i];
                        if (r != pivot_row)
                        {
                            double multiplier = B[r][c] / B[pivot_row][c];

                            for (int j = c; j < rowCount; ++j)
                            {
                                B[r][j] -= B[pivot_row][j] * multiplier;
                            }
                        }
                    }
                }
            }

            foreach (int r in rows_left)
            {
                new_rows.Add(r);
            }

            for (int i = 0; i < new_rows.Count; ++i)
            {
                int new_row = new_rows[i];
                int old_row = i;

                if (new_row != old_row)
                {
                    double[] temp = B[new_row];
                    B[new_row] = B[old_row];
                    B[old_row] = temp;

                    int new_row_index = i;
                    int old_row_index = new_rows.IndexOf(old_row);
                    Swap(new_rows, new_row_index, old_row_index);

                    rowExchangeOpCount++;
                }
            }

            return B;
        }

        private static void Swap(List<int> values, int i, int j)
        {
            int temp = values[i];
            values[i] = values[j];
            values[j] = temp;
        }


        private static int GetPivotRow(List<int> rows, double[][] A, int c)
        {
            double maxValue = double.MinValue;
            double val = 0;
            int pivot_row = 0;
            foreach (int r in rows)
            {
                val = A[r][c];
                if (val > maxValue)
                {
                    maxValue = val;
                    pivot_row = r;
                }
            }
            return pivot_row;
        }

        /// <summary>
        /// Find all the rows in the row_set such that the row has 0 in its c-th column
        /// </summary>
        /// <param name="row_set">The set of row indices from which to return the selected rows</param>
        /// <param name="A">The matrix containing all rows</param>
        /// <param name="c">The targeted column index</param>
        /// <returns>The rows in the row_set such that the row has 0 in its c-th column</returns>
        private static List<int> GetRowsWithNonZeroAtColIndex(HashSet<int> row_set, double[][] A, int c)
        {
            List<int> nonzero_rows = new List<int>();
            foreach (int r in row_set)
            {
                if (A[r][c] != 0)
                {
                    nonzero_rows.Add(r);
                }
            }
            return nonzero_rows;
        }

        public static string Summary(double[][] A)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("[");
            for (int i = 0; i < A.Length; ++i)
            {
                if (i == A.Length - 1)
                {
                    sb.Append(Summary(A[i]));
                }
                else
                {
                    sb.AppendLine(Summary(A[i]));
                }
            }
            sb.Append("]");
            return sb.ToString();
        }

        public static string Summary<T>(T[] v)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("[");
            for (int i = 0; i < v.Length; ++i)
            {
                if (i != 0)
                {
                    sb.Append("\t");
                }
                sb.AppendFormat("{0:0.00}", v[i]);
            }
            sb.Append("]");
            return sb.ToString();
        }
    }
}
