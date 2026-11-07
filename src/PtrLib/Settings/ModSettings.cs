using System;
using System.Collections.Generic;
using MelonLoader;

namespace PtrLib.Settings;

public sealed class ModSettings
{
    private readonly List<SettingsSection> _sections = new List<SettingsSection>();
    private bool _dirty;

    public IReadOnlyList<SettingsSection> Sections => _sections;
    public bool IsDirty => _dirty;

    public SettingsSection AddSection(string id, string title, MelonPreferences_Category category)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Section id must not be empty.", nameof(id));
        if (category == null)
            throw new ArgumentNullException(nameof(category));

        foreach (var existing in _sections)
        {
            if (string.Equals(existing.Id, id, StringComparison.Ordinal))
                throw new InvalidOperationException($"Duplicate settings section id '{id}'.");
        }

        var section = new SettingsSection(id, title, category, MarkDirty);
        _sections.Add(section);
        return section;
    }

    private void MarkDirty() => _dirty = true;

    public void ClearDirty() => _dirty = false;

    public void AddExternal(SettingsSection section)
    {
        if (section == null) return;
        if (_sections.Contains(section)) return;
        _sections.Add(section);
    }
}
