using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DSharpPlus;
using Fox.Databases.Entities.Models;
using Fox.Services;

namespace Fox.Databases.Migrations
{
    public sealed class MigrationHelperService
    {
        private readonly FoxDb _db;
        private readonly LogService _logger;
        
        public MigrationHelperService(FoxDb db, LogService logger)
        {
            _db = db;
            _logger = logger;
        }

        public Task MigrateCustomCommandsAsync()
        {
            if (File.Exists("databases/customcommands"))
            {
                _logger.Print(LogLevel.Debug, "Database Migration", "Migration of CustomCommands is already done.");
                return Task.CompletedTask;
            }

            _logger.Print(LogLevel.Debug, "Database Migration", "Migrating CustomCommands property on every GuildEntity.");

            var entities = _db.GuildManager.GetAll();

            foreach (var entity in entities)
            {
                if (entity.CustomCommands != null)
                {
                    continue;
                }

                entity.CustomCommands = new List<CustomCommand>();
                _db.GuildManager.Update(entity);
            }

            var f = File.Create("databases/customcommands");
            f.Close();

            return Task.CompletedTask;
        }
    }
}
