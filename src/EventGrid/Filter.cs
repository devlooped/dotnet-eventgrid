using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Azure.EventGrid.Models;
using Minimatch;

namespace Devlooped
{
    public class Filter
    {
        List<Func<PathEventGridEvent, bool>> filters = new List<Func<PathEventGridEvent, bool>>();
        ConcurrentDictionary<string, List<Minimatcher>> matchers = new ConcurrentDictionary<string, List<Minimatcher>>();

        public static Filter Parse(params string[] args)
        {
            var props = typeof(PathEventGridEvent).GetProperties()
                .ToDictionary(prop => prop.Name, StringComparer.OrdinalIgnoreCase);

            var matchers = new List<(PropertyInfo, Minimatcher)>();
            var options = new Options { IgnoreCase = true };

            foreach (var filter in args
                .Where(x => x.StartsWith('+'))
                .Select(s => s.TrimStart('+').Split(new [] { ':', '=' }))
                .Where(pair => pair.Length == 2))
            {
                if (!props.TryGetValue(filter[0], out var prop))
                    throw new ArgumentException($"Property '{filter[0]}' does not exist in {nameof(EventGridEvent)}. Cannot apply filter '+{filter[0]}'.", nameof(args));

                if (prop.PropertyType != typeof(string))
                    throw new ArgumentException($"Can only filter string properties. Cannot apply filter '+{filter[0]}'.", nameof(args));

                matchers.Add((prop, new Minimatcher(filter[1], options)));
            }

            return new Filter(matchers);
        }

        Filter(List<(PropertyInfo property, Minimatcher matcher)> matchers)
        {
            var method = GetType().GetMethod(nameof(Matches), BindingFlags.Instance | BindingFlags.NonPublic);
            var e = Expression.Parameter(typeof(PathEventGridEvent), "e");

            foreach (var pair in matchers)
            {
                this.matchers.GetOrAdd(pair.property.Name, _ => new List<Minimatcher>()).Add(pair.matcher);

                filters.Add(Expression.Lambda<Func<PathEventGridEvent, bool>>(
                    Expression.Call(
                        Expression.Constant(this),
                        method,
                        Expression.Constant(pair.property.Name),
                        Expression.Property(e, pair.property)),
                    e).Compile());
            }
        }

        public bool ShouldInclude(PathEventGridEvent e) => filters.All(x => x.Invoke(e));

        bool Matches(string property, string value) => matchers.GetOrAdd(property, _ => new List<Minimatcher>()).Any(m => m.IsMatch(value));
    }
}
