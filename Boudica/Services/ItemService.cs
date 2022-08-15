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
    public class ItemService
    {
        private readonly DVSContext _db;
        public ItemService(DVSContext database)
        {
            _db = database;
        }

        public async Task<Item> Get(string displayName)
        {
            return await _db.Items.FirstOrDefaultAsync(x => x.DisplayName == displayName);
        }

        public async Task<bool> CreateItem(string itemJson)
        {
            Item newItem = JsonConvert.DeserializeObject<Item>(itemJson);
            if (newItem == null) return await Task.FromResult(false);

            Item existingItem = await Get(newItem.DisplayName);
            if(existingItem != null) return await Task.FromResult(false);

            await _db.Items.AddAsync(newItem);
            await _db.SaveChangesAsync();
            return await Task.FromResult(true);
        }
    }
}
