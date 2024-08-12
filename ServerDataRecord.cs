using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;



namespace UnityGameServers
{
    // This is the record for the Azure Storage Table

    public class ServerDataRecord : ITableEntity
    {
        public string ID { get; set; }
        public string IPAddress { get; set; }
        public int Port { get; set; }
        public string Scene { get; set; }
        public int PlayerCount { get; set; }


        public DateTime LastUpdateDate { get; set; }


        public DateTime Date { get; set; }
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }


        public ServerDataRecord(string id, string ipaddress, int port, string scene, int playercount, DateTime savedate)
        {
            ID = id;
            IPAddress = ipaddress;
            Port = port;
            Scene = scene;
            PlayerCount = playercount;
            LastUpdateDate = savedate;

            Date = savedate;
            PartitionKey = "UnityGameServer";
            RowKey = id;
        }

        public ServerDataRecord()
        {

        }
    }
}
