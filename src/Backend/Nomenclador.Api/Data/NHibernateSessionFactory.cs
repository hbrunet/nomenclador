using FluentNHibernate.Cfg;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Dialect;
using NHibernate.Driver;
using Nomenclador.Api.Data.Mappings;
using NhEnvironment = NHibernate.Cfg.Environment;

namespace Nomenclador.Api.Data;

public static class NHibernateSessionFactory
{
    public static ISessionFactory Build(string connectionString)
    {
        var nhConfig = new Configuration();

        nhConfig.SetProperty(NhEnvironment.ConnectionDriver,
            typeof(OracleManagedDataClientDriver).AssemblyQualifiedName);

        nhConfig.SetProperty(NhEnvironment.Dialect,
            typeof(Oracle10gDialect).AssemblyQualifiedName);

        nhConfig.SetProperty(NhEnvironment.ConnectionString, connectionString);

        // Desactivar show_sql en producción; activar solo para debugging
        nhConfig.SetProperty(NhEnvironment.ShowSql, "false");
        nhConfig.SetProperty(NhEnvironment.FormatSql, "false");

        // Evitar que NHibernate genere DDL (la BD ya existe)
        nhConfig.SetProperty(NhEnvironment.Hbm2ddlAuto, "none");

        return Fluently
            .Configure(nhConfig)
            .Mappings(m => m.FluentMappings.AddFromAssemblyOf<ConfiguracionNomencladorMap>())
            .BuildSessionFactory();
    }
}
