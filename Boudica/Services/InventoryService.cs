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

        public async Task<List<Item>> GetAllItems(ulong userId)
        {
            List<GuardianInventory> guardianInventory = await _db.GuardiansInventory.Include(x => x.Item).Where(x => x.UserId == userId.ToString()).ToListAsync();
            if (guardianInventory == null)
            {
                return null;
            }

            return guardianInventory.Select(x => x.Item).ToList();
        }

        public async Task<Item> Get(ulong userId, int itemId)
        {
            GuardianInventory guardianInventory = await _db.GuardiansInventory.Include(x => x.Item).FirstOrDefaultAsync(x => x.UserId == userId.ToString() && x.Item.Id == itemId);
            if(guardianInventory == null)
            {
                return null;
            }

            return guardianInventory.Item;
        }
    }
}
