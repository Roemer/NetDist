using System;
using System.Diagnostics;

namespace NetDist.Server
{
    public static class CpuUsageReader
    {
        private static readonly PerformanceCounter CpuCounter;
        private static DateTime _lastRead;
        private static float _lastValue;

        static CpuUsageReader()
        {
            CpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _lastValue = ReadNextValue();
        }

        public static float GetValue()
        {
            if (DateTime.Now - _lastRead > TimeSpan.FromSeconds(1))
            {
                _lastValue = ReadNextValue();
            }
            return _lastValue;
        }

        private static float ReadNextValue()
        {
            _lastRead = DateTime.Now;
            return CpuCounter.NextValue() / Environment.ProcessorCount;
        }
    }
}
