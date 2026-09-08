using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using InventoryManagementSystem.Models;

namespace InventoryManagementSystem.Data
{
    public class InventoryDbContext : IdentityDbContext<ApplicationUser>
    {
        public InventoryDbContext(DbContextOptions<InventoryDbContext> options)
            : base(options)
        {
        }

        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Purchase> Purchases { get; set; }
        public DbSet<Sale> Sales { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<EmployeeAttendance> EmployeeAttendances { get; set; }
        public DbSet<EmployeeLeave> EmployeeLeaves { get; set; }
        public DbSet<PurchaseInstallment> PurchaseInstallments { get; set; }
        public DbSet<SaleInstallment> SaleInstallments { get; set; }
        public DbSet<ProductCategory> ProductCategories { get; set; }
        public DbSet<ProductCategoryTypeOption> ProductCategoryTypeOptions { get; set; }
        public DbSet<SmtpSetting> SmtpSettings { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Product SKU must be unique
            builder.Entity<Product>()
                .HasIndex(p => p.Sku)
                .IsUnique();

            // Supplier Name must be unique
            builder.Entity<Supplier>()
                .HasIndex(s => s.Name)
                .IsUnique();

            // Purchase No must be unique
            builder.Entity<Purchase>()
                .HasIndex(p => p.PurchaseNo)
                .IsUnique();

            // Invoice No must be unique
            builder.Entity<Sale>()
                .HasIndex(s => s.InvoiceNo)
                .IsUnique();


            builder.Entity<Purchase>()
                .HasOne(p => p.Product)
                .WithMany()
                .HasForeignKey(p => p.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Purchase>()
                .HasOne(p => p.Supplier)
                .WithMany()
                .HasForeignKey(p => p.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Sale>()
                .HasOne(s => s.Product)
                .WithMany()
                .HasForeignKey(s => s.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Sale>()
                .HasOne(s => s.Customer)
                .WithMany()
                .HasForeignKey(s => s.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Department configuration
            builder.Entity<Department>()
                .HasIndex(d => d.Name)
                .IsUnique();

            // Employee configurations
            builder.Entity<Employee>()
                .HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Employee>()
                .HasOne(e => e.Department)
                .WithMany()
                .HasForeignKey(e => e.DepartmentId)
                .OnDelete(DeleteBehavior.SetNull);

            // Supplier User configuration
            builder.Entity<Supplier>()
                .HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Attendance configuration
            builder.Entity<EmployeeAttendance>()
                .HasOne(ea => ea.Employee)
                .WithMany()
                .HasForeignKey(ea => ea.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            // Leave configuration
            builder.Entity<EmployeeLeave>()
                .HasOne(el => el.Employee)
                .WithMany()
                .HasForeignKey(el => el.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            // Purchase Installment configuration
            builder.Entity<PurchaseInstallment>()
                .HasOne(pi => pi.Purchase)
                .WithMany(p => p.Installments)
                .HasForeignKey(pi => pi.PurchaseId)
                .OnDelete(DeleteBehavior.Cascade);

            // Sale Installment configuration
            builder.Entity<SaleInstallment>()
                .HasOne(si => si.Sale)
                .WithMany(s => s.Installments)
                .HasForeignKey(si => si.SaleId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
