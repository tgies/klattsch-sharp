using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace KlattschSharp.Text;

/// <summary>
/// Named phoneme bank definitions with <c>extends</c> layering. Register raw
/// definitions with <see cref="Register"/>, then get a resolved
/// <see cref="IPhonemeBank"/> from <see cref="Resolve"/>.
/// </summary>
public sealed class PhonemeBankRegistry
{
    /// <summary>A raw, unresolved bank definition.</summary>
    public sealed class BankDefinition
    {
        /// <summary>Registry name for this bank (e.g. <c>"klatt1980-en"</c>).</summary>
        public string Name { get; }

        /// <summary>Name of the parent bank this one extends, or <c>null</c>.</summary>
        public string? Extends { get; }

        /// <summary>
        /// Phoneme entries defined directly in this bank, in order (see
        /// <see cref="IPhonemeBank.Names"/>). A duplicate code keeps its first
        /// position and takes the last value given for it.
        /// </summary>
        public IReadOnlyList<KeyValuePair<string, PhonemeParams>> Entries { get; }

        /// <summary>BCP-47 language tag, or <c>null</c>.</summary>
        public string? Language { get; }

        /// <summary>Definition named <paramref name="name"/>, optionally layered on <paramref name="extends"/>.</summary>
        public BankDefinition(
            string name,
            IEnumerable<KeyValuePair<string, PhonemeParams>> entries,
            string? extends = null,
            string? language = null)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (entries == null) throw new ArgumentNullException(nameof(entries));
            Name = name;
            Entries = Array.AsReadOnly(new List<KeyValuePair<string, PhonemeParams>>(entries).ToArray());
            Extends = extends;
            Language = language;
        }
    }

    private readonly Dictionary<string, BankDefinition> _definitions =
        new Dictionary<string, BankDefinition>(StringComparer.Ordinal);
    private readonly List<string> _registrationOrder = new List<string>();
    private readonly Dictionary<string, IPhonemeBank> _cache =
        new Dictionary<string, IPhonemeBank>(StringComparer.Ordinal);
    private readonly object _lock = new object();

    /// <summary>
    /// Register a bank definition. A same-named bank is replaced in place and
    /// the resolved cache is cleared.
    /// </summary>
    public void Register(BankDefinition definition)
    {
        if (definition == null) throw new ArgumentNullException(nameof(definition));
        lock (_lock)
        {
            if (!_definitions.ContainsKey(definition.Name))
            {
                _registrationOrder.Add(definition.Name);
            }
            _definitions[definition.Name] = definition;
            _cache.Clear();
        }
    }

    /// <summary>All registered names, in registration order.</summary>
    public IReadOnlyList<string> Names
    {
        get
        {
            lock (_lock)
            {
                return new List<string>(_registrationOrder);
            }
        }
    }

    /// <summary>
    /// Resolve the named bank, applying <c>extends</c> layering. The result is
    /// cached and returned again for the same name.
    /// </summary>
    /// <exception cref="KeyNotFoundException">No bank with <paramref name="name"/> is registered.</exception>
    /// <exception cref="InvalidOperationException">An <c>extends</c> cycle is detected.</exception>
    public IPhonemeBank Resolve(string name)
    {
        if (name == null) throw new ArgumentNullException(nameof(name));
        lock (_lock)
        {
            if (_cache.TryGetValue(name, out IPhonemeBank? cached))
            {
                return cached;
            }

            IPhonemeBank resolved = ResolveInternal(name, new HashSet<string>(StringComparer.Ordinal));
            _cache[name] = resolved;
            return resolved;
        }
    }

    /// <summary>
    /// Resolve the named bank if registered. Returns <c>false</c> only when no
    /// bank of that name exists; a registered bank with a cyclic or dangling
    /// <c>extends</c> chain still throws.
    /// </summary>
    /// <exception cref="InvalidOperationException">An <c>extends</c> cycle is detected.</exception>
    /// <exception cref="KeyNotFoundException">An <c>extends</c> parent is not registered.</exception>
    public bool TryResolve(string name, [MaybeNullWhen(false)] out IPhonemeBank bank)
    {
        if (name == null) throw new ArgumentNullException(nameof(name));
        lock (_lock)
        {
            if (_cache.TryGetValue(name, out IPhonemeBank? cached))
            {
                bank = cached;
                return true;
            }

            if (!_definitions.ContainsKey(name))
            {
                bank = null;
                return false;
            }

            bank = ResolveInternal(name, new HashSet<string>(StringComparer.Ordinal));
            _cache[name] = bank;
            return true;
        }
    }

    private IPhonemeBank ResolveInternal(string name, HashSet<string> visiting)
    {
        if (visiting.Contains(name))
        {
            var path = string.Join(" -> ", visiting) + " -> " + name;
            throw new InvalidOperationException($"bank extends cycle detected: {path}");
        }

        if (!_definitions.TryGetValue(name, out BankDefinition? def))
        {
            throw new KeyNotFoundException($"unknown bank: {name}");
        }

        var phonemes = new Dictionary<string, PhonemeParams>(StringComparer.Ordinal);

        // Name order is tracked explicitly because Dictionary enumeration order
        // is undefined in .NET: inherited names come first in the parent's
        // order, a same-name child entry overrides in place, and names new to
        // the child are appended.
        var order = new List<string>();

        if (def.Extends != null)
        {
            visiting.Add(name);
            IPhonemeBank parent = ResolveInternal(def.Extends, visiting);
            visiting.Remove(name);

            foreach (string code in parent.Names)
            {
                if (parent.TryGet(code, out PhonemeParams p))
                {
                    phonemes[code] = p;
                    order.Add(code);
                }
            }
        }

        foreach (var kvp in def.Entries)
        {
            if (!phonemes.ContainsKey(kvp.Key))
            {
                order.Add(kvp.Key);
            }
            phonemes[kvp.Key] = kvp.Value;
        }

        return new ResolvedBank(name, def.Language, phonemes, order.ToArray());
    }

    private sealed class ResolvedBank : IPhonemeBank
    {
        private readonly Dictionary<string, PhonemeParams> _phonemes;
        private readonly ReadOnlyCollection<string> _names;

        public ResolvedBank(string name, string? language, Dictionary<string, PhonemeParams> phonemes, string[] names)
        {
            Name = name;
            Language = language;
            _phonemes = phonemes;
            _names = Array.AsReadOnly(names);
        }

        public string Name { get; }

        public string? Language { get; }

        public IReadOnlyList<string> Names => _names;

        public bool TryGet(string code, out PhonemeParams p)
        {
            if (code == null)
            {
                p = default;
                return false;
            }
            return _phonemes.TryGetValue(code, out p);
        }
    }
}
