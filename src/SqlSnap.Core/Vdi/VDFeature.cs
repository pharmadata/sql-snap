namespace SqlSnap.Core.Vdi
{
    public enum VDFeature : uint
    {
        Removable = 0x1,
        Rewind = 0x2,
        Position = 0x10,
        SkipBlocks = 0x20,
        ReversePosition = 0x40,
        Discard = 0x80,
        FileMarks = 0x100,
        RandomAccess = 0x200,
        SnapshotPrepare = 0x400,
        EnumFrozenFiles = 0x800,
        VSSWriter = 0x1000,
        WriteMedia = 0x10000,
        ReadMedia = 0x20000,
        LatchStats = 0x80000000,
        LikePipe = 0,
        LikeTape = FileMarks | Removable | Rewind | Position | SkipBlocks | ReversePosition,
        LikeDisk = RandomAccess
    }
}