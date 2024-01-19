using Newtonsoft.Json;

public class User
{
    private readonly Guid _id;
    public Guid Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }

    private readonly List<object> _eventsToCommit = new();

    [JsonIgnore]
    public IEnumerable<object> EventsToCommit => _eventsToCommit;
    public void ClearEventsOnceCommitted()
    {
        _eventsToCommit.Clear();
    }

    public long Version { get; set; }

    public User(){}

    public User(string firstName, string lastName)
    {
        _id = Id = Guid.NewGuid();
        FirstName = firstName;
        LastName = lastName;

        // create the event
        var userCreatedEvent = new UserRegistered
        {
            Id = Id,
            FirstName = FirstName,
            LastName = LastName,
            Created = DateTime.UtcNow
        };
        Version++;
        _eventsToCommit.Add(userCreatedEvent);
    }

    public User(UserRegistered src)
    {
        Id = src.Id;
        FirstName = src.FirstName;
        LastName = src.LastName;
        Version++;
    }

    public void ChangeName(string firstName, string lastName)
    {
        var userChangedNameEvent = new UserNameChanged
        {
            Id = Id,
            FirstName = string.Equals(FirstName.ToLower(), 
                        firstName.ToLower(), 
                        StringComparison.InvariantCultureIgnoreCase) ? FirstName : firstName,
            LastName = string.Equals(LastName, 
                            lastName, 
                            StringComparison.CurrentCultureIgnoreCase) ? LastName : lastName
        };

        Apply(userChangedNameEvent);
        _eventsToCommit.Add(userChangedNameEvent);
    }

    private void Apply(UserNameChanged @event)
    {
        FirstName = @event.FirstName;
        LastName = @event.LastName;
        @event.ModifiacationDate = DateTime.UtcNow;
        Version++;
    }

    
}