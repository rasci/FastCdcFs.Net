namespace FastCdcFs.Net;

[Flags]
public enum Modes
{
    None = 0x0,
    NoZstd = 0x1,
    NoHash = 0x2
}
