using System;

namespace PtrLib.Networking;

/// <summary>Parsing and membership helpers for IPv4 subnet CIDR strings.</summary>
public static class Ipv4Subnet
{
    public static bool TryParseCidr(string? cidr, out uint network, out int prefix)
    {
        network = 0;
        prefix = 0;
        if (string.IsNullOrWhiteSpace(cidr))
            return false;

        int slash = cidr.IndexOf('/');
        if (slash < 0)
            return false;

        return Ipv4Address.TryParse(cidr.Substring(0, slash), out network) &&
            int.TryParse(cidr.Substring(slash + 1), out prefix) &&
            prefix >= 0 && prefix <= 32;
    }

    public static uint Mask(int prefix)
        => prefix <= 0 ? 0u : 0xFFFFFFFFu << (32 - prefix);

    public static uint Network(uint address, int prefix)
        => address & Mask(prefix);

    public static uint Broadcast(uint network, int prefix)
        => network | (prefix <= 0 ? uint.MaxValue : 0xFFFFFFFFu >> prefix);

    public static bool Contains(string? ip, string? cidr)
    {
        return TryParseCidr(cidr, out uint network, out int prefix) &&
            Ipv4Address.TryParse(ip, out uint value) &&
            Contains(value, network, prefix);
    }

    public static bool Contains(uint address, uint network, int prefix)
        => (address & Mask(prefix)) == (network & Mask(prefix));

    public static int HostCount(string? cidr)
    {
        if (!TryParseCidr(cidr, out _, out int prefix) || prefix > 30)
            return 0;

        long count = 1L << (32 - prefix);
        return (int)Math.Max(0, count - 2);
    }

    public static bool Overlaps(string? left, string? right)
    {
        return TryParseCidr(left, out uint a, out int aPrefix) &&
            TryParseCidr(right, out uint b, out int bPrefix) &&
            Overlaps(a, aPrefix, b, bPrefix);
    }

    public static bool Overlaps(uint a, int aPrefix, uint b, int bPrefix)
    {
        int prefix = Math.Min(aPrefix, bPrefix);
        uint mask = Mask(prefix);
        return (a & mask) == (b & mask);
    }
}
