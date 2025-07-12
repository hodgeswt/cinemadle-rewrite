namespace Cinemadle.Exceptions;

public class ObjectInstantationException : Exception
{
    private static readonly string _messageTemplate = "An error occurred instantiating {0}";

    public ObjectInstantationException()
    {
    }

    public ObjectInstantationException(string typeName) : base(string.Format(_messageTemplate, typeName))
    {
    }
}
