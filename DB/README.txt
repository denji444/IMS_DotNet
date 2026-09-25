========================================================================
Inventory Management System (IMS) - Database Setup Guide
========================================================================

This folder contains the complete database files for the project.
You can set up the database using either Method 1 (Recommended .bak Restore)
or Method 2 (Direct SQL Script Execution).

------------------------------------------------------------------------
METHOD 1: Restore Database using .BAK File in SSMS (Recommended)
------------------------------------------------------------------------
1. Open SQL Server Management Studio (SSMS) and connect to your SQL Server
   (e.g., .\SQLEXPRESS or localhost).
2. Right-click on the "Databases" node in Object Explorer -> select "Restore Database...".
3. Under "Source", select "Device", click the "..." button, and click "Add".
4. Browse and select the file:
      InventoryManagementDb.bak
5. Click OK. Verify the Destination Database name is:
      InventoryManagementDb
6. Click OK to complete the restore.

------------------------------------------------------------------------
METHOD 2: Run SQL Script (If .BAK Version Incompatible)
------------------------------------------------------------------------
If your SQL Server version is older or cannot restore the .bak file:
1. Open SQL Server Management Studio (SSMS).
2. Open the file:
      InventoryManagementDb_Schema.sql
3. Execute the script to create all tables, keys, and indexes.

------------------------------------------------------------------------
METHOD 3: Automatic EF Core Migration
------------------------------------------------------------------------
The project has automatic migrations enabled on startup in Program.cs.
When you launch the ASP.NET Core project, it will automatically connect to
your SQL Server and apply any pending migrations.

------------------------------------------------------------------------
Connection String Configuration
------------------------------------------------------------------------
Ensure the connection string in 'InventoryManagementSystem/appsettings.json'
matches your local SQL Server instance:

"ConnectionStrings": {
  "DefaultConnection": "Server=.\\SQLEXPRESS;Database=InventoryManagementDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
}

* Note: If your SQL Server instance is the default (MSSQLSERVER), change
  Server=.\\SQLEXPRESS to Server=localhost or Server=.

------------------------------------------------------------------------
Default System Administrator Login Credentials
------------------------------------------------------------------------
- Username / Email : admin@ims.com (or username 'admin')
- Password         : Admin@123
- Role             : Admin
========================================================================
