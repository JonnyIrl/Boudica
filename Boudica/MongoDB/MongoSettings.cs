using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.MongoDB
{
    public class Mongosettings
    {
        public string MongoDebugConnectionString { get; set; }
        public string MongoDebugDatabaseName { get; set; }
        public string MongoReleaseConnectionString { get; set; }
        public string MongoReleaseDatabaseName { get; set; }
    }
}
