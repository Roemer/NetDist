using NetDist.Core;
using NetDist.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Wpf.Shared;
using WpfServerAdmin.Core;

namespace WpfServerAdmin.ViewModels
{
    public class ClientInfoViewModel
    {
        private readonly ExtendedClientInfo _client;

        public Guid Id { get { return _client.ClientInfo.Id; } }
        public string Name { get { return _client.ClientInfo.Name; } }
        public string Version { get { return _client.ClientInfo.Version; } }
        public float Cpu { get { return _client.ClientInfo.CpuUsage; } }
        public List<DiskInformationViewModel> DiskInformations { get { return _client.ClientInfo.DiskInformations.ConvertAll(i => new DiskInformationViewModel(i)); } }
        public ulong TotalMemory { get { return _client.ClientInfo.TotalMemory; } }
        public ulong UsedMemory { get { return _client.ClientInfo.UsedMemory; } }
        public long TotalJobsProcessed { get { return _client.TotalJobsProcessed; } }
        public long TotalJobsFailed { get { return _client.TotalJobsFailed; } }
        public int JobsInProgress { get { return _client.JobsInProgress; } }
        public DateTime LastUpdate { get { return _client.LastCommunicationDate; } }
        public DateTime StartDate { get { return _client.ClientInfo.StartDate; } }

        #region Calculated properties
        public bool IsOffline { get { return LastUpdate.AddMinutes(5) < DateTime.Now; } }
        public bool IsCritical { get { return UsedDiskSpacePercentage > 90; } }
        public float MemoryPercentage { get { return UsedMemory / (float)TotalMemory * 100; } }
        public float UsedDiskSpacePercentage
        {
            get
            {
                var lowestFreeDiskSpace = _client.ClientInfo.DiskInformations.OrderBy(i => i.FreeDiskSpace).First();
                return 100.0f / lowestFreeDiskSpace.TotalDiskSpace * (lowestFreeDiskSpace.TotalDiskSpace - lowestFreeDiskSpace.FreeDiskSpace);
            }
        }

        #endregion

        public ClientInfoViewModel(ExtendedClientInfo client)
        {
            _client = client;
            DeleteCommand = new RelayCommand(param => OnClientEvent(new ClientEventArgs(ClientEventType.Delete, _client.ClientInfo.Id)));
        }

        public ICommand DeleteCommand { get; private set; }

        public event EventHandler<ClientEventArgs> ClientEvent;

        protected virtual void OnClientEvent(ClientEventArgs e)
        {
            var handler = ClientEvent;
            if (handler != null) handler(this, e);
        }
    }

    public class DiskInformationViewModel
    {
        private readonly DiskInformation _diskInformation;

        #region Calculated properties
        public float UsedDiskSpacePercentage { get { return 100.0f / _diskInformation.TotalDiskSpace * (_diskInformation.TotalDiskSpace - _diskInformation.FreeDiskSpace); } }
        public string FormattedFreeDiskSpace { get { return SizeSuffix.AddSizeSuffix(_diskInformation.FreeDiskSpace); } }
        public string DisplayName { get { return String.Format("{0} ({1})", _diskInformation.Label, _diskInformation.Name); } }
        #endregion

        public DiskInformationViewModel(DiskInformation diskInformation)
        {
            _diskInformation = diskInformation;
        }
    }
}
