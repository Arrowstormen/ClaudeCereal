using ClaudeCereal.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ClaudeCereal.Tests.Helpers;

/// <summary>
/// Manages a single SQLite in-memory connection for the duration of a test class.
/// The connection is kept open so the in-memory database survives across multiple
/// <see cref="AppDbContext"/> instances that share the same connection.
/// Dispose when done to release the underlying connection.
/// </summary>
public sealed class SqliteDbContextFactory : IDisposable
{
    private readonly SqliteConnection _connection;

    public SqliteDbContextFactory()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        // Create the schema once; subsequent AppDbContext instances reuse the connection.
        using var ctx = CreateContext();
        ctx.Database.EnsureCreated();
    }

    /// <summary>
    /// Creates a new <see cref="AppDbContext"/> using the shared in-memory connection.
    /// No interceptors are attached — suitable for service unit tests.
    /// </summary>
    public AppDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options);

    /// <summary>
    /// Creates a new <see cref="AppDbContext"/> with the supplied interceptor attached.
    /// Use this overload for interceptor-level tests.
    /// </summary>
    public AppDbContext CreateContextWithInterceptor(AuditInterceptor interceptor) =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(interceptor)
            .Options);

    public void Dispose() => _connection.Dispose();
}
