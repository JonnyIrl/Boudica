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
    public class EververseService
    {
        private readonly DVSContext _db;
        public EververseService(DVSContext database)
        {
            _db = database;
        }

        public async Task<List<Eververse>> GetAll()
        {
            return await _db.EververseItems.OrderBy(x => x.Price).ToListAsync();
        }

    }
}
