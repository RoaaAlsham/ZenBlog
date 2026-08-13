using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZenBlog.Persistence.Migrations
{
    /// <summary>
    /// Makes every foreign key onto AspNetUsers.Id ON UPDATE CASCADE.
    ///
    /// A user's local primary key is now their AuthDeep subject id. Existing readers
    /// were created before AuthDeep and hold a locally generated id, so the first time
    /// one signs in their row has to be re-keyed — and re-keying a primary key that
    /// seven tables point at is only a single statement if the database will carry the
    /// change through for us. Without this, the same operation needs a temporary row,
    /// a rename to dodge the unique email and username indexes, seven manual updates
    /// and a delete, all of which can half-apply.
    ///
    /// ON DELETE behaviour is preserved exactly as EF declared it: Cascade everywhere
    /// except Comments, which is Restrict so a comment cannot be silently removed with
    /// its author.
    ///
    /// The model itself is unchanged, so the snapshot does not move; this is a
    /// constraint-only migration expressed as raw SQL because EF's fluent API has no
    /// concept of ON UPDATE.
    /// </summary>
    public partial class authdeep_user_rekey_cascade : Migration
    {
        /// <summary>(table, constraint, on-delete action) for every FK onto AspNetUsers.Id.</summary>
        private static readonly (string Table, string Constraint, string OnDelete)[] UserForeignKeys =
        [
            ("AspNetUserClaims", "FK_AspNetUserClaims_AspNetUsers_UserId", "CASCADE"),
            ("AspNetUserLogins", "FK_AspNetUserLogins_AspNetUsers_UserId", "CASCADE"),
            ("AspNetUserRoles",  "FK_AspNetUserRoles_AspNetUsers_UserId",  "CASCADE"),
            ("AspNetUserTokens", "FK_AspNetUserTokens_AspNetUsers_UserId", "CASCADE"),
            ("Blogs",            "FK_Blogs_AspNetUsers_UserId",            "CASCADE"),
            ("Comments",         "FK_Comments_AspNetUsers_UserId",         "RESTRICT"),
            ("RefreshTokens",    "FK_RefreshTokens_AspNetUsers_UserId",    "CASCADE")
        ];

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            Rebuild(migrationBuilder, onUpdateCascade: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            Rebuild(migrationBuilder, onUpdateCascade: false);
        }

        /// <summary>
        /// Drops and recreates each constraint. IF EXISTS on the drop keeps the migration
        /// runnable against a database where a constraint was already renamed by hand.
        /// </summary>
        private static void Rebuild(MigrationBuilder migrationBuilder, bool onUpdateCascade)
        {
            var onUpdate = onUpdateCascade ? " ON UPDATE CASCADE" : string.Empty;

            foreach (var (table, constraint, onDelete) in UserForeignKeys)
            {
                migrationBuilder.Sql(
                    $"""
                     ALTER TABLE "{table}" DROP CONSTRAINT IF EXISTS "{constraint}";
                     ALTER TABLE "{table}"
                         ADD CONSTRAINT "{constraint}"
                         FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id")
                         ON DELETE {onDelete}{onUpdate};
                     """);
            }
        }
    }
}
