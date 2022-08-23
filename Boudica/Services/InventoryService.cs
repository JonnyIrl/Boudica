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
    public class InventoryService
    {
        private readonly DVSContext _db;
        public InventoryService(DVSContext database)
        {
            _db = database;
        }

        public async Task<Item> Get(ulong userId, int itemId)
        {
            GuardianInventory guardianInventory = await _db.GuardiansInventory.FirstOrDefaultAsync(x => x.UserId == userId.ToString() && x.Item.Id == itemId);
            if(guardianInventory == null)
            {
                return null;
            }

            return guardianInventory.Item;
        }
    }
}
