using Marten;
using Marten.Events.Projections;

public class UserProjection : EventProjection
{
    public UserProjection()
    {
        ProjectAsync<UserCreated>(async (@event, ops) =>
        {
            await using var session = ops.DocumentStore.LightweightSession();
            var user = new UserSummaryView
            {
                Id = @event.Id,
                FirstName = @event.FirstName,
                LastName = @event.LastName,
                Created = @event.Created,
            };

            session.Store(user);
            await session.SaveChangesAsync();
        });

        ProjectAsync<UserNameChanged>(async (@event, ops) =>
        {
            await using var session = ops.DocumentStore.LightweightSession();

            var item = await session.Query<UserSummaryView>().FirstOrDefaultAsync(x => x.Id == @event.Id);

            if(item == null)
                return;

            item.FirstName = @event.FirstName;
            item.LastName = @event.LastName;
            item.ModificationDate = @event.ModifiacationDate;

            session.Store(item);
            await session.SaveChangesAsync();
        });
    }
}