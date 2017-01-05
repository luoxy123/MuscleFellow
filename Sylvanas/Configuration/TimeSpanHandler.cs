namespace Sylvanas.Configuration
{
    public enum TimeSpanHandler
    {
        /// <summary>
        ///     Uses the xsd format like PT15H10M20S
        /// </summary>
        DurationFormat,

        /// <summary>
        ///     Uses the standard .net ToString method of the TimeSpan class
        /// </summary>
        StandardFormat
    }
}