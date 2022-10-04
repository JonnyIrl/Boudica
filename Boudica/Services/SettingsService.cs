using Boudica.MongoDB;
using Boudica.MongoDB.Models;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.Services
{
    public class SettingsService
    {
        private readonly IMongoDBContext _mongoDBContext;
        protected IMongoCollection<DBSetting> _settingsCollection;

        public SettingsService(IMongoDBContext mongoDBContext)
        {
            _mongoDBContext = mongoDBContext;
            _settingsCollection = _mongoDBContext.GetCollection<DBSetting>("Settings");
            //SeedDatabase();
        }

        public async Task<string> GetValue(string settingName)
        {
            return (await _settingsCollection.Find(x => x.Id == settingName).FirstOrDefaultAsync())?.Value;
        }

        public async Task<int> GetValueNumber(string settingName)
        {
            string value = (await _settingsCollection.Find(x => x.Id == settingName).FirstOrDefaultAsync())?.Value;
            if (string.IsNullOrEmpty(value)) return -1;
            return int.TryParse(value, out int result) ? result : -1;
        }

        private void SeedDatabase()
        {
            //_settingsCollection.InsertOne(new DBSetting() { Id = "AwardedGlimmerAmount", Value = "3" });
        }
    }
}
