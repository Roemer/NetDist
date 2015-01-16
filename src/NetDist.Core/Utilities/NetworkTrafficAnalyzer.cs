using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace NetDist.Core.Utilities
{
    public enum NetworkTrafficAnalyzerCounterType
    {
        InLastSec,
        OutLastSec,
    }

    public class NetworkTrafficAnalyzer
    {
        private readonly PerformanceCounterCategory _performanceCounterCategory;

        private readonly Dictionary<string, Tuple<long, long>> _startValues;

        public string CurrentAdapter { get; set; }

        public long TotalTrafficIn
        {
            get
            {
                var startValue = _startValues[CurrentAdapter];
                var currentValue = GetCounter(CurrentAdapter, NetworkTrafficAnalyzerCounterType.InLastSec).NextSample().RawValue;
                return currentValue - startValue.Item1;
            }
        }

        public long TotalTrafficOut
        {
            get
            {
                var startValue = _startValues[CurrentAdapter];
                var currentValue = GetCounter(CurrentAdapter, NetworkTrafficAnalyzerCounterType.OutLastSec).NextSample().RawValue;
                return currentValue - startValue.Item2;
            }
        }

        public NetworkTrafficAnalyzer()
        {
            _performanceCounterCategory = new PerformanceCounterCategory("Network Interface");
            _startValues = new Dictionary<string, Tuple<long, long>>();
            var allAdapters = GetNetworkAdapters();
            foreach (var adapter in allAdapters)
            {
                SetValuesForAdapter(adapter);
            }
            CurrentAdapter = allAdapters.First();
        }

        /// <summary>
        /// Get all available network adapters
        /// </summary>
        public string[] GetNetworkAdapters()
        {
            return _performanceCounterCategory.GetInstanceNames();
        }

        public void Reset()
        {
            SetValuesForAdapter(CurrentAdapter);
        }

        /// <summary>
        /// Manually get a counter
        /// </summary>
        public PerformanceCounter GetCounter(string instanceName, NetworkTrafficAnalyzerCounterType counter)
        {
            var text = CounterToString(counter);
            return new PerformanceCounter("Network Interface", text, instanceName);
        }

        private void SetValuesForAdapter(string adapter)
        {
            var trafficIn = GetCounter(adapter, NetworkTrafficAnalyzerCounterType.InLastSec).NextSample().RawValue;
            var trafficOut = GetCounter(adapter, NetworkTrafficAnalyzerCounterType.OutLastSec).NextSample().RawValue;
            _startValues[adapter] = Tuple.Create(trafficIn, trafficOut);
        }

        private string CounterToString(NetworkTrafficAnalyzerCounterType counter)
        {
            switch (counter)
            {
                case NetworkTrafficAnalyzerCounterType.InLastSec:
                    return "Bytes Received/sec";
                case NetworkTrafficAnalyzerCounterType.OutLastSec:
                    return "Bytes Sent/sec";
                default:
                    throw new ArgumentOutOfRangeException("counter");
            }
        }
    }
}
