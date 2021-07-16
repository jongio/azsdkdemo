using Azure.Cosmos;
using Lib.Model;

namespace Lib.Data.Providers
{
    public class CosmosImage : Image
    {
        public CosmosImage()
        {
        }

        public PartitionKey PartitionKeyValue { get => new PartitionKey(this.PartitionKey); }
    }
}