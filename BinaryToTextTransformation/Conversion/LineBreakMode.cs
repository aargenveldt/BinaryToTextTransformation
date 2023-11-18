namespace de.Aargenveldt.BinaryToTextTransformation.Conversion
{
    /// <summary>
    /// Kind of line breaks.
    /// </summary>
    public enum LineBreakMode
    {
        /// <summary>
        /// No line breaking
        /// </summary>
        None = 0,

        /// <summary>
        /// CarriageReturn only
        /// </summary>
        CR,

        /// <summary>
        /// Newline only (Unix like)
        /// </summary>
        NL,

        /// <summary>
        /// CarriageReturn + Newline (Windows)
        /// </summary>
        CRNL,

        /// <summary>
        /// Newline + CarriageReturn (very uncommon)
        /// </summary>
        NLCR,

        /// <summary>
        /// As defined by current environment
        /// </summary>
        Environment,

        /// <summary>
        /// Custom line break provided by caller
        /// </summary>
        Custom

    }
}
