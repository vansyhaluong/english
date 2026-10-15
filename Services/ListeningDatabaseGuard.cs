using System.Data;
using English.Data;
using English.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;

namespace English.Services;

// No connection string parsing, replacement connection, or database mutation.
public sealed class ListeningDatabaseGuard(ApplicationDbContext context)
{
    public const string DatabaseName = "English_P2_Test";
    public const string ServerName = "LAPTOP-8RQB10J4";

    public bool IsConfiguredTarget()
    {
        var connection = context.Database.GetDbConnection();
        return string.Equals(connection.Database, DatabaseName, StringComparison.Ordinal)
            && string.Equals(connection.DataSource, ServerName, StringComparison.OrdinalIgnoreCase);
    }

    public async Task<ListeningDatabaseCheck> CheckAsync(CancellationToken cancellationToken)
    {
        // Must run before OpenConnectionAsync, including when called before authorization.
        if (!IsConfiguredTarget()) return new(false, "ListeningDatabaseTarget");
        var connection = context.Database.GetDbConnection();
        var openedHere = connection.State == ConnectionState.Closed;
        try
        {
            if (openedHere) await context.Database.OpenConnectionAsync(cancellationToken);
            await using (var command = connection.CreateCommand())
            {
                command.Transaction = context.Database.CurrentTransaction?.GetDbTransaction();
                command.CommandText = """
                    SELECT DB_NAME(), CONVERT(nvarchar(128), SERVERPROPERTY('ServerName')),
                        CONVERT(nvarchar(60), DATABASEPROPERTYEX(DB_NAME(), 'Status')),
                        CONVERT(nvarchar(60), DATABASEPROPERTYEX(DB_NAME(), 'Updateability'));
                    """;
                await using var reader = await command.ExecuteReaderAsync(cancellationToken);
                if (!await reader.ReadAsync(cancellationToken)
                    || reader.GetString(0) != DatabaseName
                    || !string.Equals(reader.GetString(1), ServerName, StringComparison.OrdinalIgnoreCase)
                    || reader.GetString(2) != "ONLINE" || reader.GetString(3) != "READ_WRITE")
                    return new(false, "ListeningDatabaseTarget");
            }

            // Match the schema used by EF, not a second hand-maintained entity model.
            var tables = new[] { "LearningItem", "ListeningLesson", "TranscriptCue", "Level", "Topic" };
            foreach (var entity in context.Model.GetEntityTypes().Where(x => tables.Contains(x.GetTableName())))
            {
                var table = entity.GetTableName()!;
                var schema = entity.GetSchema() ?? "dbo";
                var store = StoreObjectIdentifier.Table(table, entity.GetSchema());
                var columns = new Dictionary<string, (string Type, bool Nullable, bool Identity)>(StringComparer.Ordinal);
                await using (var command = connection.CreateCommand())
                {
                    command.Transaction = context.Database.CurrentTransaction?.GetDbTransaction();
                    command.CommandText = """
                        SELECT c.name, t.name, c.max_length, c.precision, c.scale, c.is_nullable, c.is_identity
                        FROM sys.columns c JOIN sys.types t ON c.user_type_id=t.user_type_id
                        WHERE c.object_id=OBJECT_ID(@table);
                        """;
                    var parameter = command.CreateParameter(); parameter.ParameterName = "@table";
                    parameter.Value = schema + "." + table; command.Parameters.Add(parameter);
                    await using var reader = await command.ExecuteReaderAsync(cancellationToken);
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        var type = reader.GetString(1);
                        var length = reader.GetInt16(2);
                        if (type is "nvarchar" or "nchar" or "varchar" or "char" or "varbinary" or "binary")
                            type += "(" + (length == -1 ? "max" : (length / (type.StartsWith('n') ? 2 : 1)).ToString()) + ")";
                        if (type == "timestamp") type = "rowversion";
                        columns.Add(reader.GetString(0), (type, reader.GetBoolean(5), reader.GetBoolean(6)));
                    }
                }
                foreach (var property in entity.GetProperties())
                {
                    var column = property.GetColumnName(store)!;
                    var expectedType = property.GetColumnType() ?? property.GetRelationalTypeMapping().StoreType;
                    if (!columns.TryGetValue(column, out var actual)
                        || !string.Equals(actual.Type, expectedType, StringComparison.OrdinalIgnoreCase)
                        || actual.Nullable != property.IsColumnNullable(store))
                        return new(false, "ListeningDatabaseSchema");
                    if ((table is "LearningItem" or "TranscriptCue") && column == "Id" && !actual.Identity)
                        return new(false, "ListeningDatabaseSchema");
                }
                // Validate primary key order and all modeled foreign keys using SQL Server metadata.
                var primaryKey = entity.FindPrimaryKey()!;
                var expectedKey = string.Join(",", primaryKey.Properties.Select(x => x.GetColumnName(store)));
                await using (var command = connection.CreateCommand())
                {
                    command.Transaction = context.Database.CurrentTransaction?.GetDbTransaction();
                    command.CommandText = """
                        SELECT STRING_AGG(CONVERT(nvarchar(max), c.name), ',') WITHIN GROUP (ORDER BY ic.key_ordinal)
                        FROM sys.indexes i JOIN sys.index_columns ic ON i.object_id=ic.object_id AND i.index_id=ic.index_id
                        JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
                        WHERE i.object_id=OBJECT_ID(@table) AND i.is_primary_key=1 AND i.is_disabled=0;
                        """;
                    var parameter = command.CreateParameter(); parameter.ParameterName = "@table";
                    parameter.Value = schema + "." + table; command.Parameters.Add(parameter);
                    if (!string.Equals(await command.ExecuteScalarAsync(cancellationToken) as string, expectedKey, StringComparison.Ordinal))
                        return new(false, "ListeningDatabaseSchema");
                }
                foreach (var fk in entity.GetForeignKeys())
                {
                    var principal = fk.PrincipalEntityType;
                    var principalStore = StoreObjectIdentifier.Table(principal.GetTableName()!, principal.GetSchema());
                    await using var command = connection.CreateCommand();
                    command.Transaction = context.Database.CurrentTransaction?.GetDbTransaction();
                    command.CommandText = """
                        SELECT COUNT(*) FROM (
                          SELECT f.object_id
                          FROM sys.foreign_keys f JOIN sys.foreign_key_columns fc ON f.object_id=fc.constraint_object_id
                          JOIN sys.columns pc ON pc.object_id=fc.parent_object_id AND pc.column_id=fc.parent_column_id
                          JOIN sys.columns rc ON rc.object_id=fc.referenced_object_id AND rc.column_id=fc.referenced_column_id
                          WHERE f.parent_object_id=OBJECT_ID(@parent) AND f.referenced_object_id=OBJECT_ID(@principal)
                            AND f.is_disabled=0 AND f.is_not_trusted=0 AND f.delete_referential_action=0
                          GROUP BY f.object_id
                          HAVING STRING_AGG(CONVERT(nvarchar(max), pc.name), ',') WITHIN GROUP (ORDER BY fc.constraint_column_id)=@columns
                             AND STRING_AGG(CONVERT(nvarchar(max), rc.name), ',') WITHIN GROUP (ORDER BY fc.constraint_column_id)=@keys
                        ) matched;
                        """;
                    foreach (var (name, value) in new[] {
                        ("@parent", schema + "." + table),
                        ("@principal", (principal.GetSchema() ?? "dbo") + "." + principal.GetTableName()),
                        ("@columns", string.Join(",", fk.Properties.Select(x => x.GetColumnName(store)))),
                        ("@keys", string.Join(",", fk.PrincipalKey.Properties.Select(x => x.GetColumnName(principalStore)))) })
                    {
                        var parameter = command.CreateParameter(); parameter.ParameterName = name;
                        parameter.Value = value; command.Parameters.Add(parameter);
                    }
                    if (Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) != 1)
                        return new(false, "ListeningDatabaseSchema");
                }
            }
            await using (var command = connection.CreateCommand())
            {
                command.Transaction = context.Database.CurrentTransaction?.GetDbTransaction();
                command.CommandText = """
                    SELECT CASE WHEN
                      EXISTS (
                        SELECT 1 FROM sys.indexes i
                        JOIN sys.index_columns ic ON i.object_id=ic.object_id AND i.index_id=ic.index_id
                        JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
                        WHERE i.object_id=OBJECT_ID('dbo.TranscriptCue') AND i.is_unique=1 AND i.is_disabled=0
                          AND i.has_filter=0 AND ic.key_ordinal>0
                        GROUP BY i.index_id HAVING STRING_AGG(CONVERT(nvarchar(max),c.name),',')
                          WITHIN GROUP (ORDER BY ic.key_ordinal)='ListeningLessonId,Sequence')
                      AND NOT EXISTS (
                        SELECT 1 FROM sys.check_constraints
                        WHERE parent_object_id IN (OBJECT_ID('dbo.LearningItem'),OBJECT_ID('dbo.ListeningLesson'),OBJECT_ID('dbo.TranscriptCue'))
                          AND (is_disabled=1 OR is_not_trusted=1))
                      AND EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID('dbo.LearningItem'))
                      AND EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID('dbo.TranscriptCue'))
                      THEN 1 ELSE 0 END;
                    """;
                if (Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) != 1)
                    return new(false, "ListeningDatabaseSchema");
            }
            return new(true, "ListeningDatabaseVerified");
        }
        catch (Exception exception) when (exception is System.Data.Common.DbException or InvalidOperationException)
        {
            return new(false, "ListeningDatabaseUnverified");
        }
        finally
        {
            if (openedHere && connection.State != ConnectionState.Closed)
                await context.Database.CloseConnectionAsync();
        }
    }
}
