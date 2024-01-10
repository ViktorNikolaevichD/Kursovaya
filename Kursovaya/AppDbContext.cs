using Microsoft.EntityFrameworkCore;
using Kursovaya.Entities;

namespace Kursovaya
{
    internal class AppDbContext : DbContext
    {
        // Таблица врачей
        public DbSet<Doctor> Doctors { get; set; }
        // Таблица пациентов
        public DbSet<Patient> Patients { get; set; }
        // Таблица диагнозов
        public DbSet<Disease> Diseases { get; set; }
        // Таблица медицинских справок
        public DbSet<Сertificate>  Certificates { get; set; }

        public AppDbContext()
        {
            // Проверка базы данных на существование
            Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Строка подключения к базе данных
            optionsBuilder.UseSqlServer("Server=localhost;Database=DbCourseWork;Trusted_Connection=True;Encrypt=False;");
        }
    }
}
