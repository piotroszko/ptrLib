using System;

namespace PtrLib.Networking;

/// <summary>Parsing and comparison helpers for IPv4 addresses.</summary>
public static class Ipv4Address
{
    public static bool TryParse(string? text, out uint value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        string[] parts = text.Split('.');
        if (parts.Length != 4)
            return false;

        uint result = 0;
        for (int i = 0; i < parts.Length; i++)
        {
            if (!byte.TryParse(parts[i], out byte octet))
                return false;
            result = (result << 8) | octet;
        }

        value = result;
        return true;
    }

    public static string Format(uint value)
        => $"{value >> 24}.{(value >> 16) & 0xFF}.{(value >> 8) & 0xFF}.{value & 0xFF}";

    public static int Compare(string? left, string? right)
    {
        if (TryParse(left, out uint a) && TryParse(right, out uint b))
            return a.CompareTo(b);
        return string.Compare(left, right, StringComparison.Ordinal);
    }
}
