using Microsoft.EntityFrameworkCore;

namespace ABCBookShop.Models
{
    public class ABCBookShopContext : DbContext
    {
        public string connectionString = "workstation id=ABCbookshop.mssql.somee.com;packet size=4096;user id=giahamvui0001_SQLLogin_1;pwd=s6nvbl5fxa;data source=ABCbookshop.mssql.somee.com;persist security info=False;initial catalog=ABCbookshop;TrustServerCertificate=True";
        public ABCBookShopContext(DbContextOptions<ABCBookShopContext> connectionString) : base(connectionString)
        {

        }


    }
}
