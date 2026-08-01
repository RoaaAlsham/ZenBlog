namespace ZenBlog.Application.Contracts.Monitoring;

public interface IClientRequestInfo
{
    string SourceIp { get; }
    string Host { get; }
    string Path { get; }
}
