using Kursovaya.Entities;
namespace Kursovaya
{
    // Класс для хранения добавленных данных
    public class AddedDb
    {
        public List<Disease> AddedDiseases { get; set; } = new List<Disease> { };
        public List<Doctor> AddedDoctors { get; set; } = new List<Doctor> { };
        public List<Patient> AddedPatients { get; set; } = new List<Patient> { };
        public List <Сertificate> AddedСertificates { get; set; } = new List<Сertificate> { };
    }
}
