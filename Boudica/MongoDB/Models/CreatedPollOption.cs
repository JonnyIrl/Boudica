﻿using Boudica.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.MongoDB.Models
{
    public class CreatedPollOption
    {
        public string DisplayText { get; set; }
        public PollOption PollOption { get; set; }
    }
}
