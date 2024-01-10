using Kursovaya.Entities;
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
                // Список для хранения болезней
                List<Disease> diseases = new List<Disease> { };
                // Список для хранения врачей
                List<Doctor> doctors = new List<Doctor> { };
                // Список для хранения пациентов
                List<Patient> patients = new List<Patient> { };

                // Заполнение БД случайными данными
                for (int i = 0; i < count; i++)
                {
                    // Генерация случайных болезней
                    db.Diseases.Add(new Disease
                    {
                        // Случайные данные для расшифки болезни
                        Decoding = Faker.Name.Last()
                    });

                    // Генерация случайных враче
                    db.Doctors.Add(new Doctor 
                    {
                        FullName = Faker.Name.FullName(),
                        Age = Faker.RandomNumber.Next(27, 75)
                    });

                    // Генерация случайных пациентов
                    db.Patients.Add(new Patient 
                    { 
                        FullName = Faker.Name.FullName(), 
                        Age = Faker.RandomNumber.Next(18, 85) 
                    });
                }
                // Сохранить
                db.SaveChanges();

                // Получаем все табилцы из БД
                diseases = db.Diseases.ToList();
                doctors = db.Doctors.ToList();
                patients = db.Patients.ToList();

                for (int i = 0; i < count; i++)
                {
                    db.Certificates.Add(new Сertificate
                    { 
                        DoctorId = doctors[Faker.RandomNumber.Next(0, doctors.Count() - 1)].Id,
                        PatientId = patients[Faker.RandomNumber.Next(0, patients.Count() - 1)].Id,
                        DiseaseId = diseases[Faker.RandomNumber.Next(0, diseases.Count() - 1)].Id,
                        Condition = "Открыт"
                    });
                }
                // Сохранить
                db.SaveChanges();
            }
        }
        // Обновить БД
        public static void UpdateDb(LocalDb localDb, AddedDb addedDb, DeletedDb deletedDb)
        {
            using (var db = new AppDbContext())
            {
                // Добавить в серверную БД данные из локальной БД
                db.Diseases.AddRange(addedDb.AddedDiseases);
                db.Doctors.AddRange(addedDb.AddedDoctors);
                db.Patients.AddRange(addedDb.AddedPatients);
                db.Certificates.AddRange(addedDb.AddedСertificates);
                // Сохранить
                db.SaveChanges();

                // Удалить из серверной БД данные, которые были удалены в локальной БД
                foreach (var obj in deletedDb.DeletedDiseases)
                    db.Diseases.Remove(obj);
                foreach (var obj in deletedDb.DeletedDoctors)
                    db.Doctors.Remove(obj);
                foreach (var obj in deletedDb.DeletedPatients)
                    db.Patients.Remove(obj);
                foreach (var obj in deletedDb.DeletedСertificates)
                    db.Certificates.Remove(obj);
                // Сохранить
                db.SaveChanges();

                // Обновить измененные значения
                db.Diseases.UpdateRange(localDb.Diseases);
                db.Doctors.UpdateRange(localDb.Doctors);
                db.Patients.UpdateRange(localDb.Patients); 
                db.Certificates.UpdateRange(localDb.Сertificates);
            }
        }

    }
}
