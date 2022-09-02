using Boudica.Database;
using Boudica.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Boudica.Services
{
    public class RaidGroupService
    {
        private readonly DVSContext _db;
        public RaidGroupService(DVSContext database)
        {
            _db = database;
        }


    }
}
