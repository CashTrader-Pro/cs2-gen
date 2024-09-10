using Google.Protobuf;
using NullFX.CRC;

namespace CS2Gen;

public static class InspectLink
{
    public static string Serialize
    (
        uint accountId,
        ulong itemId,
        DefIndex defIndex,
        uint paintIndex,
        Rarity rarity,
        Quality quality,
        float paintWear,
        uint paintSeed,
        uint killEaterScoreType,
        uint killEaterValue,
        string customName,
        CEconItemPreviewDataBlock.Types.Sticker[] stickers,
        uint inventory,
        Origin origin,
        uint questId,
        uint dropReason,
        uint musicIndex,
        int entIndex
    )
    {
        var proto = new CEconItemPreviewDataBlock
        {
            Accountid = accountId,
            Itemid = itemId,
            Defindex = (uint)defIndex,
            Paintindex = paintIndex,
            Rarity = (uint)rarity,
            Quality = (uint)quality,
            Paintwear = BitConverter.ToUInt32(BitConverter.GetBytes(paintWear)),
            Paintseed = paintSeed,
            Killeaterscoretype = killEaterScoreType,
            Killeatervalue = killEaterValue,
            Customname = customName,
            Stickers = { stickers },
            Inventory = inventory,
            Origin = (uint)origin,
            Questid = questId,
            Dropreason = dropReason,
            Musicindex = musicIndex,
            Entindex = entIndex
        };

        using var memoryStream = new MemoryStream();
        memoryStream.WriteByte(default);
        proto.WriteTo(memoryStream);

        var crc = Crc32.ComputeChecksum(memoryStream.ToArray());
        var xoredCrc = (crc & 0xFFFF) ^ (uint)(proto.CalculateSize() * crc);
        var checksumBytes = BitConverter.GetBytes(xoredCrc);

        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(checksumBytes);
        }

        memoryStream.Write(checksumBytes, 0, checksumBytes.Length);

        return $"{CsActionFunction} {BitConverter.ToString(memoryStream.ToArray()).Replace("-", string.Empty)}";
    }

    public static (CEconItemPreviewDataBlock, float) Deserialize(string inspectLink)
    {
        var hexString = inspectLink.Replace(CsActionFunction, "").Trim();

        var buffer = new byte[hexString.Length / 2];
        for (var i = 0; i < buffer.Length; i++)
        {
            buffer[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
        }

        if (buffer.Length < 5)
        {
            throw new InvalidOperationException("Invalid buffer length.");
        }

        var bufferWithoutChecksum = buffer[..^4];
        var protoBytes = bufferWithoutChecksum[1..];


        var proto = new CEconItemPreviewDataBlock();
        proto.MergeFrom(protoBytes);

        var originalFloat = BitConverter.ToSingle(BitConverter.GetBytes(proto.Paintwear));

        return (proto, originalFloat);
    }

    private const string CsActionFunction = "csgo_econ_action_preview";
}