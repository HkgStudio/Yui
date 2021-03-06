using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using LiteDB;
using Yui.Entities.Commands;
using Yui.Entities.Database;
using Yui.Extensions;

namespace Yui.Modules.ModerationCommands
{
    public class GuildUtilities : CommandModule
    {
        public GuildUtilities(SharedData data, Random random, HttpClient http, Api.Imgur.Client client) : base(data, random, http, client)
        {
        }

        [Command("roles"), Cooldown(1, 10, CooldownBucketType.User), RequireGuild]
        public async Task GetRoles(CommandContext ctx)
        {
            var trans = ctx.Guild.GetTranslation(Data);
            var embed = new DiscordEmbedBuilder
            {
                Title = trans.AllTheRolesText
            };
            foreach (var role in ctx.Guild.Roles)
            {
                embed.AddField(role.Name, role.Id.ToString(), true);
            }

            await ctx.RespondAsync(embed: embed);
        }

        [Command("modrole"), Cooldown(1, 10, CooldownBucketType.Guild), RequireGuild]
        public async Task SetModRole(CommandContext ctx, DiscordRole modRole = null)
        {
            if (!IsAdmin(ctx))
                return;
            using (var db = new LiteDatabase("Data.db"))
            {
                var guilds = db.GetCollection<Guild>();
                var guild = guilds.FindOne(x => x.Id == ctx.Guild.Id);
                guild.ModRole = modRole == null ? 0 : modRole.Id;
                guilds.Update(guild);
            }
            var trans = ctx.Guild.GetTranslation(Data);
            var text = trans.SetModRoleText.Replace("{{roleName}}", modRole.Name);
            await ctx.RespondAsync(text);
        }

        [Command("nightwatch"), Cooldown(1, 10, CooldownBucketType.Guild), RequireGuild]
        public async Task SetNightwatch(CommandContext ctx, bool set)
        {
            if (!IsAdmin(ctx))
                return;
            using (var db = new LiteDatabase("Data.db"))
            {
                var guilds = db.GetCollection<Guild>();
                var guild = guilds.FindOne(x => x.Id == ctx.Guild.Id);
                guild.NightWatchEnabled = set;
                guilds.Update(guild);
            }

            var txt = set ? "enabled" : "disabled";
            await ctx.RespondAsync($"Nightwatch is now {txt}!");
        }
        [Command("lang"), Cooldown(1, 10, CooldownBucketType.Guild), RequireGuild]
        public async Task SetLangAsync(CommandContext ctx, Guild.Languages lang = Guild.Languages.EN)
        {
            if (!IsAdmin(ctx))
                return;
            using (var db = new LiteDatabase("Data.db"))
            {
                var guilds = db.GetCollection<Guild>();
                var guild = guilds.FindOne(x => x.Id == ctx.Guild.Id);
                guild.Lang = lang;
                guilds.Update(guild);
            }
            var trans = ctx.Guild.GetTranslation(Data);
            var text = trans.SetLanguageText.Replace("{{langFlag}}", trans.LangFlagText).Replace("{{langJoke}}", trans.LangJokeText);
            await ctx.RespondAsync(text);
        }

        [Command("prefix"), Cooldown(1, 10, CooldownBucketType.Guild), RequireGuild]
        public async Task SetPrefixAsync(CommandContext ctx, string prefix)
        {
            if (!IsAdmin(ctx))
                return;
            if (string.IsNullOrWhiteSpace(prefix))
                return;
            using (var db = new LiteDatabase("Data.db"))
            {
                var guilds = db.GetCollection<Guild>();
                var guild = guilds.FindOne(x => x.Id == ctx.Guild.Id);
                guild.Prefix = prefix;
                guilds.Update(guild);
            }
            var trans = ctx.Guild.GetTranslation(Data);
            var text = trans.SetPrefixText.Replace("{{prefix}}", prefix);
            await ctx.RespondAsync(text);
        }
        [Command("clear"), Aliases("purge"), Cooldown(1, 1, CooldownBucketType.Channel), RequireGuild, RequireBotPermissions(Permissions.ManageMessages)]
        public async Task ClearMessages(CommandContext ctx, int amount)
        {
            if (!IsAdmin(ctx))
                return;
            var trans = ctx.Guild.GetTranslation(Data);

            if (amount < 1 || amount > 100)
            {
                await ctx.RespondAsync(trans.ClearCommandOutOfBoundariesText);
                return;
            }

            var messages = new List<DiscordMessage>(
                (await ctx.Channel.GetMessagesAsync(amount)).Where(x =>
                    (DateTime.Now - x.CreationTimestamp).Days < 14));
            await ctx.Channel.DeleteMessagesAsync(messages);
            var msg = await ctx.RespondAsync(trans.ClearCommandDone.Replace("{{messagesCounts}}", messages.Count().ToString()));
            await Task.Delay(5000);
            await msg.DeleteAsync();
        }
        [Command("autorole"), Cooldown(1, 1, CooldownBucketType.Channel), RequireGuild, RequireBotPermissions(Permissions.ManageMessages)]
        public async Task SetAutoRole(CommandContext ctx, DiscordRole role = null)
        {
            if (!IsAdmin(ctx))
                return;
            using (var db = new LiteDatabase("Data.db"))
            {
                var guilds = db.GetCollection<Guild>();
                var guild = guilds.FindOne(x => x.Id == ctx.Guild.Id);
                guild.AutoRole = role == null ? 0 : role.Id;
                guilds.Update(guild);
            }
            var trans = ctx.Guild.GetTranslation(Data);
            if (role == null)
            {
                await ctx.RespondAsync(trans.SetAutoRoleNoRoleText);
                return;
            }
            var text = trans.SetAutoRoleText.Replace("{{role}}", role.Name);
            await ctx.RespondAsync(text);
        }
        internal static bool IsAdmin(CommandContext ctx)
        {
            if (ctx.Member.IsOwner)
                return true;
            if (ctx.Member.Roles.Any(role => role.Permissions.HasPermission(Permissions.Administrator)))
                return true;
            using (var db = new LiteDatabase("Data.db"))
            {
                var guilds = db.GetCollection<Guild>();
                var guild = guilds.FindOne(x => x.Id == ctx.Guild.Id);
                if (guild.ModRole == 0) return false;
                if (ctx.Member.Roles.First(x => x.Id == guild.ModRole) != null)
                    return true;
            }
            return false;
        }
    }
}