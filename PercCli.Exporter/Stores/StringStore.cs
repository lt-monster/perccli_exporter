namespace PercCli.Exporter.Stores;

//多个相同的字符串只存储一份，用一个StringToken代表，节省内存
public readonly record struct StringToken(int Value)
{
    public static implicit operator int(StringToken stringToken) => stringToken.Value;

    public static implicit operator StringToken(int val) => new() { Value = val };
}

public static class StringStore
{
    // 存储 ID 到 字节块的映射
    private static readonly List<byte[]> IdToBytes = [];

    // 使用 .NET 8+ 的 AlternateLookup 优化查询
    private static readonly Dictionary<byte[], StringToken> Lookup = new(new ByteArrayComparer());
    private static readonly Dictionary<byte[], StringToken>.AlternateLookup<ReadOnlySpan<byte>> SpanLookup;

    static StringStore()
    {
        SpanLookup = Lookup.GetAlternateLookup<ReadOnlySpan<byte>>();
    }

    public static StringToken GetOrAdd(ReadOnlySpan<byte> utf8Span)  
    {
        // 核心：直接用 Span 查询，如果已存在，零分配返回
        if (SpanLookup.TryGetValue(utf8Span, out var id))
        {
            return id;
        }

        // 只有遇到从未见过的新字符串时，才会分配一次
        var newArray = utf8Span.ToArray();
        id = new StringToken(IdToBytes.Count);
        Lookup[newArray] = id;
        IdToBytes.Add(newArray);
        return id;
    }

    public static ReadOnlySpan<byte> GetBytes(StringToken id) => IdToBytes[id.Value];
}

public sealed class ByteArrayComparer : IEqualityComparer<byte[]>,IAlternateEqualityComparer<ReadOnlySpan<byte>, byte[]>
{
    // 实现内容相等判断
    public bool Equals(byte[]? x, byte[]? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x == null || y == null) return false;
        // 使用 .NET 优化的 SequenceEqual，内部会使用 SIMD 加速
        return x.AsSpan().SequenceEqual(y);
    }

    // 实现哈希计算
    public int GetHashCode(byte[] obj)
    {
        var hash = new HashCode();
        hash.AddBytes(obj); // .NET 8+ 直接支持高效添加字节流哈希
        return hash.ToHashCode();
    }

    // 逻辑 A：比较 Span 和 数组
    public bool Equals(ReadOnlySpan<byte> alternate, byte[] rice) => alternate.SequenceEqual(rice);

    // 逻辑 B：计算 Span 的 HashCode (必须与数组生成的 HashCode 严格一致)
    public int GetHashCode(ReadOnlySpan<byte> alternate)
    {
        var hash = new HashCode();
        hash.AddBytes(alternate); // .NET 8+ 直接支持高效添加字节流哈希
        return hash.ToHashCode();
    }

    public byte[] Create(ReadOnlySpan<byte> alternate) => alternate.ToArray();
}