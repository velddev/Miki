﻿using Miki.Discord.Common;
using Miki.Framework;
using Miki.Framework.Commands;
using Miki.Framework.Commands.Pipelines;
using System;
using System.Threading.Tasks;
using Miki.Discord.Common.Packets.API;
using Miki.Localization;
using Miki.Modules.CustomCommands.Services;
using Miki.Utility;
using MiScript.Exceptions;

namespace Miki.Modules.CustomCommands.CommandHandlers
{
    public class CustomCommandsHandler : IPipelineStage
    {
        public async ValueTask CheckAsync(IDiscordMessage data, IMutableContext e, Func<ValueTask> next)
        {
            if (e == null)
            {
                return;
            }   

            if (e.GetMessage().Type != DiscordMessageType.DEFAULT)
            {
                await next();
                return;
            }

            var service = e.GetService<ICustomCommandsService>();
            var startIndex = e.GetPrefixMatch().Length;
            var message = e.GetMessage().Content;
            var endIndex = message.IndexOf(' ', startIndex);
            var commandName = endIndex == -1
                ? message.Substring(startIndex)
                : message.Substring(startIndex, endIndex - startIndex);

            try
            {
                var success = await service.ExecuteAsync(e, commandName);

                if (!success)
                {
                    await next();
                }
            }
            catch (MiScriptLimitException ex)
            {
                var type = ex.Type switch
                {
                    LimitType.Instructions => "instructions",
                    LimitType.Stack => "function calls",
                    LimitType.ArrayItems => "array items",
                    LimitType.ObjectItems => "object items",
                    LimitType.StringLength => "string size",
                    _ => throw new ArgumentOutOfRangeException()
                };

                await e.ErrorEmbedResource("user_error_miscript_limit", type)
                    .ToEmbed().QueueAsync(e, e.GetChannel());
            }
            catch (UserMiScriptException ex)
            {
                await e.ErrorEmbedResource("user_error_miscript_execute")
                    .AddCodeBlock(ex.Value)
                    .ToEmbed().QueueAsync(e, e.GetChannel());
            }
            catch (MiScriptException ex)
            {
                await e.ErrorEmbedResource("error_miscript_execute")
                    .AddCodeBlock(ex.Message)
                    .ToEmbed().QueueAsync(e, e.GetChannel());
            }
        }
    }
}
