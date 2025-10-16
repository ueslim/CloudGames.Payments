using Microsoft.EntityFrameworkCore.Migrations;

namespace CloudGames.Payments.Infra.Migrations;

public partial class InitPaymentsAndOutbox : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[Payments]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Payments](
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [GameId] uniqueidentifier NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [Status] nvarchar(50) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Payments] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END

IF OBJECT_ID(N'[dbo].[OutboxMessages]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[OutboxMessages](
        [Id] uniqueidentifier NOT NULL,
        [Type] nvarchar(200) NOT NULL,
        [Payload] nvarchar(max) NOT NULL,
        [OccurredAt] datetime2 NOT NULL,
        [ProcessedAt] datetime2 NULL,
        CONSTRAINT [PK_OutboxMessages] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END

IF OBJECT_ID(N'[dbo].[PaymentEvents]', N'U') IS NOT NULL
BEGIN
    DROP TABLE [dbo].[PaymentEvents];
END

");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[OutboxMessages]', N'U') IS NOT NULL DROP TABLE [dbo].[OutboxMessages];
IF OBJECT_ID(N'[dbo].[Payments]', N'U') IS NOT NULL DROP TABLE [dbo].[Payments];
");
    }
}


