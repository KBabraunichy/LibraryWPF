using LibraryWPF.Models;
using Microsoft.EntityFrameworkCore;
using System.Configuration;

namespace LibraryWPF.Repositories
{
    public class LibraryContext : DbContext
    {
        internal DbSet<LibraryModel> Library { get; set; }

        public LibraryContext()
        {
            //Database.EnsureDeleted();
            Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string connection = ConfigurationManager.AppSettings["connectionString"];
            optionsBuilder.UseSqlServer(connection);
            //optionsBuilder.LogTo(System.Console.WriteLine);
        }
    }
}
