using ABCBook.Model.Entity;
using ABCBook.Model.Enum;
using Microsoft.EntityFrameworkCore;

namespace ABCBook.Model.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(ABCBookShopContext context)
        {
            // Ensure database is created/migrated
            await context.Database.MigrateAsync();

            // 1. Seed Stores
            if (!await context.Stores.AnyAsync())
            {
                var store1 = new Store
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Code = "STORE01",
                    Name = "Nhà sách ABC - Trụ sở Quận 1",
                    Address = "123 Đường Nguyễn Huệ, Phường Bến Nghé, Quận 1",
                    City = "Hồ Chí Minh",
                    Phone = "028.3822.9999",
                    Email = "store01@abcbookshop.vn",
                    OpeningHours = "08:00 - 22:00",
                    IsActive = true
                };

                var store2 = new Store
                {
                    Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Code = "STORE02",
                    Name = "Nhà sách ABC - Chi nhánh Cầu Giấy",
                    Address = "456 Đường Cầu Giấy, Quận Cầu Giấy",
                    City = "Hà Nội",
                    Phone = "024.3799.8888",
                    Email = "store02@abcbookshop.vn",
                    OpeningHours = "08:00 - 22:00",
                    IsActive = true
                };

                await context.Stores.AddRangeAsync(store1, store2);
                await context.SaveChangesAsync();

                // 2. Seed Warehouses
                var whCentral = new Warehouse
                {
                    Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                    StoreId = store1.Id,
                    Code = "WH-CENTRAL",
                    Name = "Kho Tổng Trung Tâm (TP.HCM)",
                    Address = "123 Nguyễn Huệ, Quận 1, TP.HCM",
                    IsCentralWarehouse = true,
                    IsActive = true
                };

                var whStore1 = new Warehouse
                {
                    Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                    StoreId = store1.Id,
                    Code = "WH-STORE01",
                    Name = "Kho Bán lẻ Q1",
                    Address = "Khu vực bán lẻ tầng trệt Q1",
                    IsCentralWarehouse = false,
                    IsActive = true
                };

                var whStore2 = new Warehouse
                {
                    Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                    StoreId = store2.Id,
                    Code = "WH-STORE02",
                    Name = "Kho Bán lẻ Cầu Giấy",
                    Address = "456 Cầu Giấy, Hà Nội",
                    IsCentralWarehouse = false,
                    IsActive = true
                };

                await context.Warehouses.AddRangeAsync(whCentral, whStore1, whStore2);

                // 3. Seed POS Registers
                var pos1 = new POSRegister
                {
                    Id = Guid.Parse("d1111111-1111-1111-1111-111111111111"),
                    StoreId = store1.Id,
                    RegisterCode = "POS-01",
                    Name = "Quầy Thu Ngân 01 (Cửa vào)",
                    IsActive = true
                };

                var pos2 = new POSRegister
                {
                    Id = Guid.Parse("d2222222-2222-2222-2222-222222222222"),
                    StoreId = store1.Id,
                    RegisterCode = "POS-02",
                    Name = "Quầy Thu Ngân 02 (Tầng 1)",
                    IsActive = true
                };

                await context.POSRegisters.AddRangeAsync(pos1, pos2);
                await context.SaveChangesAsync();
            }

            // 4. Seed Categories
            if (!await context.Categories.AnyAsync())
            {
                var catBooks = new Category
                {
                    Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    Code = "CAT-BOOKS",
                    Name = "Sách Văn Học & Kỹ Năng",
                    Description = "Sách văn học trong nước và kinh doanh phát triển bản thân",
                    DisplayOrder = 1,
                    IsActive = true
                };

                var catStationery = new Category
                {
                    Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                    Code = "CAT-STATIONERY",
                    Name = "Văn Phòng Phẩm",
                    Description = "Bút, sổ tay, thước kẻ, kẹp giấy",
                    DisplayOrder = 2,
                    IsActive = true
                };

                var catSouvenirs = new Category
                {
                    Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                    Code = "CAT-SOUVENIR",
                    Name = "Quà Tặng & Lưu Niệm",
                    Description = "Bookmark, móc khóa, tượng lưu niệm",
                    DisplayOrder = 3,
                    IsActive = true
                };

                await context.Categories.AddRangeAsync(catBooks, catStationery, catSouvenirs);
                await context.SaveChangesAsync();
            }

            // 5. Seed Users & Staff
            if (!await context.AppUsers.AnyAsync())
            {
                var store1 = await context.Stores.FirstAsync();

                // Mật khẩu mặc định an toàn được mã hóa bằng BCrypt: "Admin@123"
                string passwordHash = "$2a$11$N.d.r7x2fE89r0oM1O0EJuIq1P0i0q5T0a0P0o0u0w0z0y0x0w0v0"; // Placeholder safe hash
                // Or compute live:
                passwordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123");

                var adminUser = new AppUser
                {
                    Id = Guid.Parse("99999999-9999-9999-9999-999999999999"),
                    Username = "admin",
                    PasswordHash = passwordHash,
                    FullName = "Hệ thống Quản Trị Viên Chuỗi",
                    Email = "admin@abcbookshop.vn",
                    PhoneNumber = "0909000001",
                    Role = SystemRole.SysChainOwner,
                    AssignedStoreId = null, // Toàn quyền chuỗi
                    IsActive = true
                };

                var cashierUser = new AppUser
                {
                    Id = Guid.Parse("88888888-8888-8888-8888-888888888888"),
                    Username = "cashier01",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Cashier@123"),
                    FullName = "Nguyễn Văn Thu Ngân",
                    Email = "cashier01@abcbookshop.vn",
                    PhoneNumber = "0909000002",
                    Role = SystemRole.Cashier,
                    AssignedStoreId = store1.Id,
                    IsActive = true
                };

                var staffMember = new Staff
                {
                    Id = Guid.Parse("77777777-7777-7777-7777-777777777777"),
                    StaffCode = "NV001",
                    Name = "Nguyễn Văn Thu Ngân",
                    StoreID = store1.Id,
                    UserId = cashierUser.Id,
                    gender = Gender.Male,
                    DateOfBirth = new DateTime(1998, 5, 20),
                    LabourContractDate = DateTime.UtcNow.AddMonths(-6),
                    BaseSalary = 8000000,
                    StaffPosition = StaffPosition.Cashier
                };

                await context.AppUsers.AddRangeAsync(adminUser, cashierUser);
                await context.Staffs.AddAsync(staffMember);
                await context.SaveChangesAsync();
            }

            // 6. Seed Expense Categories
            if (!await context.ExpenseCategories.AnyAsync())
            {
                var defaultExpenses = new List<ExpenseCategory>
                {
                    new ExpenseCategory { Code = "EXP-RENT", Name = "Tiền thuê mặt bằng", IsDefault = true },
                    new ExpenseCategory { Code = "EXP-ELECTRICITY", Name = "Tiền điện", IsDefault = true },
                    new ExpenseCategory { Code = "EXP-WATER", Name = "Tiền nước", IsDefault = true },
                    new ExpenseCategory { Code = "EXP-INTERNET", Name = "Internet & Viễn thông", IsDefault = true },
                    new ExpenseCategory { Code = "EXP-PAYROLL", Name = "Lương, thưởng & Phụ cấp nhân viên", IsDefault = true },
                    new ExpenseCategory { Code = "EXP-SHIPPING", Name = "Chi phí vận chuyển & Logistics", IsDefault = true },
                    new ExpenseCategory { Code = "EXP-MAINTENANCE", Name = "Bảo trì thiết bị & Cửa hàng", IsDefault = true },
                    new ExpenseCategory { Code = "EXP-MARKETING", Name = "Chi phí Tiếp thị & Quảng cáo", IsDefault = true },
                    new ExpenseCategory { Code = "EXP-STATIONERY", Name = "Văn phòng phẩm phục vụ quầy", IsDefault = true },
                    new ExpenseCategory { Code = "EXP-OTHER", Name = "Chi phí vận hành khác", IsDefault = true }
                };

                await context.ExpenseCategories.AddRangeAsync(defaultExpenses);
                await context.SaveChangesAsync();
            }

            // 7. Seed Sample Products with Inventory
            if (!await context.Products.AnyAsync())
            {
                var catBooks = await context.Categories.FirstAsync(c => c.Code == "CAT-BOOKS");
                var catStationery = await context.Categories.FirstAsync(c => c.Code == "CAT-STATIONERY");
                var whStore1 = await context.Warehouses.FirstAsync(w => w.Code == "WH-STORE01");

                var book1 = new Product
                {
                    Id = Guid.NewGuid(),
                    SKU = "BK-DACNHANTAM",
                    Barcode = "8934974163588",
                    Title = "Đắc Nhân Tâm (Khổ Lớn)",
                    Description = "Cuốn sách kinh điển về nghệ thuật giao tiếp và thu phục lòng người của Dale Carnegie.",
                    CategoryId = catBooks.Id,
                    ProductType = ProductType.Book,
                    IsBook = true,
                    Unit = "Cuốn",
                    CostPrice = 52000,
                    SellingPrice = 86000,
                    VatRate = 5,
                    LowStockThreshold = 10,
                    IsActive = true,
                    BookDetail = new BookDetail
                    {
                        ISBN = "978-604-58-7132-4",
                        Author = "Dale Carnegie",
                        Publisher = "NXB Tổng Hợp TP.HCM",
                        PublicationYear = 2024,
                        PageCount = 320,
                        Language = "Tiếng Việt",
                        CoverType = "Bìa mềm"
                    }
                };

                var book2 = new Product
                {
                    Id = Guid.NewGuid(),
                    SKU = "BK-NHAGIAGIA",
                    Barcode = "8935235226249",
                    Title = "Nhà Giả Kim",
                    Description = "Tác phẩm xuất sắc của Paulo Coelho về hành trình theo đuổi vận mệnh.",
                    CategoryId = catBooks.Id,
                    ProductType = ProductType.Book,
                    IsBook = true,
                    Unit = "Cuốn",
                    CostPrice = 45000,
                    SellingPrice = 79000,
                    VatRate = 5,
                    LowStockThreshold = 10,
                    IsActive = true,
                    BookDetail = new BookDetail
                    {
                        ISBN = "978-604-1-16345-8",
                        Author = "Paulo Coelho",
                        Publisher = "NXB Hội Nhà Văn",
                        PublicationYear = 2023,
                        PageCount = 228,
                        Language = "Tiếng Việt",
                        CoverType = "Bìa mềm"
                    }
                };

                var pen1 = new Product
                {
                    Id = Guid.NewGuid(),
                    SKU = "ST-BUTGEL-DEN",
                    Barcode = "8935001802111",
                    Title = "Bút Gel Thiên Long 0.5mm Mực Đen",
                    Description = "Bút gel viết êm, mực ra đều, khô nhanh.",
                    CategoryId = catStationery.Id,
                    ProductType = ProductType.Stationery,
                    IsBook = false,
                    Unit = "Cây",
                    CostPrice = 6000,
                    SellingPrice = 12000,
                    VatRate = 8,
                    LowStockThreshold = 30,
                    IsActive = true
                };

                await context.Products.AddRangeAsync(book1, book2, pen1);
                await context.SaveChangesAsync();

                // Seed StockBalance & StockTransactions
                var sb1 = new StockBalance { WarehouseId = whStore1.Id, ProductId = book1.Id, Quantity = 50 };
                var sb2 = new StockBalance { WarehouseId = whStore1.Id, ProductId = book2.Id, Quantity = 35 };
                var sb3 = new StockBalance { WarehouseId = whStore1.Id, ProductId = pen1.Id, Quantity = 120 };

                await context.StockBalances.AddRangeAsync(sb1, sb2, sb3);

                var st1 = new StockTransaction
                {
                    WarehouseId = whStore1.Id,
                    ProductId = book1.Id,
                    DocumentNo = "INIT-STOCK-001",
                    TransactionType = StockTransactionType.PurchaseReceipt,
                    Quantity = 50,
                    UnitCost = 52000,
                    BeforeStock = 0,
                    AfterStock = 50,
                    Note = "Tồn kho ban đầu",
                    CreatedBy = "System"
                };

                var st2 = new StockTransaction
                {
                    WarehouseId = whStore1.Id,
                    ProductId = book2.Id,
                    DocumentNo = "INIT-STOCK-001",
                    TransactionType = StockTransactionType.PurchaseReceipt,
                    Quantity = 35,
                    UnitCost = 45000,
                    BeforeStock = 0,
                    AfterStock = 35,
                    Note = "Tồn kho ban đầu",
                    CreatedBy = "System"
                };

                var st3 = new StockTransaction
                {
                    WarehouseId = whStore1.Id,
                    ProductId = pen1.Id,
                    DocumentNo = "INIT-STOCK-001",
                    TransactionType = StockTransactionType.PurchaseReceipt,
                    Quantity = 120,
                    UnitCost = 6000,
                    BeforeStock = 0,
                    AfterStock = 120,
                    Note = "Tồn kho ban đầu",
                    CreatedBy = "System"
                };

                await context.StockTransactions.AddRangeAsync(st1, st2, st3);
                await context.SaveChangesAsync();
            }
        }
    }
}
