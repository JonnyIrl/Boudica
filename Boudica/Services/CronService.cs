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
    public class CronService
    {
        private readonly Timer _actionTimer;
        private const int FiveMinutes = 300000;

        private readonly IMongoDBContext _mongoDBContext;
        protected IMongoCollection<CronTask> _awardedGuardiansCollection;

        public CronService(IMongoDBContext mongoDBContext, IServiceProvider services)
        {
            _mongoDBContext = mongoDBContext;
            _awardedGuardiansCollection = _mongoDBContext.GetCollection<CronTask>(typeof(CronTask).Name);

            if (_actionTimer == null)
            {
                _actionTimer = new Timer(TimerElapsed, null, FiveMinutes, FiveMinutes);
            }
        }

        public async void TimerElapsed(object state)
        {
            try
            {

            }
            catch (Exception ex)
            {

            }
        }
    }
}
