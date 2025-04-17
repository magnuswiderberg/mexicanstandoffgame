namespace Shared.BotPlay
{
    public class ExternalBot
    {
        /// <summary>
        /// Id of the bot
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Type of bot
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Description of the bot
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Name of the bot
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// The url where we interact with the bot
        /// </summary>
        public string ActionUrl { get; set; }
    }
}
