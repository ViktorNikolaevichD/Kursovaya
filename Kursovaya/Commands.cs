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
                            .Skip(offsetPatient)
                            .Take(partPatient)
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
                    db.Certificates.Add(new Certificate
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
        public static void UpdateDb(LocalDb localDb, AddedDb addedDb, Intracommunicator comm)
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


                // Обновить измененные значения
                /*db.Diseases.UpdateRange(localDb.Diseases);
                db.Doctors.UpdateRange(localDb.Doctors);
                db.Patients.UpdateRange(localDb.Patients);
                db.SaveChanges();*/
                db.Certificates.UpdateRange(localDb.Сertificates);
                // Сохранить
                db.SaveChanges();
            }
        }

        // Открыть больничный пациенту (работает 0 процесс)
        public static void OpenCertif(LocalDb localDb, LocalDb generalDb, int idPatient, int idDisease)
        {
            // Найти больничный пациента по Id больничного c открытым больничным
            Certificate? certificate = generalDb.Сertificates.Where(p => p.PatientId == idPatient && p.Condition == "Открыт").FirstOrDefault();
            // Если больничный не нашелся, то выйти
            if (certificate != null)
            {
                Console.WriteLine($"У пациента уже открыт больничный под Id {certificate.Id}");
                return;
            }

            // Вначале ищется пациент в локальной БД, чтобы правильно отслеживать уже существующий объект
            Patient? patient = localDb.Patients.Where(p => p.Id == idPatient).FirstOrDefault(); 
            if (patient == null)
            {
                patient = generalDb.Patients.Where(p => p.Id == idPatient).FirstOrDefault();
                // Если пациент не нашелся, то выйти
                if (patient == null)
                {
                    Console.WriteLine("Такого пациента нет в базе");
                    return;
                }
            }

            // Вначале ищется болезнь в локальной БД, чтобы правильно отслеживать уже существующий объект
            Disease? disease = localDb.Diseases.Where(p => p.Id == idDisease).FirstOrDefault();
            if (disease == null)
            {
                // Если болезнь не нашлась, то выйти
                disease = generalDb.Diseases.Where(p => p.Id == idDisease).FirstOrDefault();
                if (disease == null)
                {
                    Console.WriteLine("Такой болезни нет в базе");
                    return;
                }
            }

            // Случайный врач. Выбирается случайный доктор из общей базы, потом проверяется локальной базе 0 процесса, чтобы взять его оттуда
            Doctor doctor = generalDb.Doctors[Faker.RandomNumber.Next(0, generalDb.Doctors.Count() - 1)];
            Doctor? localDoctor = localDb.Doctors.Where(p => p.Id == doctor.Id).FirstOrDefault();
            // Если нашелся в локальной базе, то нужно взять его
            if (localDoctor != null)
                doctor = localDoctor;

            // Новый больничный 
            Certificate newCertificate = new Certificate
            {
                // Случайный врач
                DoctorId = doctor.Id,
                PatientId = patient.Id,
                DiseaseId = disease.Id,
                Condition = "Открыт"
            };

            using (var db = new AppDbContext())
            {
                db.Certificates.Add(newCertificate);
                db.SaveChanges();
            }
            
            // Добавить в локальную базу 0 процесса новый больничный
            localDb.Сertificates.Add(newCertificate);

            Console.WriteLine($"Новый больничный открыт");
        } 

        // Закрыть больничный пациента
        public static void CloseCertif(LocalDb localDb, int idPatient)
        {
            // Найти больничный пациента по Id больничного
            Certificate? certificate = localDb.Сertificates.Where(p => p.PatientId == idPatient && p.Condition == "Открыт").FirstOrDefault();
            // Если больничный нашелся, то закрыть
            if (certificate != null) 
            {
                certificate.Condition = "Закрыт";
            }
        }

        // Зарегистрировать пациента
        public static void RegPatient(LocalDb localDb, string fullName, int Age)
        {
            // Создать объект пациента
            Patient patient = new Patient 
            {
                FullName = fullName,
                Age = Age
            };
            // Добавить пациента в серверную БД, чтобы получить его Id
            using (var db = new AppDbContext())
            {
                db.Patients.Add(patient);
                db.SaveChanges();
            }
            // Добавить пациента в локальную БД
            localDb.Patients.Add(patient);
        }

        // Список справок пациента
        public static List<Certificate> GetCertificates(LocalDb localDb, int idPatient)
        {
            // Вернуть справки по id пациента
            return localDb.Сertificates.Where(p => p.PatientId == idPatient).OrderBy(p => p.Id).ToList();
        }

        // Список болеющих определенной болезнью
        public static List<Patient> GetDisease(LocalDb localDb, int idDisease)
        {
            // Выбрать список пациентов, которые болеют определенной болезнью
            return localDb.Сertificates.Where(p => p.DiseaseId == idDisease && p.Condition == "Открыт").Select(p => p.Patient).ToList();
        }
    }
}
