namespace TopSpeed.Localization
{
    internal sealed class PassthroughLocalizer : ITextLocalizer
    {
        public static readonly PassthroughLocalizer Instance = new PassthroughLocalizer();

        private PassthroughLocalizer()
        {
        }

        public string Translate(string messageId)
        {
            return messageId;
        }

        public string Translate(string context, string messageId)
        {
            return messageId;
        }
    }
}
