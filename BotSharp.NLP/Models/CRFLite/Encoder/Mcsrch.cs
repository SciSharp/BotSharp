using BotSharp.Models.CRFLite.Utils;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BotSharp.Models.CRFLite.Encoder
{
    class Mcsrch
    {
        private int infoc;
        private bool stage1, brackt;
        private double dginit;
        private double width, width1;
        private double fx, dgx, fy, dgy;
        private double finit;
        private double dgtest;
        private double stx, sty;
        private double stmin, stmax;

        private ParallelOptions parallelOption;

        public Mcsrch(int thread_num)
        {
            infoc = 0;
            stage1 = false;
            brackt = false;
            finit = 0.0;
            dginit = 0.0;
            dgtest = 0.0;
            width = 0.0;
            width1 = 0.0;
            stx = 0.0;
            fx = 0.0;
            dgx = 0.0;
            sty = 0.0;
            fy = 0.0;
            dgy = 0.0;
            stmin = 0.0;
            stmax = 0.0;

            parallelOption = new ParallelOptions();
            parallelOption.MaxDegreeOfParallelism = thread_num;
        }



        void mcstep(ref double stx, ref double fx, ref double dx,
            ref double sty, ref double fy, ref double dy,
            ref double stp, double fp, double dp,
            ref bool brackt,
            double stpmin, double stpmax,
            ref int info)
        {
            var bound = true;
            double p, q, d3, r, stpq, stpc, stpf;
            double gamma;
            double s;
            double d1, d2;
            double theta;
            info = 0;

            if (brackt == true && ((stp <= Math.Min(stx, sty) || stp >= Math.Max(stx, sty)) ||
                            dx * (stp - stx) >= 0.0 || stpmax < stpmin))
            {
                return;
            }

            var sgnd = dp * (dx / Math.Abs(dx));
            if (fp > fx)
            {
                info = 1;
                bound = true;
                theta = (fx - fp) * 3 / (stp - stx) + dx + dp;
                d1 = Math.Abs(theta);
                d2 = Math.Abs(dx);
                d1 = Math.Max(d1, d2);
                d2 = Math.Abs(dp);
                s = Math.Max(d1, d2);
                d1 = theta / s;
                gamma = s * Math.Sqrt(d1 * d1 - dx / s * (dp / s));
                if (stp < stx)
                {
                    gamma = -gamma;
                }
                p = gamma - dx + theta;
                q = gamma - dx + gamma + dp;
                r = p / q;
                stpc = stx + r * (stp - stx);
                stpq = stx + dx / ((fx - fp) /
                                     (stp - stx) + dx) / 2 * (stp - stx);
                d1 = stpc - stx;
                d2 = stpq - stx;
                if (Math.Abs(d1) < Math.Abs(d2))
                {
                    stpf = stpc;
                }
                else
                {
                    stpf = stpc + (stpq - stpc) / 2;
                }
                brackt = true;
            }
            else if (sgnd < 0.0)
            {
                info = 2;
                bound = false;
                theta = (fx - fp) * 3 / (stp - stx) + dx + dp;
                d1 = Math.Abs(theta);
                d2 = Math.Abs(dx);
                d1 = Math.Max(d1, d2);
                d2 = Math.Abs(dp);
                s = Math.Max(d1, d2);
                d1 = theta / s;
                gamma = s * Math.Sqrt(d1 * d1 - dx / s * (dp / s));
                if (stp > stx)
                {
                    gamma = -gamma;
                }
                p = gamma - dp + theta;
                q = gamma - dp + gamma + dx;
                r = p / q;
                stpc = stp + r * (stx - stp);
                stpq = stp + dp / (dp - dx) * (stx - stp);

                d1 = stpc - stp;
                d2 = stpq - stp;
                if (Math.Abs(d1) > Math.Abs(d2))
                {
                    stpf = stpc;
                }
                else
                {
                    stpf = stpq;
                }
                brackt = true;
            }
            else if (Math.Abs(dp) < Math.Abs(dx))
            {
                info = 3;
                bound = true;
                theta = (fx - fp) * 3 / (stp - stx) + dx + dp;
                d1 = Math.Abs(theta);
                d2 = Math.Abs(dx);
                d1 = Math.Max(d1, d2);
                d2 = Math.Abs(dp);
                s = Math.Max(d1, d2);
                d3 = theta / s;
                d1 = 0.0f;
                d2 = d3 * d3 - dx / s * (dp / s);
                gamma = s * Math.Sqrt((Math.Max(d1, d2)));
                if (stp > stx)
                {
                    gamma = -gamma;
                }
                p = gamma - dp + theta;
                q = gamma + (dx - dp) + gamma;
                r = p / q;
                if (r < 0.0 && gamma != 0.0)
                {
                    stpc = stp + r * (stx - stp);
                }
                else if (stp > stx)
                {
                    stpc = stpmax;
                }
                else
                {
                    stpc = stpmin;
                }
                stpq = stp + dp / (dp - dx) * (stx - stp);
                if (brackt == true)
                {
                    d1 = stp - stpc;
                    d2 = stp - stpq;
                    if (Math.Abs(d1) < Math.Abs(d2))
                    {
                        stpf = stpc;
                    }
                    else
                    {
                        stpf = stpq;
                    }
                }
                else
                {
                    d1 = stp - stpc;
                    d2 = stp - stpq;
                    if (Math.Abs(d1) > Math.Abs(d2))
                    {
                        stpf = stpc;
                    }
                    else
                    {
                        stpf = stpq;
                    }
                }
            }
            else
            {
                info = 4;
                bound = false;
                if (brackt == true)
                {
                    theta = (fp - fy) * 3 / (sty - stp) + dy + dp;
                    d1 = Math.Abs(theta);
                    d2 = Math.Abs(dy);
                    d1 = Math.Max(d1, d2);
                    d2 = Math.Abs(dp);
                    s = Math.Max(d1, d2);
                    d1 = theta / s;
                    gamma = s * Math.Sqrt(d1 * d1 - dy / s * (dp / s));
                    if (stp > sty)
                    {
                        gamma = -gamma;
                    }
                    p = gamma - dp + theta;
                    q = gamma - dp + gamma + dy;
                    r = p / q;
                    stpc = stp + r * (sty - stp);
                    stpf = stpc;
                }
                else if (stp > stx)
                {
                    stpf = stpmax;
                }
                else
                {
                    stpf = stpmin;
                }
            }

            if (fp > fx)
            {
                sty = stp;
                fy = fp;
                dy = dp;
            }
            else
            {
                if (sgnd < 0.0)
                {
                    sty = stx;
                    fy = fx;
                    dy = dx;
                }
                stx = stp;
                fx = fp;
                dx = dp;
            }

            stpf = Math.Min(stpmax, stpf);
            stpf = Math.Max(stpmin, stpf);
            stp = stpf;
            if (brackt == true && bound)
            {
                if (sty > stx)
                {
                    d1 = stx + (sty - stx) * 0.66;
                    stp = Math.Min(d1, stp);
                }
                else
                {
                    d1 = stx + (sty - stx) * 0.66;
                    stp = Math.Max(d1, stp);
                }
            }

            return;
        }



        const double lb3_1_gtol = 0.9;
        const double xtol = 1e-16;
        const double lb3_1_stpmin = 1e-20;
        const double lb3_1_stpmax = 1e20;
        const double ftol = 1e-4;
        const double p5 = 0.5;
        const double p66 = 0.66;
        const double xtrapf = 4.0;
        const int maxfev = 20;

        private double ddot_(long size, double[] dx, long dx_idx, FixedBigArray<double> dy, long dy_idx)
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

        public void mcsrch(double[] x, double f, double[] g, FixedBigArray<double> s, long s_idx,
            ref double stp, ref long info, ref long nfev, double[] wa)
        {
            var size = x.LongLength - 1;
            /* Parameter adjustments */
            if (info == -1)
            {
                info = 0;
                nfev++;

                var dg = ddot_(size, g, 1, s, s_idx + 1);
                var ftest1 = finit + stp * dgtest;

                if (brackt && ((stp <= stmin || stp >= stmax) || infoc == 0))
                {
                    info = 6;
                    Console.WriteLine("MCSRCH warning: Rounding errors prevent further progress.There may not be a step which satisfies the sufficient decrease and curvature conditions. Tolerances may be too small.");
                    Console.WriteLine("bracket: {0}, stp:{1}, stmin:{2}, stmax:{3}, infoc:{4}", brackt, stp, stmin, stmax, infoc);
                }
                if (stp == lb3_1_stpmax && f <= ftest1 && dg <= dgtest)
                {
                    info = 5;
                    Console.WriteLine("MCSRCH warning: The step is too large.");
                }
                if (stp == lb3_1_stpmin && (f > ftest1 || dg >= dgtest))
                {
                    info = 4;
                    Console.WriteLine("MCSRCH warning: The step is too small.");
                    Console.WriteLine("stp:{0}, lb3_1_stpmin:{1}, f:{2}, ftest1:{3}, dg:{4}, dgtest:{5}", stp, lb3_1_stpmin, f, ftest1, dg, dgtest);
                }
                if (nfev >= maxfev)
                {
                    info = 3;
                    Console.WriteLine("MCSRCH warning: More than {0} function evaluations were required at the present iteration.", maxfev);
                }
                if (brackt && stmax - stmin <= xtol * stmax)
                {
                    info = 2;
                    Console.WriteLine("MCSRCH warning: Relative width of the interval of uncertainty is at most xtol.");
                }
                if (f <= ftest1 && Math.Abs(dg) <= lb3_1_gtol * (-dginit))
                {
                    info = 1;
                }

                if (info != 0)
                {
                    return;
                }

                if (stage1 && f <= ftest1 && dg >= Math.Min(ftol, lb3_1_gtol) * dginit)
                {
                    stage1 = false;
                }

                if (stage1 && f <= fx && f > ftest1)
                {
                    var fm = f - stp * dgtest;
                    var fxm = fx - stx * dgtest;
                    var fym = fy - sty * dgtest;
                    var dgm = dg - dgtest;
                    var dgxm = dgx - dgtest;
                    var dgym = dgy - dgtest;
                    mcstep(ref stx, ref fxm, ref dgxm, ref sty, ref fym, ref dgym, ref stp, fm, dgm, ref brackt,
                           stmin, stmax, ref infoc);
                    fx = fxm + stx * dgtest;
                    fy = fym + sty * dgtest;
                    dgx = dgxm + dgtest;
                    dgy = dgym + dgtest;
                }
                else
                {
                    mcstep(ref stx, ref fx, ref dgx, ref sty, ref fy, ref dgy, ref stp, f, dg, ref brackt,
                           stmin, stmax, ref infoc);
                }

                if (brackt)
                {
                    var d1 = 0.0;
                    d1 = sty - stx;
                    if (Math.Abs(d1) >= p66 * width1)
                    {
                        stp = stx + p5 * (sty - stx);
                    }
                    width1 = width;
                    d1 = sty - stx;
                    width = Math.Abs(d1);
                }
            }
            else
            {
                infoc = 1;
                if (size <= 0 || stp <= 0.0)
                {
                    return;
                }

                dginit = ddot_(size, g, 1, s, s_idx + 1);
                if (dginit >= 0.0)
                {
                    return;
                }

                brackt = false;
                stage1 = true;
                nfev = 0;
                finit = f;
                dgtest = ftol * dginit;
                width = lb3_1_stpmax - lb3_1_stpmin;
                width1 = width / p5;

                Parallel.For(1, size + 1, parallelOption, i =>
                {
                    wa[i] = x[i];
                }
                );

                stx = 0.0;
                fx = finit;
                dgx = dginit;
                sty = 0.0;
                fy = finit;
                dgy = dginit;
            }

            if (brackt)
            {
                stmin = Math.Min(stx, sty);
                stmax = Math.Max(stx, sty);
            }
            else
            {
                stmin = stx;
                stmax = stp + xtrapf * (stp - stx);
            }

            stp = Math.Max(stp, lb3_1_stpmin);
            stp = Math.Min(stp, lb3_1_stpmax);

            if ((brackt && ((stp <= stmin || stp >= stmax) ||
                            nfev >= maxfev - 1 || infoc == 0)) ||
                (brackt && (stmax - stmin <= xtol * stmax)))
            {
                stp = stx;
            }

            var stp_t = stp;
            Parallel.For(1, size + 1, parallelOption, i =>
            {
                x[i] = (wa[i] + stp_t * s[s_idx + i]);
            });

            info = -1;
        }

    }
}
