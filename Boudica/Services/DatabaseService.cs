using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.Services
{
    public class DatabaseService : Database
    {
        public DatabaseService()
        {
            int breakHere = 0;
        }
        public override async Task<string> GetData()
        {
            return await Task.FromResult("message");
        }

    }


    public abstract class Database
    {
        public abstract Task<string> GetData();
    }
}
