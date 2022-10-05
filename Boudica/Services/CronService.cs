using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.Services
{
    public class CronService
    {
        private Timer _actionTimer;
        private const int FiveMinutes = 300000;

        public CronService(IServiceProvider services)
        {
            if(_actionTimer == null)
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
