using Microsoft.EntityFrameworkCore;

namespace Kursovaya
{
    public class Commands
    {
        // Загрузка БД с сервера
        public static LocalDb LoadingDb(int rank, int size)
        {
            using (var db = new AppDbContext())
            {
                // Количество строк в каждой таблице
                int countDisease = db.Diseases.Count();
                int countDoctor = db.Doctors.Count();
                int countPatient = db.Patients.Count();
                int countCertificate = db.Certificates.Count();
                // Размер части для каждой таблицы
                int partDisease = (countDisease / size + 1);
                int partDoctor = (countDoctor / size + 1);
                int partPatient = (countPatient / size + 1);
                int partCertificate = (countCertificate / size + 1);
                // Смещение по каждой таблице
                int offsetDisease = rank * partDisease;
                int offsetDoctor = rank * partDoctor;
                int offsetPatient = rank * partPatient;
                int offsetCertificate = rank * partCertificate;

                // Вернуть локальную базу данных
                return new LocalDb
                {
                    // Список болезней
                    Diseases = db.Diseases
                            .Skip(offsetDisease)
                            .Take(partDisease)
                            .ToList(),
                    // Список врачей
                    Doctors = db.Doctors
                            .Skip(offsetDoctor)
                            .Take(partDoctor)
                            .ToList(),
                    // Список пациентов
                    Patients = db.Patients
                            .Skip(offsetCertificate)
                            .Take(partCertificate)
                            .ToList(),
                    // Список медицинских справок
                    Сertificates = db.Certificates
                                .Include(p => p.Doctor)
                                .Include(p => p.Patient)
                                .Include(p => p.Disease)
                                .Skip(offsetCertificate)
                                .Take(partCertificate)
                                .ToList()
                };
            }
        }

        // Генерация данных
        public static void GenerateData(int count)
        {
            using (var db = new AppDbContext())
            {

            }
        }
    }
}
