using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnityGameServers
{
    // This class is shared between the Unity code and the Azure Function.
    // We use this to send data between the client and the Azure Function.


    class ServerData
    {
        public string   id;
        public string   ipAddress;
        public int      port;
        public string   scene;
        public int      playerCount;
    }

    class ServerDataList
    {
        public List<ServerData> data;

        public ServerDataList() 
        { 
            data = new List<ServerData>();
        }

    }

}
