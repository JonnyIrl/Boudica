using Boudica.MongoDB.Models;
using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.Classes
{
    public class LastActivityUser
    {
        public string Username { get; set; }
        public Raid Raid { get; set; }
        public Fireteam Firetam { get; set; }
        public DateTime LastActivityDateTime { get; set; }
        public IReadOnlyCollection<IRole> Roles { get; set; }
        
        public LastActivityUser(string username, Raid raid, Fireteam fireteam, IReadOnlyCollection<IRole> roles)
        {
            Username = username;
            Raid = raid;
            Firetam = fireteam;
            Roles = roles;
            AssignDateTime();
        }

        public void AssignDateTime()
        {
            if (Raid == null && Firetam == null)
            {
                LastActivityDateTime = DateTime.MinValue;
            }
            else if (Raid != null && Firetam == null)
            {
                if (Raid.DateTimeClosed == DateTime.MinValue)
                {
                    LastActivityDateTime = Raid.DateTimeCreated;
                }
                else
                {
                    LastActivityDateTime = Raid.DateTimeClosed;
                }
            }
            else
            {
                if (Firetam.DateTimeClosed == DateTime.MinValue)
                {
                    LastActivityDateTime = Firetam.DateTimeCreated;
                }
                else
                {
                    LastActivityDateTime = Firetam.DateTimeClosed;
                }
            }
        }

        public string GetLastActivityText()
        {
            if (Raid == null && Firetam == null)
            {
                return "Never";
            }
            else if (Raid != null && Firetam == null)
            {
                if (Raid.DateTimeClosed == DateTime.MinValue)
                {
                    return $"{(int)DateTime.UtcNow.Subtract(Raid.DateTimeCreated).TotalDays} Days ago";
                }
                else
                {
                    return $"{(int)DateTime.UtcNow.Subtract(Raid.DateTimeClosed).TotalDays} Days ago";
                }
            }
            else
            {
                if (Firetam.DateTimeClosed == DateTime.MinValue)
                {
                    return $"{(int)DateTime.UtcNow.Subtract(Firetam.DateTimeCreated).TotalDays} Days ago";
                }
                else
                {
                    return $"{(int)DateTime.UtcNow.Subtract(Firetam.DateTimeClosed).TotalDays} Days ago";
                }
            }
        }
    }
}
