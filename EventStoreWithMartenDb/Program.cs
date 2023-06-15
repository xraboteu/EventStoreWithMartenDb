using Marten;
using Marten.Events;
using Marten.Events.Projections;
using Marten.Linq.SoftDeletes;
using Marten.Services.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Weasel.Core;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();

builder.Services.AddMarten(options =>
{
    // Establish the connection string to your Marten database
    options.Connection(builder.Configuration.GetConnectionString("Marten")!);

    // If we're running in development mode, let Marten just take care
    // of all necessary schema building and patching behind the scenes
    options.AutoCreateSchemaObjects = AutoCreate.All;
    options.Events.StreamIdentity = StreamIdentity.AsGuid;
    options.UseDefaultSerialization(enumStorage: EnumStorage.AsString,
        serializerType: SerializerType.SystemTextJson);

    options.Schema.For<UserSummaryView>()
        .Duplicate(x => x.LastName, pgType: "varchar(250)",
            notNull: false);

    options.Schema.For<UserSummaryView>()
        .Duplicate(x => x.FirstName, pgType: "varchar(250)",
            notNull: false);

    options.Projections.Add<UserProjection>(ProjectionLifecycle.Inline);
    options.Events.AddEventType(typeof(UserCreated));
    options.Events.AddEventType(typeof(UserNameChanged));

    options.Logger(new ConsoleMartenLogger());

});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapPost("/add", async (CreateUserRequest create, [FromServices] IDocumentSession session) =>
{
    var user = new User(create.FirstName, create.LastName);
    var events = user.EventsToCommit.ToArray();
    
    session.Events.Append(user.Id, user.Version, events);
    
    session.Store(user);
    await session.SaveChangesAsync();
    user.ClearEventsOnceCommitted();
});

app.MapGet("/users-projection", async ([FromServices] IQuerySession session, CancellationToken cancellationToken) =>
{
    return await session.Query<UserSummaryView>()
        .OrderBy(_ => _.LastName)
        .ToListAsync(cancellationToken);
});

app.MapGet("/users-aggregates", async ([FromServices] IDocumentSession session, CancellationToken cancellationToken) =>
{
    var users = await session.Query<User>().ToListAsync();
    return users;
});



app.MapGet("/user", async ([FromServices] IDocumentSession session, Guid userId, CancellationToken ct) =>
{
    return await session.Events.AggregateStreamAsync<User>(userId,token: ct);
});

app.MapPost("/update", async (UpdateUserRequest updatedUser, [FromServices] IDocumentSession session, CancellationToken cancellationToken) =>
{
    var user = await session.Events.AggregateStreamAsync<User>(updatedUser.Id, token: cancellationToken);

    user?.ChangeName(updatedUser.FirstName, updatedUser.LastName);

    var events = user?.EventsToCommit.ToArray();
    session.Events.Append(user.Id, user.Version, events);
    session.Store(user);

    await session.SaveChangesAsync(cancellationToken);
    user.ClearEventsOnceCommitted();
});

app.MapGet("/user-events", async ([FromServices] IDocumentSession session, Guid userId, CancellationToken cancellationToken) =>
{
    var items = await session.Events.FetchStreamAsync(userId, token: cancellationToken);
    return items.Select(_ => new
    {
        _.Data,
        _.Version,
    }).ToArray();
});

//TODO: Add a route to update user phone number

//TODO: Add a route to delete a user

app.Run();