using System;

namespace LightSide.UIBenchmark
{
    /// <summary>
    /// Summary statistics over one recorded metric for a scenario.
    /// </summary>
    public struct StatResult
    {
        public int count;
        public double min;
        public double max;
        public double mean;
        public double p50;
        public double p95;
        public double p99;
        public double std;
    }

    /// <summary>
    /// Fixed-capacity append buffer for one per-frame metric. <see cref="Add"/> is allocation-free
    /// so it is safe on the measured hot path; <see cref="Compute"/> sorts a copy and is meant to
    /// run once at scenario end.
    /// </summary>
    public sealed class StatSeries
    {
        readonly double[] _v;
        int _count;

        public StatSeries(int capacity)
        {
            _v = new double[capacity < 1 ? 1 : capacity];
        }

        /// <summary>Samples recorded so far this scenario.</summary>
        public int Count => _count;

        /// <summary>Clears the buffer for a new scenario without reallocating.</summary>
        public void Reset() => _count = 0;

        /// <summary>Appends one sample. Silently drops samples past capacity.</summary>
        public void Add(double value)
        {
            if (_count < _v.Length) _v[_count++] = value;
        }

        /// <summary>Sum of all samples; used for totals such as bytes allocated over the run.</summary>
        public double Sum()
        {
            double s = 0;
            for (int i = 0; i < _count; i++) s += _v[i];
            return s;
        }

        /// <summary>Computes min/max/mean/std and the p50/p95/p99 percentiles over the recorded samples.</summary>
        public StatResult Compute()
        {
            var r = new StatResult { count = _count };
            if (_count == 0) return r;

            var tmp = new double[_count];
            Array.Copy(_v, tmp, _count);
            Array.Sort(tmp);

            double sum = 0;
            for (int i = 0; i < _count; i++) sum += tmp[i];

            r.min = tmp[0];
            r.max = tmp[_count - 1];
            r.mean = sum / _count;
            r.p50 = Percentile(tmp, 0.50);
            r.p95 = Percentile(tmp, 0.95);
            r.p99 = Percentile(tmp, 0.99);

            double varSum = 0;
            for (int i = 0; i < _count; i++)
            {
                double d = tmp[i] - r.mean;
                varSum += d * d;
            }
            r.std = Math.Sqrt(varSum / _count);
            return r;
        }

        static double Percentile(double[] sorted, double p)
        {
            int idx = (int)Math.Ceiling(p * sorted.Length) - 1;
            if (idx < 0) idx = 0;
            if (idx >= sorted.Length) idx = sorted.Length - 1;
            return sorted[idx];
        }
    }
}
