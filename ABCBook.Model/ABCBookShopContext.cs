using ABCBook.Model.Entity;
using Microsoft.EntityFrameworkCore;

namespace ABCBook.Model
{
    public class ABCBookShopContext : DbContext
    {
        public ABCBookShopContext(DbContextOptions<ABCBookShopContext> options) : base(options)
        {
        }

        // Organization & Identity
        public DbSet<Store> Stores { get; set; }
        public DbSet<Warehouse> Warehouses { get; set; }
        public DbSet<POSRegister> POSRegisters { get; set; }
        public DbSet<AppUser> AppUsers { get; set; }
        public DbSet<Staff> Staffs { get; set; }

        // Catalog & Products
        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<BookDetail> BookDetails { get; set; }

        // Inventory
        public DbSet<StockBalance> StockBalances { get; set; }
        public DbSet<StockTransaction> StockTransactions { get; set; }
        public DbSet<TransferOrder> TransferOrders { get; set; }
        public DbSet<TransferOrderItem> TransferOrderItems { get; set; }

        // Purchasing & Suppliers
        public DbSet<Provider> Providers { get; set; }
        public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
        public DbSet<PurchaseOrderItem> PurchaseOrderItems { get; set; }
        public DbSet<GoodsReceiptNote> GoodsReceiptNotes { get; set; }
        public DbSet<GoodsReceiptNoteItem> GoodsReceiptNoteItems { get; set; }

        // POS & Sales
        public DbSet<ShiftSession> ShiftSessions { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<PaymentTransaction> PaymentTransactions { get; set; }

        // Finance & Expenses
        public DbSet<ExpenseCategory> ExpenseCategories { get; set; }
        public DbSet<ExpenseVoucher> ExpenseVouchers { get; set; }

        // Customer & Loyalty
        public DbSet<Customer> Customers { get; set; }
        public DbSet<LoyaltyPointLedger> LoyaltyPointLedgers { get; set; }

        // Audit
        public DbSet<AuditLog> AuditLogs { get; set; }

        // Legacy compatibility
        public DbSet<Book> Books { get; set; }
        public DbSet<Discount> Discounts { get; set; }
        public DbSet<Price> Prices { get; set; }
        public DbSet<Publisher> Publishers { get; set; }
        public DbSet<Salary> Salaries { get; set; }
        public DbSet<ShelvesLocation> ShelvesLocations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1. StockBalance: Composite Key (WarehouseId, ProductId)
            modelBuilder.Entity<StockBalance>()
                .HasKey(sb => new { sb.WarehouseId, sb.ProductId });

            modelBuilder.Entity<StockBalance>()
                .HasOne(sb => sb.Warehouse)
                .WithMany(w => w.StockBalances)
                .HasForeignKey(sb => sb.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StockBalance>()
                .HasOne(sb => sb.Product)
                .WithMany(p => p.StockBalances)
                .HasForeignKey(sb => sb.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // 2. BookDetail: 1-1 with Product
            modelBuilder.Entity<BookDetail>()
                .HasOne(b => b.Product)
                .WithOne(p => p.BookDetail)
                .HasForeignKey<BookDetail>(b => b.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // 2.1. AppUser: 1-1 with Staff
            modelBuilder.Entity<AppUser>()
                .HasOne(u => u.Staff)
                .WithOne(s => s.User)
                .HasForeignKey<Staff>(s => s.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            // 3. Unique Constraints
            modelBuilder.Entity<Store>()
                .HasIndex(s => s.Code)
                .IsUnique();

            modelBuilder.Entity<Warehouse>()
                .HasIndex(w => w.Code)
                .IsUnique();

            modelBuilder.Entity<AppUser>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<Product>()
                .HasIndex(p => p.SKU)
                .IsUnique();

            modelBuilder.Entity<Order>()
                .HasIndex(o => o.OrderNumber)
                .IsUnique();

            modelBuilder.Entity<Order>()
                .HasIndex(o => o.IdempotencyKey)
                .IsUnique();

            modelBuilder.Entity<ShiftSession>()
                .HasIndex(s => s.ShiftCode)
                .IsUnique();

            modelBuilder.Entity<TransferOrder>()
                .HasIndex(t => t.TransferCode)
                .IsUnique();

            modelBuilder.Entity<PurchaseOrder>()
                .HasIndex(po => po.POCode)
                .IsUnique();

            modelBuilder.Entity<GoodsReceiptNote>()
                .HasIndex(grn => grn.GRNCode)
                .IsUnique();

            modelBuilder.Entity<ExpenseVoucher>()
                .HasIndex(e => e.VoucherCode)
                .IsUnique();

            modelBuilder.Entity<Customer>()
                .HasIndex(c => c.CustomerCode)
                .IsUnique();

            // 4. StockTransaction relationships (Restrict deletion to preserve audit history)
            modelBuilder.Entity<StockTransaction>()
                .HasOne(st => st.Warehouse)
                .WithMany(w => w.StockTransactions)
                .HasForeignKey(st => st.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StockTransaction>()
                .HasOne(st => st.Product)
                .WithMany(p => p.StockTransactions)
                .HasForeignKey(st => st.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // 5. TransferOrder relationships
            modelBuilder.Entity<TransferOrder>()
                .HasOne(t => t.FromWarehouse)
                .WithMany()
                .HasForeignKey(t => t.FromWarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TransferOrder>()
                .HasOne(t => t.ToWarehouse)
                .WithMany()
                .HasForeignKey(t => t.ToWarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            // 6. Order relationships
            modelBuilder.Entity<Order>()
                .HasOne(o => o.Store)
                .WithMany()
                .HasForeignKey(o => o.StoreId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.ShiftSession)
                .WithMany(s => s.Orders)
                .HasForeignKey(o => o.ShiftSessionId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Order)
                .WithMany(o => o.Items)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PaymentTransaction>()
                .HasOne(pt => pt.Order)
                .WithMany(o => o.Payments)
                .HasForeignKey(pt => pt.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // 7. ExpenseVoucher relationships
            modelBuilder.Entity<ExpenseVoucher>()
                .HasOne(ev => ev.Store)
                .WithMany()
                .HasForeignKey(ev => ev.StoreId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ExpenseVoucher>()
                .HasOne(ev => ev.Category)
                .WithMany(c => c.Vouchers)
                .HasForeignKey(ev => ev.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // 8. Tắt Cascade Delete tự động cho toàn bộ hệ thống để bảo vệ dữ liệu tài chính và chống chu trình khóa
            foreach (var relationship in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            {
                if (relationship.DeclaringEntityType.ClrType != typeof(BookDetail) &&
                    relationship.DeclaringEntityType.ClrType != typeof(OrderItem) &&
                    relationship.DeclaringEntityType.ClrType != typeof(PaymentTransaction) &&
                    relationship.DeclaringEntityType.ClrType != typeof(PurchaseOrderItem) &&
                    relationship.DeclaringEntityType.ClrType != typeof(GoodsReceiptNoteItem) &&
                    relationship.DeclaringEntityType.ClrType != typeof(TransferOrderItem))
                {
                    relationship.DeleteBehavior = DeleteBehavior.Restrict;
                }
            }
        }
    }
}
