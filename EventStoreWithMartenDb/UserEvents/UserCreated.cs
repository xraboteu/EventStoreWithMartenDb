public class UserCreated
{
    public Guid Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public DateTime Created { get; set; } = DateTime.Now;
}