using System;
using System.Threading;
using System.Threading.Tasks;

namespace BotSharp.Models.CRFLite.Encoder
{
    public class LBFGS
    {
        double [] diag;
        CRFLite.Utils.FixedBigArray<double> w;
        Mcsrch mcsrch_;
        long nfev, point, npt, iter, info, ispt, iypt;
        int iflag_;
        double stp;
        public int zeroone;
        public int err;
        public double obj;

        public double[] expected;
        public double[] v;
        public double[] xi;

        private ParallelOptions parallelOption;

        public LBFGS(int thread_num)
        {
            iflag_ = 0; nfev = 0;
            point = 0; npt = 0; iter = 0; info = 0;
            ispt = 0; iypt = 0;
            stp = 0.0;
            mcsrch_ = new Mcsrch(thread_num);

            parallelOption = new ParallelOptions();
            parallelOption.MaxDegreeOfParallelism = thread_num;
        }

        private double ddot_(long size, CRFLite.Utils.FixedBigArray<double> dx, long dx_idx, CRFLite.Utils.FixedBigArray<double> dy, long dy_idx)
        {
            double ret = 0.0f;
            Parallel.For<double>(0, size, parallelOption, () => 0, (i, loop, subtotal) =>
            {
                subtotal += dx[i + dx_idx] * dy[i + dy_idx];
                return subtotal;
            },
            (subtotal) => // lock free accumulator
            {
                double initialValue;
                double newValue;
                do
                {
                    initialValue = ret; // read current value
                    newValue = initialValue + subtotal;  //calculate new value
                }
                while (initialValue != Interlocked.CompareExchange(ref ret, newValue, initialValue));
            });
            return ret;
        }


        private double ddot_(long size, double[] dx, long dx_idx, double[] dy, long dy_idx)
        {
            double ret = 0.0f;
            Parallel.For<double>(0, size, parallelOption, () => 0, (i, loop, subtotal) =>
            {
                subtotal += dx[i + dx_idx] * dy[i + dy_idx];
                return subtotal;
            },
            (subtotal) => // lock free accumulator
            {
                double initialValue;
                double newValue;
                do
                {
                    initialValue = ret; // read current value
                    newValue = initialValue + subtotal;  //calculate new value
                }
                while (initialValue != Interlocked.CompareExchange(ref ret, newValue, initialValue));
            });
            return ret;
        }
        void pseudo_gradient(double[] x, double C)
        {
            var size = expected.LongLength - 1;
            Parallel.For(1, size + 1, parallelOption, i =>
            {
                if (x[i] == 0)
                {
                    if (expected[i] + C < 0)
                    {
                        v[i] = (expected[i] + C);
                    }
                    else if (expected[i] - C > 0)
                    {
                        v[i] = (expected[i] - C);
                    }
                    else
                    {
                        v[i] = 0;
                    }
                }
                else
                {
                    v[i] = (expected[i] + C * sigma(x[i]));
                }
            });
        }
        double sigma(double x)
        {
            if (x > 0) return 1.0;
            else if (x < 0) return -1.0;
            return 0.0;
        }

        public int optimize(double[] x, double C, bool orthant)
        {
            const long msize = 5;
            var size = x.LongLength - 1;
            if (w == null || w.LongLength == 0)
            {
                iflag_ = 0;
                w = new CRFLite.Utils.FixedBigArray<double>(size * (2 * msize + 1) + 2 * msize, 1);
                diag = new double[size + 1];
                if (orthant == true)
                {
                    xi = new double[size + 1];
                    v = new double[size + 1];
                }
            }

            if (orthant == true)
            {
                pseudo_gradient(x, C);
            }
            else
            {
                v = expected;
            }

            lbfgs_optimize(msize, x, orthant, C);
            if (iflag_ < 0)
            {
                Console.WriteLine("routine stops with unexpected error");
                return -1;
            }

            return iflag_;
        }

        void lbfgs_optimize(long msize, double[] x, bool orthant, double C)
        {
            var size = x.LongLength - 1;
            var yy = 0.0;
            var ys = 0.0;
            long bound = 0;
            long cp = 0;
            var bExit = false;

            // initialization
            if (iflag_ == 0)
            {
                point = 0;
                ispt = size + (msize << 1);
                iypt = ispt + size * msize;

                Parallel.For(1, size + 1, parallelOption, i =>
                {
                    diag[i] = 1.0f;
                    w[ispt + i] = -v[i];
                    w[i] = expected[i];
                });

                if (orthant == true)
                {
                    Parallel.For(1, size + 1, parallelOption, i =>
                    {
                        xi[i] = (x[i] != 0 ? sigma(x[i]) : sigma(-v[i]));
                    });
                }

                //第一次试探步长
                stp = 1.0f / Math.Sqrt(ddot_(size, v, 1, v, 1));

                ++iter;
                info = 0;
                nfev = 0;
            }

            // MAIN ITERATION LOOP
            bExit = LineSearchAndUpdateStepGradient(msize, x, orthant);
            while (bExit == false)
            {
                ++iter;
                info = 0;

                if (orthant == true)
                {
                    Parallel.For(1, size + 1, parallelOption, i =>
                    {
                        xi[i] = (x[i] != 0 ? sigma(x[i]) : sigma(-v[i]));
                    });
                }

                if (iter > size)
                {
                    bound = size;
                }

                // COMPUTE -H*G USING THE FORMULA GIVEN IN: Nocedal, J. 1980,
                // "Updating quasi-Newton matrices with limited storage",
                // Mathematics of Computation, Vol.24, No.151, pp. 773-782.
                ys = ddot_(size, w, iypt + npt + 1, w, ispt + npt + 1);
                yy = ddot_(size, w, iypt + npt + 1, w, iypt + npt + 1);

                var r_ys_yy = ys / yy;
                Parallel.For(1, size + 1, parallelOption, i =>
                {
                    diag[i] = r_ys_yy;
                    w[i] = -v[i];
                });

                cp = point;
                if (point == 0)
                {
                    cp = msize;
                }

                w[size + cp] = (1.0 / ys);
                //回退次数
                bound = Math.Min(iter - 1, msize);
                cp = point;
                for (var i = 1; i <= bound; ++i)
                {
                    --cp;
                    if (cp == -1) cp = msize - 1;
                    var sq = ddot_(size, w, ispt + cp * size + 1, w, 1);
                    var inmc = size + msize + cp + 1;
                    var iycn = iypt + cp * size;
                    w[inmc] = (w[size + cp + 1] * sq);
                    var d = -w[inmc];

                    Parallel.For(1, size + 1, parallelOption, j =>
                        {
                            w[j] = (w[j] + d * w[iycn + j]);
                        });
                }

                Parallel.For(1, size + 1, parallelOption, i =>
                {
                    w[i] = (diag[i] * w[i]);
                });

                for (var i = 1; i <= bound; ++i)
                {
                    var yr = ddot_(size, w, iypt + cp * size + 1, w, 1);
                    var beta = w[size + cp + 1] * yr;
                    var inmc = size + msize + cp + 1;
                    beta = w[inmc] - beta;
                    var iscn = ispt + cp * size;

                    Parallel.For(1, size + 1, parallelOption, j =>
                        {
                            w[j] = (w[j] + beta * w[iscn + j]);
                        });

                    ++cp;
                    if (cp == msize)
                    {
                        cp = 0;
                    }
                }

                if (orthant == true)
                {
                    Parallel.For(1, size + 1, parallelOption, i =>
                    {
                        w[i] = (sigma(w[i]) == sigma(-v[i]) ? w[i] : 0);
                    });
                }


                // STORE THE NEW SEARCH DIRECTION
                var offset = ispt + point * size;
                Parallel.For(1, size + 1, parallelOption, i =>
                {
                    w[offset + i] = w[i];
                    w[i] = expected[i];
                });

                stp = 1.0f;
                nfev = 0;
                bExit = LineSearchAndUpdateStepGradient(msize, x, orthant);
            }
        }

        private bool LineSearchAndUpdateStepGradient(long msize, double[] x, bool orthant)
        {
            var size = x.LongLength - 1;
            var bExit = false;
            mcsrch_.mcsrch(x, obj, v, w, ispt + point * size,
                            ref stp, ref info, ref nfev, diag);
            if (info == -1)
            {
                if (orthant == true)
                {
                    Parallel.For(1, size + 1, parallelOption, i =>
                    {
                        x[i] = (sigma(x[i]) == sigma(xi[i]) ? x[i] : 0);
                    });
                }


                iflag_ = 1;  // next value
                bExit = true;
            }
            else if (info != 1)
            {
                //MCSRCH error, please see error code in info
                iflag_ = -1;
                bExit = true;
            }
            else
            {
                // COMPUTE THE NEW STEP AND GRADIENT CHANGE
                npt = point * size;
                Parallel.For(1, size + 1, parallelOption, i =>
                {
                    w[ispt + npt + i] = (stp * w[ispt + npt + i]);
                    w[iypt + npt + i] = expected[i] - w[i];
                });

                ++point;
                if (point == msize)
                {
                    point = 0;
                }

                var gnorm = Math.Sqrt(ddot_(size, v, 1, v, 1));
                var xnorm = Math.Max(1.0, Math.Sqrt(ddot_(size, x, 1, x, 1)));
                if (gnorm / xnorm <= BaseUtils.eps)
                {
                    iflag_ = 0;  // OK terminated
                    bExit = true;
                }
            }

            return bExit;
        }
    }
}
