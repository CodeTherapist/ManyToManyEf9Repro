
# Bug Report

Code for Repro: https://github.com/dotnet/efcore/issues/36077

The model demonstrates a classic many-to-many relationship pattern in Entity Framework Core, 
with added time-series capabilities through TimescaleDB integration.

### Entities
1. **User**
    - Has a composite primary key (DateTime, Id)
    - Properties include: Id, DateTime, and Name
    - Part of a many-to-many relationship with UserRole

2. **UserRole**
    - Has a simple primary key (Id)
    - Properties include: Id and Name
    - Part of a many-to-many relationship with User

3. **UserToUserRole**
    - Junction/link table that implements the many-to-many relationship
    - Has its own primary key (Id)
    - Contains foreign keys to both User (UserDateTime, UserId) and UserRole (UserRoleId)

### Key Relationships
- Users can have multiple roles through the UserToUserRole junction table
- Roles can be assigned to multiple users through the same junction table
- The model uses the Entity Framework Core's fluent API to configure these relationships

### Special Features
- Uses TimescaleDB extension with a hypertable on the User table's DateTime column
- The User entity has a composite key which includes a timestamp, supporting time-series data functionality
- Database migration support is implemented in the application

Finding

* Removing the Hypertable does "solve" the issue.
* Strange behavior: when the item (User Entity) count is small (<6) it does work as expected. 
* Closing and reopening the connection (without disposing the object) between the insert operations for the 
  child records resolves the issue.
----

I’ve further investigated the issue by trying to isolate and reproduce it without Entity Framework Core. 
Specifically, I established the SQL connection manually and executed the commands with parameters 
directly—bypassing the EF context entirely. I also verified that all DateTime values 
(both in parent and child rows) match exactly down to the ticks, to rule out any subtle time shifts as the cause.

Despite these efforts, I still encounter the same exception and behavior. 
This suggests the issue may not be specific to EF Core, but potentially lies a layer deeper, 
possibly within Npgsql itself.

But that approach also failed.
I attempted a "SQL-only" method in PgAdmin following the initial database setup performed by the console application.
However the result was identical: the same error occurred at the exact same insert.

```cmd
ERROR:  Key (UserDateTime, UserId)=(2025-06-09 13:30:18.477113+00, 6) is not present in table "User".insert or update on table "UserToUserRole" violates foreign key constraint "FK_UserToUserRole_User_UserDateTime_UserId" 

ERROR:  insert or update on table "UserToUserRole" violates foreign key constraint "FK_UserToUserRole_User_UserDateTime_UserId"
SQL state: 23503
Detail: Key (UserDateTime, UserId)=(2025-06-09 13:30:18.477113+00, 6) is not present in table "User".
```

**Comparison: SQL Script vs. EF Core**

**SQL Script:**
`DETAIL: Key (UserDateTime, UserId) = (2025-06-09 13:30:18.477113+00, 6) is not present in table "User".`

**EF Core:**
`DETAIL: Key (UserDateTime, UserId) = (2025-06-09 14:42:32.207283+00, 6) is not present in table "User".`

*Note: The difference in timestamps is expected — EF Core generates a new timestamp on each run, while the script uses a static value.*

The SQL script:

```sql
INSERT INTO "User" ("DateTime", "Name")
VALUES ('2025-06-09 13:30:18.241049+00', 'User-1');

INSERT INTO "UserToUserRole" ("UserDateTime", "UserId", "UserRoleId")
VALUES ('2025-06-09 13:30:18.241049+00', 1, 1);

INSERT INTO "User" ("DateTime", "Name")
VALUES ('2025-06-09 13:30:18.390341+00', 'User-2');

INSERT INTO "UserToUserRole" ("UserDateTime", "UserId", "UserRoleId")
VALUES ('2025-06-09 13:30:18.390341+00', 2, 1);

INSERT INTO "User" ("DateTime", "Name")
VALUES ('2025-06-09 13:30:18.450911+00', 'User-3');

INSERT INTO "UserToUserRole" ("UserDateTime", "UserId", "UserRoleId")
VALUES ('2025-06-09 13:30:18.450911+00', 3, 1);

INSERT INTO "User" ("DateTime", "Name")
VALUES ('2025-06-09 13:30:18.462023+00', 'User-4');

INSERT INTO "UserToUserRole" ("UserDateTime", "UserId", "UserRoleId")
VALUES ('2025-06-09 13:30:18.462023+00', 4, 1);

INSERT INTO "User" ("DateTime", "Name")
VALUES ('2025-06-09 13:30:18.469193+00', 'User-5');

INSERT INTO "UserToUserRole" ("UserDateTime", "UserId", "UserRoleId")
VALUES ('2025-06-09 13:30:18.469193+00', 5, 1);

INSERT INTO "User" ("DateTime", "Name")
VALUES ('2025-06-09 13:30:18.477113+00', 'User-6');

INSERT INTO "UserToUserRole" ("UserDateTime", "UserId", "UserRoleId")
VALUES ('2025-06-09 13:30:18.477113+00', 6, 1);

INSERT INTO "User" ("DateTime", "Name")
VALUES ('2025-06-09 13:30:18.484287+00', 'User-7');

INSERT INTO "UserToUserRole" ("UserDateTime", "UserId", "UserRoleId")
VALUES ('2025-06-09 13:30:18.484287+00', 7, 1);

INSERT INTO "User" ("DateTime", "Name")
VALUES ('2025-06-09 13:30:18.49289+00', 'User-8');

INSERT INTO "UserToUserRole" ("UserDateTime", "UserId", "UserRoleId")
VALUES ('2025-06-09 13:30:18.49289+00', 8, 1);

INSERT INTO "User" ("DateTime", "Name")
VALUES ('2025-06-09 13:30:18.501791+00', 'User-9');

INSERT INTO "UserToUserRole" ("UserDateTime", "UserId", "UserRoleId")
VALUES ('2025-06-09 13:30:18.501791+00', 9, 1);

INSERT INTO "User" ("DateTime", "Name")
VALUES ('2025-06-09 13:30:18.510856+00', 'User-10');

INSERT INTO "UserToUserRole" ("UserDateTime", "UserId", "UserRoleId")
VALUES ('2025-06-09 13:30:18.510856+00', 10, 1);
```

The stacktrace from EF Core inserts by the console app:

```cmd
Unhandled exception. Microsoft.EntityFrameworkCore.DbUpdateException: An error occurred while saving the entity changes. See the inner exception for details.
---> Npgsql.PostgresException (0x80004005): 23503: insert or update on table "UserToUserRole" violates foreign key constraint "FK_UserToUserRole_User_UserDateTime_UserId"

DETAIL: Key (UserDateTime, UserId)=(2025-06-09 14:42:32.207283+00, 6) is not present in table "User".
at Npgsql.Internal.NpgsqlConnector.ReadMessageLong(Boolean async, DataRowLoadingMode dataRowLoadingMode, Boolean readingNotifications, Boolean isReadingPrependedMessage)
at System.Runtime.CompilerServices.PoolingAsyncValueTaskMethodBuilder`1.StateMachineBox`1.System.Threading.Tasks.Sources.IValueTaskSource<TResult>.GetResult(Int16 token)
at Npgsql.NpgsqlDataReader.NextResult(Boolean async, Boolean isConsuming, CancellationToken cancellationToken)
at Npgsql.NpgsqlDataReader.NextResult(Boolean async, Boolean isConsuming, CancellationToken cancellationToken)
at Npgsql.NpgsqlDataReader.NextResult()
at Npgsql.EntityFrameworkCore.PostgreSQL.Update.Internal.NpgsqlModificationCommandBatch.Consume(RelationalDataReader reader, Boolean async, CancellationToken cancellationToken)
Exception data:
Severity: ERROR
SqlState: 23503
MessageText: insert or update on table "UserToUserRole" violates foreign key constraint "FK_UserToUserRole_User_UserDateTime_UserId"
Detail: Key (UserDateTime, UserId)=(2025-06-09 14:42:32.207283+00, 6) is not present in table "User".
SchemaName: public
TableName: UserToUserRole
ConstraintName: FK_UserToUserRole_User_UserDateTime_UserId
File: ri_triggers.c
Line: 2599
Routine: ri_ReportViolation
--- End of inner exception stack trace ---
```
