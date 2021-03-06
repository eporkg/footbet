﻿using System.Collections.Generic;

namespace Footbet.Models.DomainModels
{
    public class Group
    {
        public Group()
        {
            Teams = new List<Team>();
            Games = new List<Game>();
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public int WinnerGameId { get; set; }
        public int RunnerUpGameId { get; set; }
        public int SportsEventId { get; set; }

        public virtual ICollection<Team> Teams { get; set; }
        public virtual ICollection<Game> Games { get; set; } 
        
    }
}