namespace TopSpeed.Localization
{
    internal interface ITextLocalizer
    {
        string Translate(string messageId);
        string Translate(string context, string messageId);
    }
}
