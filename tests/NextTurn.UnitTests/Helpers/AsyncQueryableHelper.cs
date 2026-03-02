using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;
using Moq;

namespace NextTurn.UnitTests.Helpers;

/// <summary>
/// Provides a factory method (<see cref="BuildMockDbSet{T}"/>) that creates a Moq
/// <see cref="Mock{T}"/> of <see cref="Microsoft.EntityFrameworkCore.DbSet{T}"/> backed
/// by an in-memory list.
///
/// <para>
/// EF Core's async LINQ extension methods (e.g. <c>FirstOrDefaultAsync</c>,
/// <c>ToListAsync</c>) rely on the <see cref="IAsyncQueryProvider"/> resolved from
/// the <c>DbSet</c>'s <c>Provider</c> property. When a plain <c>Mock&lt;DbSet&lt;T&gt;&gt;</c>
/// is used without this wiring, those calls throw <see cref="InvalidOperationException"/>
/// at runtime because the default mock provider is not async-capable.
/// </para>
///
/// <para>
/// This helper sets up the mock with a custom <see cref="TestAsyncQueryProvider{T}"/>
/// and a matching <see cref="TestAsyncEnumerator{T}"/>, making the DbSet behave like
/// a real EF Core in-memory set for LINQ queries — without requiring the InMemory
/// EF provider package or a full <c>ApplicationDbContext</c> instance.
/// </para>
/// </summary>
internal static class AsyncQueryableHelper
{
    /// <summary>
    /// Creates a <see cref="Mock{T}"/> of <c>DbSet&lt;T&gt;</c> whose LINQ queries
    /// are executed over <paramref name="data"/> in memory.
    /// </summary>
    public static Mock<Microsoft.EntityFrameworkCore.DbSet<T>> BuildMockDbSet<T>(
        IEnumerable<T> data)
        where T : class
    {
        var queryable = data.AsQueryable();
        var mock      = new Mock<Microsoft.EntityFrameworkCore.DbSet<T>>();

        // Wire up IQueryable so normal LINQ operators work.
        mock.As<IQueryable<T>>()
            .Setup(m => m.Provider)
            .Returns(new TestAsyncQueryProvider<T>(queryable.Provider));

        mock.As<IQueryable<T>>()
            .Setup(m => m.Expression)
            .Returns(queryable.Expression);

        mock.As<IQueryable<T>>()
            .Setup(m => m.ElementType)
            .Returns(queryable.ElementType);

        mock.As<IQueryable<T>>()
            .Setup(m => m.GetEnumerator())
            .Returns(() => queryable.GetEnumerator());

        // Wire up IAsyncEnumerable so async LINQ operators (ToListAsync, etc.) work.
        mock.As<IAsyncEnumerable<T>>()
            .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(new TestAsyncEnumerator<T>(queryable.GetEnumerator()));

        return mock;
    }

    // ── Internal test helpers ─────────────────────────────────────────────────

    /// <summary>
    /// Wraps a synchronous <see cref="IQueryProvider"/> as an
    /// <see cref="IAsyncQueryProvider"/> so that EF Core's async LINQ extension
    /// methods (e.g. <c>FirstOrDefaultAsync</c>) execute against in-memory data.
    /// </summary>
    private sealed class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
    {
        private readonly IQueryProvider _inner;

        internal TestAsyncQueryProvider(IQueryProvider inner) => _inner = inner;

        public IQueryable CreateQuery(Expression expression)
            => new TestAsyncEnumerable<TEntity>(expression);

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
            => new TestAsyncEnumerable<TElement>(expression);

        public object? Execute(Expression expression)
            => _inner.Execute(expression);

        public TResult Execute<TResult>(Expression expression)
            => _inner.Execute<TResult>(expression);

        public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
        {
            var resultType = typeof(TResult).GetGenericArguments().FirstOrDefault()
                             ?? typeof(TResult);

            // EF Core calls ExecuteAsync<Task<T>> for single-result async methods
            // (e.g. FirstOrDefaultAsync) — we execute synchronously and wrap in a Task.
            var executionResult = typeof(IQueryProvider)
                .GetMethod(nameof(IQueryProvider.Execute), 1, [typeof(Expression)])!
                .MakeGenericMethod(resultType)
                .Invoke(_inner, [expression]);

            return (TResult)typeof(Task)
                .GetMethod(nameof(Task.FromResult))!
                .MakeGenericMethod(resultType)
                .Invoke(null, [executionResult])!;
        }
    }

    private sealed class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
    {
        public TestAsyncEnumerable(IEnumerable<T> enumerable) : base(enumerable) { }
        public TestAsyncEnumerable(Expression expression)     : base(expression)  { }

        IQueryProvider IQueryable.Provider
            => new TestAsyncQueryProvider<T>(this);

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            => new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
    }

    private sealed class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> _inner;

        public TestAsyncEnumerator(IEnumerator<T> inner) => _inner = inner;

        public ValueTask<bool> MoveNextAsync() => ValueTask.FromResult(_inner.MoveNext());
        public T Current => _inner.Current;
        public ValueTask DisposeAsync() { _inner.Dispose(); return ValueTask.CompletedTask; }
    }
}
