using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace PipServices3.MongoDb.Persistence
{
    public static class PartitionHelper
    {
        public const string PartitionKey = "partition_key";
        public const string PartitionPrefix = "partition";
        public const string PartitionTemplate = "{0}_{1}";
        public const int PartitionCount = 10;

        public static string GetValue(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return string.Empty;
            }

            return string.Format(PartitionTemplate, PartitionPrefix,
                BigInteger.Abs(GetHashNumber(id) % PartitionCount));
        }

        public static string GetName(int index)
        {
            return string.Format(PartitionTemplate, PartitionPrefix, index);
        }

        private static BigInteger GetHashNumber(string key)
        {
            using (var algorithm = SHA1.Create())
            {
                return new BigInteger(algorithm.ComputeHash(Encoding.UTF8.GetBytes(key)));
            }
        }
    }
}
