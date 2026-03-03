namespace NextTurn.Domain.Common;


// making a custom exception which makes it easier to catch these errors later, wink (thank me later)
public class DomainException : Exception {
    public DomainException(string message) : base(message) { }
}
