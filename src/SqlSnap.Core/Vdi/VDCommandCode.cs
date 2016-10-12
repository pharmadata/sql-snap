namespace SqlSnap.Core.Vdi
{
    public enum VDCommandCode
    {
        Read = 1,
        Write,
        ClearError,
        Rewind,
        WriteMark,
        SkipMarks,
        SkipBlocks,
        Load,
        GetPosition,
        SetPosition,
        Discard,
        Flush,
        Snapshot,
        MountSnapshot,
        PrepareToFreeze,
        FileInfoBegin,
        FileInfoEnd
    }
}