using NetDist.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace NetDist.Server
{
    public class ClientManager
    {
        /// <summary>
        /// Dictionary which olds information about the known clients
        /// </summary>
        private readonly ConcurrentDictionary<Guid, ExtendedClientInfo> _knownClients;

        public ClientManager()
        {
            _knownClients = new ConcurrentDictionary<Guid, ExtendedClientInfo>();
        }

        public void Clear()
        {
            _knownClients.Clear();
        }

        public IEnumerable<ExtendedClientInfo> GetStatistics()
        {
            return _knownClients.Select(kvp => kvp.Value);
        }

        public ExtendedClientInfo GetOrCreate(Guid clientId)
        {
            return _knownClients.GetOrAdd(clientId, guid => Create(clientId));
        }

        private ExtendedClientInfo Create(Guid clientId)
        {
            return new ExtendedClientInfo
            {
                ClientInfo = new ClientInfo { Id = clientId },
                LastCommunicationDate = DateTime.Now
            };
        }
    }
}
