using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Cbo.API.Data;

/// <summary>
/// Recognizes Postgres constraint violations behind an EF save failure.
/// </summary>
public static class PostgresConstraint
{
    public static bool IsUniqueViolation(DbUpdateException exception, string constraintName) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } postgres
        && postgres.ConstraintName == constraintName;

    public static bool IsForeignKeyViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation };
}
