using Kursovaya.Entities;
using Microsoft.EntityFrameworkCore;
using MPI;

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

                    // Случайный пациент
                    var patient = new Patient
                    {
                        FullName = Faker.Name.FullName(),
                        Age = Faker.RandomNumber.Next(18, 85)
                    };
                    // Добавление пациента в БД
                    db.Patients.Add(patient);
                    // Добавление пациента в локальный список
                    patients.Add(patient);
                }              
                // Сохранить
                db.SaveChanges();

                // Получаем 2 табилцы из БД
                diseases = db.Diseases.ToList();
                doctors = db.Doctors.ToList();

                for (int i = 0; i < count; i++)
                {
                    // Если пациенты закончились
                    if (patients.Count() < 1) break;
                    int patientId = Faker.RandomNumber.Next(0, patients.Count() - 1);
                    db.Certificates.Add(new Сertificate
                    { 
                        DoctorId = doctors[Faker.RandomNumber.Next(0, doctors.Count() - 1)].Id,
                        PatientId = patients[patientId].Id,
                        DiseaseId = diseases[Faker.RandomNumber.Next(0, diseases.Count() - 1)].Id,
                        Condition = "Открыт"
                    });
                    // Удалить пациента, которому выписан больничный
                    patients.Remove(patients[patientId]);
                }
                // Сохранить
                db.SaveChanges();
            }
        }
        // Обновить БД
        public static void UpdateDb(LocalDb localDb, AddedDb addedDb, DeletedDb deletedDb, Intracommunicator comm)
        {
            using (var db = new AppDbContext())
            {
                // Добавить в серверную БД данные из локальной БД
                db.Diseases.AddRange(addedDb.AddedDiseases);
                db.Doctors.AddRange(addedDb.AddedDoctors);
                db.Patients.AddRange(addedDb.AddedPatients);
                // Сохранить
                db.SaveChanges();
                // Подождать
                comm.Barrier();
                db.Certificates.AddRange(addedDb.AddedСertificates);
                // Сохранить
                db.SaveChanges();
                // Подождать
                comm.Barrier();

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
                // Подождать
                comm.Barrier();

                // Обновить измененные значения
                db.Diseases.UpdateRange(localDb.Diseases);
                db.Doctors.UpdateRange(localDb.Doctors);
                db.Patients.UpdateRange(localDb.Patients); 
                db.Certificates.UpdateRange(localDb.Сertificates);
                // Сохранить
                db.SaveChanges();
            }
        }
        // Открыть больничный пациенту (работает 0 процесс)
        public static void OpenCertif(LocalDb localDb, AddedDb addedDb, LocalDb generalDb, int idPatient, int idDisease)
        {
            // Найти больничный пациента по Id больничного c открытым больничным
            Сertificate? certificate = generalDb.Сertificates.Where(p => p.PatientId == idPatient && p.Condition == "Открыт").FirstOrDefault();
            // Если больничный нашелся, то закрыть
            if (certificate != null)
            {
                Console.WriteLine($"У пациента уже открыт больничный под Id {certificate.Id}");
                return;
            }
            // Новый больничный 
            var newCertificate = new Сertificate
            {
                // Случайный врач
                DoctorId = generalDb.Doctors[Faker.RandomNumber.Next(0, generalDb.Doctors.Count() - 1)].Id,
                PatientId = idPatient,
                DiseaseId = idDisease,
                Condition = "Открыт"
            };
            // Добавить в локальную базу 0 процесса новый больничный
            localDb.Сertificates.Add(newCertificate);
            // Добавить в добавленные
            addedDb.AddedСertificates.Add(newCertificate);

            Console.WriteLine($"Новый больничный открыт");
        } 
        // Закрыть больничный пациента
        public static void CloseCertif(LocalDb localDb, int idPatient)
        {
            // Найти больничный пациента по Id больничного
            Сertificate? certificate = localDb.Сertificates.Where(p => p.PatientId == idPatient).FirstOrDefault();
            // Если больничный нашелся, то закрыть
            if (certificate != null) 
            {
                certificate.Condition = "Закрыт";
            }
        }
    }
}
