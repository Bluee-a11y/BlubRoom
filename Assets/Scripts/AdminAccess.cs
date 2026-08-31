using System.Collections.Generic;

namespace ClubhousePC
{
    public static class AdminAccess
    {
        // Paste Unity Authentication Player IDs between the quotes.
        // Example: "abcdef12-3456-7890-abcd-ef1234567890",
        private static readonly HashSet<string> AllowedPlayerIds = new()
        {
            "rzOfGXMzMi31pjvoQHYPBkjdGUra",
            "EEvhUpV6XXfR6hKJW5oong85kfIH",
            "s6mm0LaYgDQiuFaTtPZIa0lVxLga",
        };

        public static bool IsAllowed(string playerId) =>
            !string.IsNullOrWhiteSpace(playerId) && AllowedPlayerIds.Contains(playerId);
    }
}
