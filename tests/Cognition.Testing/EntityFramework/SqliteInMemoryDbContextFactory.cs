using System.Diagnostics.CodeAnalysis;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cognition.Testing.EntityFramework;

/// <summary>
/// Provides helpers for constructing EF Core DbContext instances with test-friendly providers.
/// </summary>
public static class TestDbContextFactory
{
    public static TestDbContextHandle<TContext> CreateSqliteInMemory<TContext>(
        Action<DbContextOptionsBuilder<TContext>>? configureOptions = null,
        Action<TContext>? seed = null)
        where TContext : DbContext
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var optionsBuilder = new DbContextOptionsBuilder<TContext>()
            .UseSqlite(connection);
        configureOptions?.Invoke(optionsBuilder);

        var context = CreateContextInstance(optionsBuilder.Options);

        seed?.Invoke(context);
        context.SaveChanges();

        return new TestDbContextHandle<TContext>(context, connection);
    }

    public static async Task<TestDbContextHandle<TContext>> CreateSqliteInMemoryAsync<TContext>(
        Action<DbContextOptionsBuilder<TContext>>? configureOptions = null,
        Func<TContext, Task>? seed = null,
        CancellationToken cancellationToken = default)
        where TContext : DbContext
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var optionsBuilder = new DbContextOptionsBuilder<TContext>()
            .UseSqlite(connection);
        configureOptions?.Invoke(optionsBuilder);

        var context = CreateContextInstance(optionsBuilder.Options);

        if (seed != null)
        {
            await seed(context).ConfigureAwait(false);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return new TestDbContextHandle<TContext>(context, connection);
    }

    public static TestDbContextHandle<TContext> CreateInMemory<TContext>(
        string? databaseName = null,
        Action<DbContextOptionsBuilder<TContext>>? configureOptions = null,
        Action<TContext>? seed = null)
        where TContext : DbContext
    {
        databaseName ??= Guid.NewGuid().ToString("N");

        var optionsBuilder = new DbContextOptionsBuilder<TContext>()
            .UseInMemoryDatabase(databaseName);
        configureOptions?.Invoke(optionsBuilder);

        var context = CreateContextInstance(optionsBuilder.Options);

        seed?.Invoke(context);
        context.SaveChanges();

        return new TestDbContextHandle<TContext>(context, connection: null);
    }

    public static async Task<TestDbContextHandle<TContext>> CreateInMemoryAsync<TContext>(
        string? databaseName = null,
        Action<DbContextOptionsBuilder<TContext>>? configureOptions = null,
        Func<TContext, Task>? seed = null)
        where TContext : DbContext
    {
        databaseName ??= Guid.NewGuid().ToString("N");

        var optionsBuilder = new DbContextOptionsBuilder<TContext>()
            .UseInMemoryDatabase(databaseName);
        configureOptions?.Invoke(optionsBuilder);

        var context = CreateContextInstance(optionsBuilder.Options);

        if (seed != null)
        {
            await seed(context).ConfigureAwait(false);
            await context.SaveChangesAsync().ConfigureAwait(false);
        }

        return new TestDbContextHandle<TContext>(context, connection: null);
    }

    [return: NotNull]
    private static TContext CreateContextInstance<TContext>(DbContextOptions<TContext> options)
        where TContext : DbContext
    {
        var context = Activator.CreateInstance(typeof(TContext), options) as TContext;
        if (context is null)
        {
            throw new InvalidOperationException($"Unable to construct DbContext of type {typeof(TContext).FullName}. Ensure it exposes a constructor accepting DbContextOptions.");
        }

        return context;
    }
}

public sealed class TestDbContextHandle<TContext> : IAsyncDisposable, IDisposable
    where TContext : DbContext
{
    private readonly SqliteConnection? _connection;
    private bool _disposed;

    public TestDbContextHandle(TContext context, SqliteConnection? connection)
    {
        Context = context;
        _connection = connection;
    }

    public TContext Context { get; }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Context.Dispose();
        _connection?.Dispose();
        _disposed = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        await Context.DisposeAsync().ConfigureAwait(false);
        if (_connection is not null)
        {
            await _connection.DisposeAsync().ConfigureAwait(false);
        }

        _disposed = true;
    }
}
