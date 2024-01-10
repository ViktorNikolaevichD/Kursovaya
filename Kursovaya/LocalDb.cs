using Kursovaya.Entities;

namespace Kursovaya
{
    // Класс для хранение списков таблиц
    public class LocalDb
    {
        public List<Doctor> Doctors { get; set; } = new List<Doctor> { };
        public List<Disease> Diseases { get; set; } = new List<Disease> { };
        public List<Patient> Patients { get; set; } = new List<Patient> { };
        public List<Сertificate> Сertificates { get; set; } = new List<Сertificate> { };

    }
}
