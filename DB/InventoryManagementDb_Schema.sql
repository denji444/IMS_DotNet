IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810092504_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetRoles] (
        [Id] nvarchar(450) NOT NULL,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810092504_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUsers] (
        [Id] nvarchar(450) NOT NULL,
        [FullName] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UserName] nvarchar(256) NULL,
        [NormalizedUserName] nvarchar(256) NULL,
        [Email] nvarchar(256) NULL,
        [NormalizedEmail] nvarchar(256) NULL,
        [EmailConfirmed] bit NOT NULL,
        [PasswordHash] nvarchar(max) NULL,
        [SecurityStamp] nvarchar(max) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [PhoneNumberConfirmed] bit NOT NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [LockoutEnd] datetimeoffset NULL,
        [LockoutEnabled] bit NOT NULL,
        [AccessFailedCount] int NOT NULL,
        CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810092504_InitialCreate'
)
BEGIN
    CREATE TABLE [Suppliers] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [ContactName] nvarchar(100) NOT NULL,
        [Email] nvarchar(100) NOT NULL,
        [Phone] nvarchar(20) NOT NULL,
        [Address] nvarchar(250) NOT NULL,
        CONSTRAINT [PK_Suppliers] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810092504_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetRoleClaims] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810092504_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserClaims] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810092504_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserLogins] (
        [LoginProvider] nvarchar(450) NOT NULL,
        [ProviderKey] nvarchar(450) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810092504_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserRoles] (
        [UserId] nvarchar(450) NOT NULL,
        [RoleId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810092504_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserTokens] (
        [UserId] nvarchar(450) NOT NULL,
        [LoginProvider] nvarchar(450) NOT NULL,
        [Name] nvarchar(450) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810092504_InitialCreate'
)
BEGIN
    CREATE TABLE [Products] (
        [Id] int NOT NULL IDENTITY,
        [Sku] nvarchar(50) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Description] nvarchar(500) NOT NULL,
        [SupplierId] int NOT NULL,
        [Price] decimal(18,2) NOT NULL,
        [StockQuantity] int NOT NULL,
        CONSTRAINT [PK_Products] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Products_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Suppliers] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810092504_InitialCreate'
)
BEGIN
    CREATE TABLE [Purchases] (
        [Id] int NOT NULL IDENTITY,
        [PurchaseNo] nvarchar(50) NOT NULL,
        [ProductId] int NOT NULL,
        [SupplierId] int NOT NULL,
        [Quantity] int NOT NULL,
        [UnitPrice] decimal(18,2) NOT NULL,
        [TotalCost] decimal(18,2) NOT NULL,
        [PurchaseDate] datetime2 NOT NULL,
        [Notes] nvarchar(500) NOT NULL,
        CONSTRAINT [PK_Purchases] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Purchases_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Purchases_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Suppliers] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810092504_InitialCreate'
)
BEGIN
    CREATE TABLE [Sales] (
        [Id] int NOT NULL IDENTITY,
        [InvoiceNo] nvarchar(50) NOT NULL,
        [ProductId] int NOT NULL,
        [CustomerId] nvarchar(450) NOT NULL,
        [Quantity] int NOT NULL,
        [UnitPrice] decimal(18,2) NOT NULL,
        [TotalAmount] decimal(18,2) NOT NULL,
        [SaleDate] datetime2 NOT NULL,
        [Notes] nvarchar(500) NOT NULL,
        CONSTRAINT [PK_Sales] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Sales_AspNetUsers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Sales_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810092504_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810092504_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810092504_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810092504_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810092504_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810092504_InitialCreate'
)
BEGIN
    CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810092504_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810092504_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Products_Sku] ON [Products] ([Sku]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810092504_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Products_SupplierId] ON [Products] ([SupplierId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810092504_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Purchases_ProductId] ON [Purchases] ([ProductId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810092504_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Purchases_PurchaseNo] ON [Purchases] ([PurchaseNo]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810092504_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Purchases_SupplierId] ON [Purchases] ([SupplierId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810092504_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Sales_CustomerId] ON [Sales] ([CustomerId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810092504_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Sales_InvoiceNo] ON [Sales] ([InvoiceNo]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810092504_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Sales_ProductId] ON [Sales] ([ProductId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810092504_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260810092504_InitialCreate', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811104448_AddUniqueSupplierName'
)
BEGIN
                    UPDATE Products
                    SET SupplierId = MasterSuppliers.MasterId
                    FROM Products p
                    INNER JOIN Suppliers s ON p.SupplierId = s.Id
                    INNER JOIN (
                        SELECT Name, MIN(Id) AS MasterId
                        FROM Suppliers
                        GROUP BY Name
                    ) MasterSuppliers ON s.Name = MasterSuppliers.Name
                    WHERE s.Id <> MasterSuppliers.MasterId;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811104448_AddUniqueSupplierName'
)
BEGIN
                    DELETE s1 
                    FROM Suppliers s1 
                    INNER JOIN Suppliers s2 ON s1.Name = s2.Name AND s1.Id > s2.Id;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811104448_AddUniqueSupplierName'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Suppliers_Name] ON [Suppliers] ([Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811104448_AddUniqueSupplierName'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260811104448_AddUniqueSupplierName', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260817101039_AddSupplierEmailVerification'
)
BEGIN
    ALTER TABLE [Suppliers] ADD [EmailVerificationToken] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260817101039_AddSupplierEmailVerification'
)
BEGIN
    ALTER TABLE [Suppliers] ADD [IsEmailVerified] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260817101039_AddSupplierEmailVerification'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260817101039_AddSupplierEmailVerification', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818061214_RemoveSupplierFromProduct'
)
BEGIN
    ALTER TABLE [Products] DROP CONSTRAINT [FK_Products_Suppliers_SupplierId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818061214_RemoveSupplierFromProduct'
)
BEGIN
    DROP INDEX [IX_Products_SupplierId] ON [Products];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818061214_RemoveSupplierFromProduct'
)
BEGIN
    DECLARE @var0 sysname;
    SELECT @var0 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Products]') AND [c].[name] = N'SupplierId');
    IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [Products] DROP CONSTRAINT [' + @var0 + '];');
    ALTER TABLE [Products] DROP COLUMN [SupplierId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818061214_RemoveSupplierFromProduct'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260818061214_RemoveSupplierFromProduct', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818115153_AddVariantToProduct'
)
BEGIN
    ALTER TABLE [Products] ADD [Variant] nvarchar(100) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818115153_AddVariantToProduct'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260818115153_AddVariantToProduct', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260820114526_AddUnifiedPersonSchema'
)
BEGIN
    ALTER TABLE [Suppliers] ADD [UserId] nvarchar(450) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260820114526_AddUnifiedPersonSchema'
)
BEGIN
    CREATE TABLE [Departments] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [Description] nvarchar(500) NOT NULL,
        CONSTRAINT [PK_Departments] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260820114526_AddUnifiedPersonSchema'
)
BEGIN
    CREATE TABLE [Employees] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [DepartmentId] int NULL,
        [Designation] nvarchar(100) NOT NULL,
        [HireDate] datetime2 NOT NULL,
        [Salary] decimal(18,2) NOT NULL,
        [Status] nvarchar(50) NOT NULL,
        CONSTRAINT [PK_Employees] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Employees_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Employees_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [Departments] ([Id]) ON DELETE SET NULL
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260820114526_AddUnifiedPersonSchema'
)
BEGIN
    CREATE TABLE [EmployeeAttendances] (
        [Id] int NOT NULL IDENTITY,
        [EmployeeId] int NOT NULL,
        [Date] datetime2 NOT NULL,
        [ClockIn] time NULL,
        [ClockOut] time NULL,
        [Status] nvarchar(50) NOT NULL,
        CONSTRAINT [PK_EmployeeAttendances] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EmployeeAttendances_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260820114526_AddUnifiedPersonSchema'
)
BEGIN
    CREATE TABLE [EmployeeLeaves] (
        [Id] int NOT NULL IDENTITY,
        [EmployeeId] int NOT NULL,
        [StartDate] datetime2 NOT NULL,
        [EndDate] datetime2 NOT NULL,
        [LeaveType] nvarchar(50) NOT NULL,
        [Status] nvarchar(50) NOT NULL,
        [Notes] nvarchar(500) NOT NULL,
        CONSTRAINT [PK_EmployeeLeaves] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EmployeeLeaves_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260820114526_AddUnifiedPersonSchema'
)
BEGIN
    CREATE INDEX [IX_Suppliers_UserId] ON [Suppliers] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260820114526_AddUnifiedPersonSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Departments_Name] ON [Departments] ([Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260820114526_AddUnifiedPersonSchema'
)
BEGIN
    CREATE INDEX [IX_EmployeeAttendances_EmployeeId] ON [EmployeeAttendances] ([EmployeeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260820114526_AddUnifiedPersonSchema'
)
BEGIN
    CREATE INDEX [IX_EmployeeLeaves_EmployeeId] ON [EmployeeLeaves] ([EmployeeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260820114526_AddUnifiedPersonSchema'
)
BEGIN
    CREATE INDEX [IX_Employees_DepartmentId] ON [Employees] ([DepartmentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260820114526_AddUnifiedPersonSchema'
)
BEGIN
    CREATE INDEX [IX_Employees_UserId] ON [Employees] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260820114526_AddUnifiedPersonSchema'
)
BEGIN
    ALTER TABLE [Suppliers] ADD CONSTRAINT [FK_Suppliers_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260820114526_AddUnifiedPersonSchema'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260820114526_AddUnifiedPersonSchema', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825060547_MakeNotesOptional'
)
BEGIN
    DECLARE @var1 sysname;
    SELECT @var1 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Sales]') AND [c].[name] = N'Notes');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Sales] DROP CONSTRAINT [' + @var1 + '];');
    ALTER TABLE [Sales] ALTER COLUMN [Notes] nvarchar(500) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825060547_MakeNotesOptional'
)
BEGIN
    DECLARE @var2 sysname;
    SELECT @var2 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Purchases]') AND [c].[name] = N'Notes');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [Purchases] DROP CONSTRAINT [' + @var2 + '];');
    ALTER TABLE [Purchases] ALTER COLUMN [Notes] nvarchar(500) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825060547_MakeNotesOptional'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260825060547_MakeNotesOptional', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825064142_AddLeaseInstallmentSystem'
)
BEGIN
    ALTER TABLE [Sales] ADD [DownPayment] decimal(18,2) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825064142_AddLeaseInstallmentSystem'
)
BEGIN
    ALTER TABLE [Sales] ADD [InstallmentFrequency] nvarchar(50) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825064142_AddLeaseInstallmentSystem'
)
BEGIN
    ALTER TABLE [Sales] ADD [InstallmentsCount] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825064142_AddLeaseInstallmentSystem'
)
BEGIN
    ALTER TABLE [Sales] ADD [PaymentMode] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825064142_AddLeaseInstallmentSystem'
)
BEGIN
    ALTER TABLE [Purchases] ADD [DownPayment] decimal(18,2) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825064142_AddLeaseInstallmentSystem'
)
BEGIN
    ALTER TABLE [Purchases] ADD [InstallmentFrequency] nvarchar(50) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825064142_AddLeaseInstallmentSystem'
)
BEGIN
    ALTER TABLE [Purchases] ADD [InstallmentsCount] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825064142_AddLeaseInstallmentSystem'
)
BEGIN
    ALTER TABLE [Purchases] ADD [PaymentMode] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825064142_AddLeaseInstallmentSystem'
)
BEGIN
    CREATE TABLE [PurchaseInstallments] (
        [Id] int NOT NULL IDENTITY,
        [PurchaseId] int NOT NULL,
        [InstallmentNumber] int NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [PaidAmount] decimal(18,2) NOT NULL,
        [DueDate] datetime2 NOT NULL,
        [PaymentDate] datetime2 NULL,
        [Status] nvarchar(50) NOT NULL,
        CONSTRAINT [PK_PurchaseInstallments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PurchaseInstallments_Purchases_PurchaseId] FOREIGN KEY ([PurchaseId]) REFERENCES [Purchases] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825064142_AddLeaseInstallmentSystem'
)
BEGIN
    CREATE TABLE [SaleInstallments] (
        [Id] int NOT NULL IDENTITY,
        [SaleId] int NOT NULL,
        [InstallmentNumber] int NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [PaidAmount] decimal(18,2) NOT NULL,
        [DueDate] datetime2 NOT NULL,
        [PaymentDate] datetime2 NULL,
        [Status] nvarchar(50) NOT NULL,
        CONSTRAINT [PK_SaleInstallments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SaleInstallments_Sales_SaleId] FOREIGN KEY ([SaleId]) REFERENCES [Sales] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825064142_AddLeaseInstallmentSystem'
)
BEGIN
    CREATE INDEX [IX_PurchaseInstallments_PurchaseId] ON [PurchaseInstallments] ([PurchaseId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825064142_AddLeaseInstallmentSystem'
)
BEGIN
    CREATE INDEX [IX_SaleInstallments_SaleId] ON [SaleInstallments] ([SaleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825064142_AddLeaseInstallmentSystem'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260825064142_AddLeaseInstallmentSystem', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260903120857_MakeProductPriceNullable'
)
BEGIN
    DROP INDEX [IX_EmployeeAttendances_EmployeeId] ON [EmployeeAttendances];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260903120857_MakeProductPriceNullable'
)
BEGIN
    DECLARE @var3 sysname;
    SELECT @var3 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Products]') AND [c].[name] = N'Price');
    IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [Products] DROP CONSTRAINT [' + @var3 + '];');
    ALTER TABLE [Products] ALTER COLUMN [Price] decimal(18,2) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260903120857_MakeProductPriceNullable'
)
BEGIN
    CREATE UNIQUE INDEX [IX_EmployeeAttendances_EmployeeId_Date] ON [EmployeeAttendances] ([EmployeeId], [Date]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260903120857_MakeProductPriceNullable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260903120857_MakeProductPriceNullable', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904062516_AddBatchNumberToPurchaseAndSale'
)
BEGIN
    ALTER TABLE [Sales] ADD [BatchNumber] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904062516_AddBatchNumberToPurchaseAndSale'
)
BEGIN
    ALTER TABLE [Purchases] ADD [BatchNumber] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904062516_AddBatchNumberToPurchaseAndSale'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260904062516_AddBatchNumberToPurchaseAndSale', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904094643_AddProductCategoryAndTypes'
)
BEGIN
    ALTER TABLE [Products] ADD [CategoryName] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904094643_AddProductCategoryAndTypes'
)
BEGIN
    ALTER TABLE [Products] ADD [ProductType] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904094643_AddProductCategoryAndTypes'
)
BEGIN
    CREATE TABLE [ProductCategories] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [Description] nvarchar(300) NULL,
        CONSTRAINT [PK_ProductCategories] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904094643_AddProductCategoryAndTypes'
)
BEGIN
    CREATE TABLE [ProductCategoryTypeOptions] (
        [Id] int NOT NULL IDENTITY,
        [ProductCategoryId] int NOT NULL,
        [TypeName] nvarchar(100) NOT NULL,
        CONSTRAINT [PK_ProductCategoryTypeOptions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProductCategoryTypeOptions_ProductCategories_ProductCategoryId] FOREIGN KEY ([ProductCategoryId]) REFERENCES [ProductCategories] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904094643_AddProductCategoryAndTypes'
)
BEGIN
    CREATE INDEX [IX_ProductCategoryTypeOptions_ProductCategoryId] ON [ProductCategoryTypeOptions] ([ProductCategoryId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904094643_AddProductCategoryAndTypes'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260904094643_AddProductCategoryAndTypes', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908061139_AddSmtpSettingsTable'
)
BEGIN
    CREATE TABLE [SmtpSettings] (
        [Id] int NOT NULL IDENTITY,
        [Server] nvarchar(200) NOT NULL,
        [Port] int NOT NULL,
        [SenderName] nvarchar(100) NOT NULL,
        [SenderEmail] nvarchar(150) NOT NULL,
        [Username] nvarchar(150) NOT NULL,
        [Password] nvarchar(255) NOT NULL,
        [EnableSsl] bit NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_SmtpSettings] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908061139_AddSmtpSettingsTable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260908061139_AddSmtpSettingsTable', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909054638_AddPaymentMethodOptions'
)
BEGIN
    ALTER TABLE [Sales] ADD [BankName] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909054638_AddPaymentMethodOptions'
)
BEGIN
    ALTER TABLE [Sales] ADD [CheckDate] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909054638_AddPaymentMethodOptions'
)
BEGIN
    ALTER TABLE [Sales] ADD [PaymentMethod] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909054638_AddPaymentMethodOptions'
)
BEGIN
    ALTER TABLE [Sales] ADD [PaymentReference] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909054638_AddPaymentMethodOptions'
)
BEGIN
    ALTER TABLE [SaleInstallments] ADD [BankName] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909054638_AddPaymentMethodOptions'
)
BEGIN
    ALTER TABLE [SaleInstallments] ADD [CheckDate] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909054638_AddPaymentMethodOptions'
)
BEGIN
    ALTER TABLE [SaleInstallments] ADD [PaymentMethod] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909054638_AddPaymentMethodOptions'
)
BEGIN
    ALTER TABLE [SaleInstallments] ADD [PaymentReference] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909054638_AddPaymentMethodOptions'
)
BEGIN
    ALTER TABLE [Purchases] ADD [BankName] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909054638_AddPaymentMethodOptions'
)
BEGIN
    ALTER TABLE [Purchases] ADD [CheckDate] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909054638_AddPaymentMethodOptions'
)
BEGIN
    ALTER TABLE [Purchases] ADD [PaymentMethod] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909054638_AddPaymentMethodOptions'
)
BEGIN
    ALTER TABLE [Purchases] ADD [PaymentReference] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909054638_AddPaymentMethodOptions'
)
BEGIN
    ALTER TABLE [PurchaseInstallments] ADD [BankName] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909054638_AddPaymentMethodOptions'
)
BEGIN
    ALTER TABLE [PurchaseInstallments] ADD [CheckDate] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909054638_AddPaymentMethodOptions'
)
BEGIN
    ALTER TABLE [PurchaseInstallments] ADD [PaymentMethod] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909054638_AddPaymentMethodOptions'
)
BEGIN
    ALTER TABLE [PurchaseInstallments] ADD [PaymentReference] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909054638_AddPaymentMethodOptions'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260909054638_AddPaymentMethodOptions', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909064028_AddMultiItemSalesAndPurchases'
)
BEGIN
    DECLARE @var4 sysname;
    SELECT @var4 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Sales]') AND [c].[name] = N'ProductId');
    IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [Sales] DROP CONSTRAINT [' + @var4 + '];');
    ALTER TABLE [Sales] ALTER COLUMN [ProductId] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909064028_AddMultiItemSalesAndPurchases'
)
BEGIN
    DECLARE @var5 sysname;
    SELECT @var5 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Purchases]') AND [c].[name] = N'ProductId');
    IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [Purchases] DROP CONSTRAINT [' + @var5 + '];');
    ALTER TABLE [Purchases] ALTER COLUMN [ProductId] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909064028_AddMultiItemSalesAndPurchases'
)
BEGIN
    CREATE TABLE [PurchaseItems] (
        [Id] int NOT NULL IDENTITY,
        [PurchaseId] int NOT NULL,
        [ProductId] int NOT NULL,
        [BatchNumber] nvarchar(100) NULL,
        [Quantity] int NOT NULL,
        [UnitPrice] decimal(18,2) NOT NULL,
        [TotalCost] decimal(18,2) NOT NULL,
        CONSTRAINT [PK_PurchaseItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PurchaseItems_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PurchaseItems_Purchases_PurchaseId] FOREIGN KEY ([PurchaseId]) REFERENCES [Purchases] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909064028_AddMultiItemSalesAndPurchases'
)
BEGIN
    CREATE TABLE [SaleItems] (
        [Id] int NOT NULL IDENTITY,
        [SaleId] int NOT NULL,
        [ProductId] int NOT NULL,
        [BatchNumber] nvarchar(100) NULL,
        [Quantity] int NOT NULL,
        [UnitPrice] decimal(18,2) NOT NULL,
        [TotalAmount] decimal(18,2) NOT NULL,
        CONSTRAINT [PK_SaleItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SaleItems_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SaleItems_Sales_SaleId] FOREIGN KEY ([SaleId]) REFERENCES [Sales] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909064028_AddMultiItemSalesAndPurchases'
)
BEGIN
    CREATE INDEX [IX_PurchaseItems_ProductId] ON [PurchaseItems] ([ProductId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909064028_AddMultiItemSalesAndPurchases'
)
BEGIN
    CREATE INDEX [IX_PurchaseItems_PurchaseId] ON [PurchaseItems] ([PurchaseId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909064028_AddMultiItemSalesAndPurchases'
)
BEGIN
    CREATE INDEX [IX_SaleItems_ProductId] ON [SaleItems] ([ProductId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909064028_AddMultiItemSalesAndPurchases'
)
BEGIN
    CREATE INDEX [IX_SaleItems_SaleId] ON [SaleItems] ([SaleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909064028_AddMultiItemSalesAndPurchases'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260909064028_AddMultiItemSalesAndPurchases', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909072855_AddCnicToCustomerAndSupplier'
)
BEGIN
    ALTER TABLE [Suppliers] ADD [Cnic] nvarchar(20) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909072855_AddCnicToCustomerAndSupplier'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [Cnic] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260909072855_AddCnicToCustomerAndSupplier'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260909072855_AddCnicToCustomerAndSupplier', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260911105054_AddBatchRatesToPurchaseItem'
)
BEGIN
    ALTER TABLE [Purchases] ADD [DemandRate] decimal(18,2) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260911105054_AddBatchRatesToPurchaseItem'
)
BEGIN
    ALTER TABLE [Purchases] ADD [FixRate] decimal(18,2) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260911105054_AddBatchRatesToPurchaseItem'
)
BEGIN
    ALTER TABLE [PurchaseItems] ADD [DemandRate] decimal(18,2) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260911105054_AddBatchRatesToPurchaseItem'
)
BEGIN
    ALTER TABLE [PurchaseItems] ADD [FixRate] decimal(18,2) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260911105054_AddBatchRatesToPurchaseItem'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260911105054_AddBatchRatesToPurchaseItem', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916044610_AddCompanyProfileTable'
)
BEGIN
    CREATE TABLE [CompanyProfiles] (
        [Id] int NOT NULL IDENTITY,
        [CompanyName] nvarchar(max) NOT NULL,
        [Tagline] nvarchar(max) NOT NULL,
        [Address] nvarchar(max) NOT NULL,
        [Phone] nvarchar(max) NOT NULL,
        [Email] nvarchar(max) NOT NULL,
        [LogoPath] nvarchar(max) NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_CompanyProfiles] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916044610_AddCompanyProfileTable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260916044610_AddCompanyProfileTable', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260923102046_AddBarcodeToProduct'
)
BEGIN
    ALTER TABLE [Products] ADD [Barcode] nvarchar(50) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260923102046_AddBarcodeToProduct'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Products_Barcode] ON [Products] ([Barcode]) WHERE [Barcode] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260923102046_AddBarcodeToProduct'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260923102046_AddBarcodeToProduct', N'8.0.0');
END;
GO

COMMIT;
GO

