using English.Data;
using Microsoft.EntityFrameworkCore;

namespace English.Services;

// Inspect connection metadata only; never open a connection to the main database.
public sealed class BooksDatabaseGuard(ApplicationDbContext context)
{
    public bool IsConfiguredTarget()
    {
        var connection = context.Database.GetDbConnection();
        return string.Equals(connection.Database, "English_P2_Test", StringComparison.Ordinal)
            && string.Equals(connection.DataSource, "LAPTOP-8RQB10J4", StringComparison.OrdinalIgnoreCase);
    }

    public void EnsureTarget()
    {
        if (!IsConfiguredTarget()) throw new InvalidOperationException("BooksDatabaseTarget");
    }
}
