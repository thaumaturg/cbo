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

    /// <summary>
    /// A row is still referenced. Postgres reports 23503 for NO ACTION foreign keys and 23001 for the RESTRICT ones
    /// (<c>DeleteBehavior.Restrict</c>).
    /// </summary>
    public static bool IsForeignKeyViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation or PostgresErrorCodes.RestrictViolation };
}
