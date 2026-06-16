using global::Cassandra;

namespace Esports.Ranking.Api.Cassandra;

public interface ICassandraSession
{
    global::Cassandra.ISession Session { get; }
}

public class CassandraSession : ICassandraSession, IDisposable
{
    public global::Cassandra.ISession Session { get; }
    private readonly ICluster _cluster;

    public CassandraSession(IConfiguration config)
    {
        var contactPoint = config["Cassandra:ContactPoints"] ?? "localhost";
        var port = int.Parse(config["Cassandra:Port"] ?? "9042");

        _cluster = Cluster.Builder()
            .AddContactPoint(contactPoint)
            .WithPort(port)
            .Build();

        Session = _cluster.Connect();
    }

    public void Dispose()
    {
        Session.Dispose();
        _cluster.Dispose();
    }
}
