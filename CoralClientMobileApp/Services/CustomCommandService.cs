using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CoralClientMobileApp.Model;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using CoralClientMobileApp.DbContext;

namespace CoralClientMobileApp.Services
{
    public interface ICustomCommandService
    {
        Task<IEnumerable<CustomCommand>> GetCommandsForServerAsync(Guid serverProfileId);
        Task<IEnumerable<CustomCommand>> GetCommandsByTargetAsync(Guid serverProfileId, CommandTarget target);
        Task<CustomCommand> SaveCommandAsync(CustomCommand command);
        Task DeleteCommandAsync(CustomCommand command);
        Task<CustomCommand?> GetCommandByIdAsync(Guid commandId);
    }

    public class CustomCommandService : ICustomCommandService
    {
        private readonly ServerProfileContext _context;
        private readonly ILogger<CustomCommandService> _logger;

        public CustomCommandService(ServerProfileContext context, ILogger<CustomCommandService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<CustomCommand>> GetCommandsForServerAsync(Guid serverProfileId)
        {
            try
            {
                _logger.LogDebug("Getting custom commands for server profile: {ServerProfileId}", serverProfileId);
                return await _context.CustomCommands
                    .Where(c => c.ServerProfileId == serverProfileId)
                    .OrderBy(c => c.Target)
                    .ThenBy(c => c.Category)
                    .ThenBy(c => c.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get custom commands for server: {ServerProfileId}", serverProfileId);
                return new List<CustomCommand>();
            }
        }

        public async Task<IEnumerable<CustomCommand>> GetCommandsByTargetAsync(Guid serverProfileId, CommandTarget target)
        {
            try
            {
                _logger.LogDebug("Getting custom commands for server: {ServerProfileId}, target: {Target}", serverProfileId, target);
                return await _context.CustomCommands
                    .Where(c => c.ServerProfileId == serverProfileId && c.Target == target)
                    .OrderBy(c => c.Category)
                    .ThenBy(c => c.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get custom commands by target for server: {ServerProfileId}", serverProfileId);
                return new List<CustomCommand>();
            }
        }

        public async Task<CustomCommand> SaveCommandAsync(CustomCommand command)
        {
            try
            {
                var existingCommand = await _context.CustomCommands
                    .FirstOrDefaultAsync(c => c.Id == command.Id);

                if (existingCommand != null)
                {
                    // Update existing command
                    existingCommand.Name = command.Name;
                    existingCommand.Description = command.Description;
                    existingCommand.Command = command.Command;
                    existingCommand.Category = command.Category;
                    existingCommand.RequiresPlayerName = command.RequiresPlayerName;
                    existingCommand.Target = command.Target;
                    existingCommand.ServerProfileId = command.ServerProfileId;
                    
                    _logger.LogInformation("Updating custom command: {CommandName} (ID: {CommandId})", command.Name, command.Id);
                }
                else
                {
                    // Add new command
                    _context.CustomCommands.Add(command);
                    _logger.LogInformation("Adding new custom command: {CommandName} (ID: {CommandId})", command.Name, command.Id);
                }

                await _context.SaveChangesAsync();
                return command;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save custom command: {CommandName}", command.Name);
                throw;
            }
        }

        public async Task DeleteCommandAsync(CustomCommand command)
        {
            try
            {
                var existingCommand = await _context.CustomCommands
                    .FirstOrDefaultAsync(c => c.Id == command.Id);

                if (existingCommand != null)
                {
                    _context.CustomCommands.Remove(existingCommand);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Deleted custom command: {CommandName} (ID: {CommandId})", command.Name, command.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete custom command: {CommandName}", command.Name);
                throw;
            }
        }

        public async Task<CustomCommand?> GetCommandByIdAsync(Guid commandId)
        {
            try
            {
                return await _context.CustomCommands
                    .FirstOrDefaultAsync(c => c.Id == commandId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get custom command by ID: {CommandId}", commandId);
                return null;
            }
        }
    }
}