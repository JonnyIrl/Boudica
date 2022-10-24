using Boudica.Enums;
using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.MongoDB.Models
{
    public class PlayerVote
    {
        public ulong Id { get; set; }
        public string Username { get; set; }
        public string VotedEmoteName { get; set; }
        public DateTime DateTimeVoted { get; set; }
        public TrialsMap TrialsMap { get; set; }
    }
}
