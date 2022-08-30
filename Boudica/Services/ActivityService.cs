using Boudica.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.Services
{
    public class ActivityService
    {
        private readonly DVSContext _db;

        public ActivityService(DVSContext database)
        {
            _db = database;
        }


    }

}
