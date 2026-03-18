using TopSpeed.Localization;

namespace TopSpeed.Data
{
    public readonly struct TrackTsmIssue
    {
        public TrackTsmIssue(TrackTsmIssueSeverity severity, int lineNumber, string message)
        {
            Severity = severity;
            LineNumber = lineNumber;
            Message = message ?? string.Empty;
        }

        public TrackTsmIssueSeverity Severity { get; }
        public int LineNumber { get; }
        public string Message { get; }

        public override string ToString()
        {
            var severityLabel = Severity == TrackTsmIssueSeverity.Warning
                ? LocalizationService.Translate(LocalizationService.Mark("Warning"))
                : LocalizationService.Translate(LocalizationService.Mark("Error"));
            return LineNumber > 0
                ? severityLabel + " (line " + LineNumber + "): " + Message
                : severityLabel + ": " + Message;
        }
    }
}
