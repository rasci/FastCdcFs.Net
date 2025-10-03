namespace FastCdcFs.Net;

public class FastCdcFsException(string message) : Exception(message);

public class InvalidFastCdcFsVersionException(byte actualVersion) : FastCdcFsException($"Invalid FastCdcFs version {actualVersion}")
{
    public byte ActualVersion => actualVersion;
}

public class InvalidFastCdcFsFileException(string path) : FastCdcFsException($"Invalid file {path}");

public class CorruptedDataException() : FastCdcFsException("Data is corrupted");

public class CorruptedMetaDataException() : FastCdcFsException("Meta data is corrupted");
