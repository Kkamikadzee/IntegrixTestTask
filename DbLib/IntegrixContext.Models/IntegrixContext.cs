using System;
using System.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

#nullable disable

namespace DbLib.IntegrixContext.Models
{
    public partial class IntegrixContext : DbContext
    {
        public IntegrixContext()
        {
        }

        public IntegrixContext(DbContextOptions<IntegrixContext> options)
            : base(options)
        {
        }

        public virtual DbSet<NotReservedItem> NotReservedItems { get; set; }
        public virtual DbSet<Order> Orders { get; set; }
        public virtual DbSet<OrderItem> OrderItems { get; set; }
        public virtual DbSet<Product> Products { get; set; }
        public virtual DbSet<ProductInfoInWarehouse> ProductInfoInWarehouses { get; set; }
        public virtual DbSet<User> Users { get; set; }
        public virtual DbSet<Warehouse> Warehouses { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseNpgsql(
                    ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None).
                    ConnectionStrings.ConnectionStrings["Database"].ConnectionString);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasAnnotation("Relational:Collation", "Russian_Russia.1251");

            modelBuilder.Entity<NotReservedItem>(entity =>
            {
                entity.HasKey(e => e.NotReservedItemsId)
                    .HasName("not_reserved_items_pkey");

                entity.ToTable("not_reserved_items");

                entity.Property(e => e.NotReservedItemsId).HasColumnName("not_reserved_items_id");

                entity.Property(e => e.ProductId).HasColumnName("product_id");

                entity.Property(e => e.Quantity).HasColumnName("quantity");

                entity.Property(e => e.Reason)
                    .HasMaxLength(128)
                    .HasColumnName("reason");

                entity.Property(e => e.ReservationDate).HasColumnName("reservation_date");

                entity.Property(e => e.UserId).HasColumnName("user_id");

                entity.Property(e => e.WarehouseId).HasColumnName("warehouse_id");

                entity.HasOne(d => d.Product)
                    .WithMany(p => p.NotReservedItems)
                    .HasForeignKey(d => d.ProductId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("fk_product_and_not_reserved_items");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.NotReservedItems)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("fk_user_and_not_reserved_items");

                entity.HasOne(d => d.Warehouse)
                    .WithMany(p => p.NotReservedItems)
                    .HasForeignKey(d => d.WarehouseId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("fk_warehouse_and_not_reserved_items");
            });

            modelBuilder.Entity<Order>(entity =>
            {
                entity.ToTable("orders");

                entity.Property(e => e.OrderId).HasColumnName("order_id");

                entity.Property(e => e.OrderDate).HasColumnName("order_date");

                entity.Property(e => e.UserId).HasColumnName("user_id");

                entity.Property(e => e.WarehouseId).HasColumnName("warehouse_id");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.Orders)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("fk_user_and_orders");

                entity.HasOne(d => d.Warehouse)
                    .WithMany(p => p.Orders)
                    .HasForeignKey(d => d.WarehouseId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("fk_warehouse_and_orders");
            });

            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.HasKey(e => e.OrderItemsId)
                    .HasName("order_items_pkey");

                entity.ToTable("order_items");

                entity.Property(e => e.OrderItemsId).HasColumnName("order_items_id");

                entity.Property(e => e.OrderId).HasColumnName("order_id");

                entity.Property(e => e.ProductId).HasColumnName("product_id");

                entity.Property(e => e.Quantity).HasColumnName("quantity");

                entity.HasOne(d => d.Order)
                    .WithMany(p => p.OrderItems)
                    .HasForeignKey(d => d.OrderId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("fk_order_and_oreder_items");

                entity.HasOne(d => d.Product)
                    .WithMany(p => p.OrderItems)
                    .HasForeignKey(d => d.ProductId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("fk_product_and_order_items");
            });

            modelBuilder.Entity<Product>(entity =>
            {
                entity.ToTable("products");

                entity.Property(e => e.ProductId).HasColumnName("product_id");

                entity.Property(e => e.FullName)
                    .IsRequired()
                    .HasMaxLength(16)
                    .HasColumnName("full_name");
            });

            modelBuilder.Entity<ProductInfoInWarehouse>(entity =>
            {
                entity.HasKey(e => e.ProdInfoWrhsId)
                    .HasName("product_info_in_warehouse_pkey");

                entity.ToTable("product_info_in_warehouse");

                entity.Property(e => e.ProdInfoWrhsId).HasColumnName("prod_info_wrhs_id");

                entity.Property(e => e.ProductId).HasColumnName("product_id");

                entity.Property(e => e.QuantityFreeProduct).HasColumnName("quantity_free_product");

                entity.Property(e => e.QuantityReservedProduct).HasColumnName("quantity_reserved_product");

                entity.Property(e => e.WarehouseId).HasColumnName("warehouse_id");

                entity.HasOne(d => d.Product)
                    .WithMany(p => p.ProductInfoInWarehouses)
                    .HasForeignKey(d => d.ProductId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("fk_product_and_prod_info_wrhs");

                entity.HasOne(d => d.Warehouse)
                    .WithMany(p => p.ProductInfoInWarehouses)
                    .HasForeignKey(d => d.WarehouseId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("fk_warehouse_and_prod_info_wrhs");
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("users");

                entity.Property(e => e.UserId).HasColumnName("user_id");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(16)
                    .HasColumnName("name");
            });

            modelBuilder.Entity<Warehouse>(entity =>
            {
                entity.ToTable("warehouses");

                entity.Property(e => e.WarehouseId).HasColumnName("warehouse_id");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(16)
                    .HasColumnName("name");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
