using CS2_Echo.Domain;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace CS2_Echo.Infrastructure;

public class ChatParser
{

    // top tier regex
    // ^
    // (?<datetime>\d{2}\/\d{2} \d{2}:\d{2}:\d{2}) - datetime - 03/17 15:28:27
    // \s{2}\[ - format for all chat logs
    // (?<channel>ALL|T|CT) - channel type (ALL, T, CT) ([\w+] for other languages?) - [ALL]
    // \]\s+
    // (?<name>.*?) - player name - Player1
    // (?:﹫(?<location>[^:]*?))? - optional location part if there is ﹫ - Location1
    // (?:\s+\[(?<status>DEAD)\])? - optional status part if there is [DEAD] (maybe [\w+] is enough for other languages?) - DEAD
    // :\s+
    // (?<message>.*) - message
    // $
    private static readonly Regex ChatRegex = new Regex(
        @"^(?<datetime>\d{2}\/\d{2} \d{2}:\d{2}:\d{2})\s{2}\[(?<channel>ALL|T|CT)\]\s+(?<name>.*?)(?:﹫(?<location>[^:]*?))?(?:\s+\[(?<status>DEAD)\])?:\s+(?<message>.*)$",
        RegexOptions.Compiled);


    public ChatMessage? ParseLine(string logLine)
    {
        if (!logLine.Contains("  ["))
            return null;

        var match = ChatRegex.Match(logLine);

        if (match.Success)
        {
            return new ChatMessage(
                Timestamp: match.Groups["datetime"].Value,
                Channel: match.Groups["channel"].Value,
                PlayerName: match.Groups["name"].Value.Replace("\u200E", "").Trim(),
                Location: match.Groups["location"].Success ? match.Groups["location"].Value.Trim() : null,
                IsDead: match.Groups["status"].Success,
                Message: match.Groups["message"].Value.Trim()
            );

        }
        return null;


    }
}
