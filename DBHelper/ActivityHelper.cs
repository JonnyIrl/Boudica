using Boudica.MongoDB;
using Boudica.MongoDB.Models;
using Boudica.Services;
using DBHelper.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBHelper
{
    public class ActivityHelper
    {

        private List<GuardianStats> _guardianStats = new List<GuardianStats>();
        private List<Raid> _season20Raids = new List<Raid>();
        private List<Fireteam> _season20Fireteams = new List<Fireteam>();
        private readonly ActivityService _activityService;
        private readonly HistoryService _historyService;
        private readonly GuardianService _guardianService;

        public List<GuardianStats> GuardianStats => _guardianStats;
        
        public ActivityHelper(IMongoDBContext context)
        {
            _activityService = new ActivityService(context);
            _historyService = new HistoryService(context);
            _guardianService = new GuardianService(context);
        }


        public List<GuardianStats> GetFireteamStats(DateTime from, DateTime to)
        {
            _season20Fireteams = _activityService.GetAllFireteams(from, to).Result;
            foreach(Fireteam fireteam in _season20Fireteams)
            {
                foreach(ActivityUser activityUser in fireteam.Players)
                {
                    if(_guardianStats.Exists(x => x.Id == activityUser.UserId) == false)
                    {
                        _guardianStats.Add(new GuardianStats() { Id = activityUser.UserId, Username = activityUser.DisplayName});
                    }

                    GuardianStats guardianStats = _guardianStats.First(x => x.Id == activityUser.UserId);
                    if(fireteam.AwardedGlimmer)
                    {
                        guardianStats.CompletedFireteamCount++;
                    }
                    else
                    {
                        guardianStats.FailedFireteamCount++;
                    }
                }
            }
            return _guardianStats;
        }
        public List<GuardianStats> GetRaidStats(DateTime from, DateTime to)
        {
            _season20Raids = _activityService.GetAllRaids(from, to).Result;
            foreach (Raid raid in _season20Raids)
            {
                foreach (ActivityUser activityUser in raid.Players)
                {
                    if (_guardianStats.Exists(x => x.Id == activityUser.UserId) == false)
                    {
                        _guardianStats.Add(new GuardianStats() { Id = activityUser.UserId, Username = activityUser.DisplayName });
                    }

                    GuardianStats guardianStats = _guardianStats.First(x => x.Id == activityUser.UserId);
                    if (raid.AwardedGlimmer)
                    {
                        guardianStats.CompletedRaidCount++;
                    }
                    if(raid.AwardedGlimmer == false)
                    {
                        guardianStats.FailedRaidCount++;
                    }
                    if(raid.Players.Count != raid.MaxPlayerCount)
                    {
                        guardianStats.FailedRaidCount++;
                    }
                }
            }
            return _guardianStats;
        }
        public List<GuardianStats> GetHistoryDetails(DateTime from, DateTime to)
        {
            List<HistoryRecord> historyRecords = _historyService.GetAllHistoryRecordsAsync(from, to).Result;
            foreach(HistoryRecord historyRecord in historyRecords)
            {
                if (_guardianStats.Exists(x => x.Id == historyRecord.UserId) == false)
                {
                    _guardianStats.Add(new GuardianStats() { Id = historyRecord.UserId});
                }
                if (historyRecord.TargetUserId != null && _guardianStats.Exists(x => x.Id == historyRecord.TargetUserId) == false)
                {
                    _guardianStats.Add(new GuardianStats() { Id = (ulong) historyRecord.TargetUserId });
                }

                GuardianStats guardianStats = _guardianStats.First(x => x.Id == historyRecord.UserId);
                GuardianStats targetedGuardianStats = historyRecord.TargetUserId != null ? _guardianStats.FirstOrDefault(x => x.Id == historyRecord.TargetUserId) : null;
                switch (historyRecord.HistoryType)
                {
                    case Boudica.Enums.HistoryType.Insult:
                        guardianStats.InsultIssuedCount++;
                        if(targetedGuardianStats != null) 
                            targetedGuardianStats.InsultReceivedCount++;
                        break;
                    case Boudica.Enums.HistoryType.Award:
                        guardianStats.AwardsIssuedCount++;
                        if (targetedGuardianStats != null)
                            targetedGuardianStats.AwardsReceivedCount++;
                        break;
                    case Boudica.Enums.HistoryType.SuperSub:
                        if (targetedGuardianStats != null)
                            targetedGuardianStats.SuperSubCount++;
                        break;
                    case Boudica.Enums.HistoryType.Compliment:
                        guardianStats.ComplimentIssuedCount++;
                        if (targetedGuardianStats != null)
                            targetedGuardianStats.ComplimentReceivedCount++;
                        break;
                    case Boudica.Enums.HistoryType.AddPlayer:
                        if (targetedGuardianStats != null)
                            targetedGuardianStats.AddedToActivityCount++;
                        break;
                    case Boudica.Enums.HistoryType.DailyGift:
                        guardianStats.DailyGiftCount++;
                        break;
                    case Boudica.Enums.HistoryType.TrialsVote:
                        guardianStats.TrialsVoteCount++;
                        break;
                    case Boudica.Enums.HistoryType.UserChallenge:
                        break;
                    case Boudica.Enums.HistoryType.BotChallengeHigherOrLower:
                        guardianStats.HigherOrLowerCount++;
                        break;
                    default:
                        break;
                }
            }

            return _guardianStats;
        }
        public Guardian GetGuardian(ulong id)
        {
            return _guardianService.GetGuardian(id).Result;
        }
    }
}
