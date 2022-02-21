using System;
using System.Diagnostics;
using System.Threading.Tasks;
using DotNet.Testcontainers.Containers.Builders;
using DotNet.Testcontainers.Containers.Configurations.Databases;
using DotNet.Testcontainers.Containers.Modules;
using DotNet.Testcontainers.Containers.Modules.Databases;
using DotNet.Testcontainers.Containers.WaitStrategies;
using DotNet.Testcontainers.Networks.Builders;

namespace ConsoleApp1
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var network = new TestcontainersNetworkBuilder().WithName("octo-network").Build();
            await network.CreateAsync();
            var password = "Password01!";
            var databaseBuilder = new TestcontainersBuilder<MsSqlTestcontainer>()
                .WithDatabase(new MsSqlTestcontainerConfiguration { Password = password })
                .WithNetwork(network)
                .WithName("db");

            var octopusServerBuilder = new TestcontainersBuilder<TestcontainersContainer>()
                .WithImage("octopusdeploy/octopusdeploy")
                .WithName("octopus")
                .WithNetwork(network)
                .WithEnvironment("ACCEPT_EULA", "Y")
                .WithEnvironment("DB_CONNECTION_STRING",
                    $"Server=db,1433;Initial Catalog=Octopus;Persist Security Info=false;User ID=sa;Password={password};MultipleActiveResultSets=false;Connection Timeout=30;")
                .WithEnvironment("ADMIN_USERNAME", "admin")
                .WithEnvironment("ADMIN_PASSWORD", password)
                .WithPortBinding(8080, assignRandomHostPort: true)
                .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(8080));

            Stopwatch sw = Stopwatch.StartNew();

            await using (var database = databaseBuilder.Build())
            await using(var server = octopusServerBuilder.Build())
            {
                Task.WaitAll(server.StartAsync(), database.StartAsync());

                sw.Stop();
                Console.WriteLine($"Listening on port {server.GetMappedPublicPort(8080)}");
                Console.WriteLine($"Running in {sw.Elapsed.TotalSeconds} seconds");
                Console.ReadKey();
            }

            await network.DeleteAsync();
        }
    }
}
