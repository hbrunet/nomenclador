using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using NHibernate;
using NHibernate.Context;
using NHibernate.Dialect;
using NHibernate.Driver;
using Nomenclador.Api.Data.Mappings;
using NhEnvironment = NHibernate.Cfg.Environment;

namespace Nomenclador.Api.Data;

public static class NHibernateSessionFactory
{
    public static ISessionFactory Build(string connectionString)
    {
        return Fluently.Configure()
            .Database(
                OracleDataClientConfiguration.Oracle10
                    .Driver<OracleManagedDataClientDriver>()
                    .Dialect<Oracle10gDialect>()
                    .ConnectionString(connectionString)
            )
            .Mappings(m => m.FluentMappings.AddFromAssemblyOf<ConfiguracionNomencladorMap>())
            .ExposeConfiguration(config =>
            {
                config.SetProperty(NhEnvironment.ShowSql, "false");
                config.SetProperty(NhEnvironment.FormatSql, "false");
                config.SetProperty(NhEnvironment.Hbm2ddlAuto, "none");
            })
            .CurrentSessionContext<WebSessionContext>()
            .BuildSessionFactory();
    }
}
