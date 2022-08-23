using Boudica.Database;
using Boudica.Database.Models;
using Discord.Commands;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Boudica.Services
{
    public class LoadoutService
    {
        private readonly DVSContext _db;
        public LoadoutService(DVSContext database)
        {
            _db = database;
        }

    }
}
