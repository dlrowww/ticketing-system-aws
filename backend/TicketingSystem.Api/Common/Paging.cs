#nullable enable

using Microsoft.EntityFrameworkCore; // for CountAsync/ToListAsync with EF Core
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;

namespace TicketingSystem.Api.Common
{
    public enum SortDirection { Asc, Desc }

    /// <summary>Sort specification: e.g., "createdAt:desc"</summary>
    public sealed class SortSpec
    {
        public string Field { get; }
        public SortDirection Direction { get; }

        public SortSpec(string field, SortDirection direction)
        {
            Field = field;
            Direction = direction;
        }

        public static SortSpec Parse(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return new SortSpec("Id", SortDirection.Asc);
            var s = input.Trim();

            // Support "-field" or "field:desc"
            if (s.StartsWith("-"))
                return new SortSpec(s[1..], SortDirection.Desc);

            var parts = s.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length == 2 && parts[1].Equals("desc", StringComparison.OrdinalIgnoreCase))
                return new SortSpec(parts[0], SortDirection.Desc);

            return new SortSpec(parts[0], SortDirection.Asc);
        }
    }

    /// <summary>Standard paging request coming from query string.</summary>
    public sealed class PageRequest
    {
        private const int MaxSize = 200;

        [Range(1, int.MaxValue)]
        public int Page { get; init; } = 1;

        [Range(1, MaxSize)]
        public int Size { get; init; } = 20;

        /// <summary>Format: "field", "-field", or "field:desc".</summary>
        public string? Sort { get; init; }

        public SortSpec? TryGetSort() => string.IsNullOrWhiteSpace(Sort) ? null : SortSpec.Parse(Sort!);

        public (int skip, int take) ToSkipTake()
        {
            var size = Math.Clamp(Size, 1, MaxSize);
            var page = Math.Max(Page, 1);
            return ((page - 1) * size, size);
        }
    }

    /// <summary>Envelope returned to FE for any paged list.</summary>
    public sealed class Paged<T>
    {
        public IReadOnlyList<T> Items { get; }
        public int Total { get; }
        public int Page { get; }
        public int Size { get; }
        public int TotalPages { get; }
        public bool HasNext => Page < TotalPages;
        public bool HasPrevious => Page > 1;
        public string? Sort { get; }

        public Paged(IReadOnlyList<T> items, int total, PageRequest req)
        {
            Items = items;
            Total = total;
            Page = Math.Max(req.Page, 1);
            Size = Math.Clamp(req.Size, 1, 200);
            TotalPages = Math.Max((int)Math.Ceiling(total / (double)Size), 1);
            Sort = req.Sort;
        }
    }

    public static class PagingExtensions
    {
        /// <summary>
        /// Apply dynamic ordering by property name (case-insensitive).
        /// Only public readable properties are supported.
        /// </summary>
        public static IQueryable<T> ApplySort<T>(this IQueryable<T> source, SortSpec sort)
        {
            if (sort is null) return source;

            var param = Expression.Parameter(typeof(T), "x");
            var prop = typeof(T).GetProperties()
                                .FirstOrDefault(p => p.Name.Equals(sort.Field, StringComparison.OrdinalIgnoreCase));
            if (prop is null)
                return source; // unknown field → keep default ordering

            var body = Expression.Property(param, prop);
            var lambda = Expression.Lambda(body, param);

            var methodName = sort.Direction == SortDirection.Asc ? "OrderBy" : "OrderByDescending";
            var result = typeof(Queryable)
                .GetMethods()
                .Single(m => m.Name == methodName
                             && m.IsGenericMethodDefinition
                             && m.GetParameters().Length == 2)
                .MakeGenericMethod(typeof(T), prop.PropertyType)
                .Invoke(null, new object[] { source, lambda })!;

            return (IQueryable<T>)result;
        }

        /// <summary>
        /// Materialize a paged result (EF Core async-friendly).
        /// </summary>
        public static async Task<Paged<T>> ToPagedAsync<T>(
            this IQueryable<T> query,
            PageRequest req,
            CancellationToken ct = default)
        {
            // Sorting
            var sort = req.TryGetSort();
            if (sort != null)
                query = query.ApplySort(sort);

            var (skip, take) = req.ToSkipTake();

            var total = await query.CountAsync(ct);
            var items = total > 0
                ? await query.Skip(skip).Take(take).ToListAsync(ct)
                : new List<T>(0);

            return new Paged<T>(items, total, req);
        }

        /// <summary>
        /// In-memory paging (for ADO results already materialized).
        /// </summary>
        public static Paged<T> ToPaged<T>(this IEnumerable<T> items, PageRequest req)
        {
            var list = items as IList<T> ?? items.ToList();
            var total = list.Count;
            var (skip, take) = req.ToSkipTake();

            // Note: in-memory sort optional; typically ADO should sort in SQL.
            if (!string.IsNullOrWhiteSpace(req.Sort))
            {
                var sort = req.TryGetSort()!;
                list = sort.Direction == SortDirection.Asc
                    ? list.OrderBy(x => GetPropValue(x, sort.Field)).ToList()
                    : list.OrderByDescending(x => GetPropValue(x, sort.Field)).ToList();
            }

            var pageItems = (skip < total) ? list.Skip(skip).Take(take).ToList() : new List<T>(0);
            return new Paged<T>(pageItems, total, req);

            static object? GetPropValue(object obj, string name)
                => obj.GetType().GetProperty(name,
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase)
                     ?.GetValue(obj);
        }
    }
}