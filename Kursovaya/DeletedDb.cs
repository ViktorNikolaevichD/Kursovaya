using Kursovaya.Entities;

namespace Kursovaya
{
    // Класс для хранения удаленных таблиц
    public class DeletedDb
    {
        public List<Disease> DeletedDiseases { get; set; } = new List<Disease> { };
        public List<Doctor> DeletedDoctors { get; set; } = new List<Doctor> { };
        public List<Patient> DeletedPatients { get; set; } = new List<Patient> { };
        public List<Сertificate> DeletedСertificates { get; set; } = new List<Сertificate> { };
    }
}
