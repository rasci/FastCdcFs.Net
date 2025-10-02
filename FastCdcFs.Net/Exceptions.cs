namespace FastCdcFs.Net;

public abstract class FastCdcFsException(string message) : Exception(message);

public class InvalidFastCdcFsFileException(string path) : FastCdcFsException($"Invalid file {path}");

public class CorruptedDataException() : FastCdcFsException("Data is corrupted");
