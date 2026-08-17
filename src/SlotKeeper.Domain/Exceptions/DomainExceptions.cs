namespace SlotKeeper.Domain.Exceptions;

public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
}

public class ConflictException : DomainException
{
    public ConflictException(string message) : base(message) { }
}

public class BookingConflictException : DomainException
{
    public BookingConflictException(string message) : base(message) { }
}

public class BookingLimitExceededException : DomainException
{
    public BookingLimitExceededException(string message) : base(message) { }
}

public class EntityNotFoundException : DomainException
{
    public EntityNotFoundException(string message) : base(message) { }
}

public class InvalidBookingWindowException : DomainException
{
    public InvalidBookingWindowException(string message) : base(message) { }
}

public class InvalidCredentialsException : DomainException
{
    public InvalidCredentialsException(string message) : base(message) { }
}
