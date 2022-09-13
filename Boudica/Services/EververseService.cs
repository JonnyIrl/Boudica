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
using Boudica.Classes;

namespace Boudica.Services
{
    public class EververseService
    {
        private readonly DVSContext _db;
        public EververseService(DVSContext database)
        {
            _db = database;
        }

        public async Task<Item> GetItem(int id)
        {
            Eververse eververseItem = await _db.EververseItems.FirstOrDefaultAsync(x => x.Item.Id == id);
            if (eververseItem == null) return null;
            return eververseItem.Item;
        }

        public async Task<Eververse> Get(int id)
        {
            return await _db.EververseItems.Include(x => x.Item).FirstOrDefaultAsync(x => x.Item.Id == id);
        }

        public async Task<List<Eververse>> GetAll()
        {
            return await _db.EververseItems.Include(x => x.Item).OrderBy(x => x.Item.IsPrimary).ThenBy(x => x.Item.IsSecondary).ThenBy(x => x.Item.IsSuper).ToListAsync();
        }

        public async Task<List<Eververse>> GetAllPrimaryWeapons()
        {
            return await _db.EververseItems.Include(x => x.Item).Where(x => x.Item.IsPrimary).OrderBy(x => x.Price).ToListAsync();
        }

        public async Task<List<Eververse>> GetAllSecondaryWeapons()
        {
            return await _db.EververseItems.Include(x => x.Item).Where(x => x.Item.IsSecondary).OrderBy(x => x.Price).ToListAsync();
        }

        public async Task<List<Eververse>> GetAllSupers()
        {
            return await _db.EververseItems.Include(x => x.Item).Where(x => x.Item.IsSuper).OrderBy(x => x.Price).ToListAsync();
        }

        public async Task<ResponseResult> PurchaseItem(ulong userId, int id)
        {
            Eververse eververseItem = await Get(id);
            if(eververseItem == null)
            {
                return await Task.FromResult(new ResponseResult(false, "Could not find matching item. Please make sure the Id is correct"));
            }

            if(eververseItem.Item == null)
            {
                return await Task.FromResult(new ResponseResult(false, "Could not find matching item. Please make sure the Id is correct"));
            }

            List<GuardianInventory> existingGuardianInventoryItems = await _db.GuardiansInventory.Where(x => x.UserId == userId.ToString()).ToListAsync();
            if(existingGuardianInventoryItems == null || (existingGuardianInventoryItems.Any() && existingGuardianInventoryItems[0].Guardian == null))
            {
                return await Task.FromResult(new ResponseResult(false, "Guardian could not be found"));
            }

            if(existingGuardianInventoryItems.FirstOrDefault(x => x.Item.Id == id) != null)
            {
                return await Task.FromResult(new ResponseResult(false, "You already own this item, we'll nickname you Banshee"));
            }

            Guardian existingGuardian = await _db.Guardians.FirstOrDefaultAsync(x => x.UserId == userId.ToString());
            if(existingGuardian == null)
            {
                return await Task.FromResult(new ResponseResult(false, "Guardian could not be found"));
            }
            

            if(existingGuardian.Glimmer >= eververseItem.Price)
            {
                try
                {
                    await _db.GuardiansInventory.AddAsync(new GuardianInventory()
                    {
                        UserId = userId.ToString(),
                        ItemId = id,
                        GuardianId = existingGuardian.Id
                    });

                    await _db.SaveChangesAsync(true);

                }
                catch(Exception ex)
                {
                    return await Task.FromResult(new ResponseResult(false, "Oops something went wrong we couldn't purchase this item.. blame Jonny"));
                }
            }
            else
            {
                return await Task.FromResult(new ResponseResult(false, $"You don't have enough Glimmer to purchase this, your total Glimmer is: {string.Format("{0:n0}", existingGuardian.Glimmer)}, you need another {string.Format("{0:n0}", (eververseItem.Price - existingGuardian.Glimmer))} Glimmer"));
            }

            return await Task.FromResult(new ResponseResult(true, $"Successfully purchased {eververseItem.Item.DisplayName}, you can view your inventory by typing ;inventory"));
        }

    }
}
