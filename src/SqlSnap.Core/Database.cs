using System.IO;

namespace SqlSnap.Core
{
    public class Database
    {
        public string Name { get; set; }

        public Stream MetadataStream { get; set; }
    }
}