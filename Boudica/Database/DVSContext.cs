using Boudica.Database.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.Database
{
    public class DVSContext: DbContext
    {
        private readonly IConfiguration _configuration;
        public DVSContext(DbContextOptions<DVSContext> options, IConfiguration configuration) : base(options)
        {
            _configuration = configuration;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var connectionStringBuilder = new SqlConnectionStringBuilder 
            { 
                DataSource = _configuration["DataSource"], 
                UserID = _configuration["UserID"], 
                Password = _configuration["Password"], 
                InitialCatalog= _configuration["InitialCatalog"]
            };

            var connectionString = connectionStringBuilder.ToString();
            var connection = new SqlConnection(connectionString);
            optionsBuilder.UseSqlServer(connection);
        }

        public DbSet<Guardian> Guardians { get; set; }
        public DbSet<Item> Items { get; set; }
        public DbSet<Eververse> EververseItems { get; set; }
        public DbSet<GuardianInventory> GuardiansInventory { get; set; }
        public DbSet<Raid> Raids { get; set; }
        public DbSet<Fireteam> Fireteams { get; set; }
        public DbSet<GuardianReputation> GuardianReputation { get; set; }
        public DbSet<Insult> Insult { get; set; }
        public DbSet<RaidGroup> RaidGroups { get; set; }
    }
}
