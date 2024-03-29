﻿using Kursovaya.Entities;
using System.Diagnostics;
using System.Text.Json;

namespace Kursovaya
{
    public class Program
    {
        static void Main(string[] args)
        {
            MPI.Environment.Run(ref args, comm =>
            {
                // Объект для хранения локальной БД
                LocalDb localDb = new LocalDb();
                // Предзагрузка базы данных
                // Первым загружает базу 0 процесс, чтобы не было проблемы с одновременным созданием базы данных
                if (comm.Rank == 0)
                    // База данных для 0 процесса
                    localDb = Commands.LoadingDb(comm.Rank, comm.Size);
                // После того как 0 процесс загрузит базу, то все процессы выйдут из барьера
                comm.Barrier();
                // Все !0 процессы загрузят базу
                if (comm.Rank != 0)
                {
                    // База данных для остальных(не 0) процессов
                    localDb = Commands.LoadingDb(comm.Rank, comm.Size);
                }

                // Замер времени работы
                Stopwatch stopWatch = new Stopwatch();

                // Команда от пользователя
                string? command = null;
                while (command != "quit")
                {
                    // Получение команды от пользователя
                    if (comm.Rank == 0)
                    {
                        Console.Write("Введите команду \ngen - генерация данных;" +
                                                      "\nupdate - обновить БД на сервере;" +
                                                      "\nopen - открыть пациенту больничный;" +
                                                      "\nclose - закрыть пациенту больничный;" +
                                                      "\ncertif - посмотреть справки пациента;" +
                                                      "\nreg - зарегистрировать пациента;" +
                                                      "\npatients - посмотреть список пациентов;" +
                                                      "\ndoctors - посмотреть список докторов;" +
                                                      "\nds- посмотреть пациентов с определенной болезнью;" +
                                                      "\ndses - посмотреть список расшифровок болезней;" +
                                                      "\nquit - выйти: ");
                        command = Console.ReadLine();
                    }

                    // Рассылка команды по всем процессам
                    comm.Broadcast(ref command, 0);

                    switch (command)
                    {
                        // Генерация данных
                        case "gen":
                            int count = 0;
                            // Обновление базы перед началом генерации
                            Commands.UpdateDb(localDb);
                            comm.Barrier();
                            if (comm.Rank == 0)
                            {
                                Console.Write("Введите количество строк для генерации: ");
                                count = Convert.ToInt32(Console.ReadLine());
                                stopWatch.Restart();
                                // Генерация данных
                                Commands.GenerateData(count);
                            }
                            // Все процессы ожидают окончания генерации
                            comm.Barrier();
                            // Загрузка данных из БД
                            localDb = Commands.LoadingDb(comm.Rank, comm.Size);
                            comm.Barrier();
                            if (comm.Rank == 0)
                            {
                                stopWatch.Stop();
                                Console.WriteLine("Данные сгенерированы");
                            }
                            break;
                        // Обновление БД
                        case "update":
                            if (comm.Rank == 0)
                            {
                                Console.WriteLine("Обновляем БД");
                                stopWatch.Restart();
                            }
                            // Обновление БД
                            Commands.UpdateDb(localDb);
                            comm.Barrier();
                            if (comm.Rank == 0)
                            {
                                stopWatch.Stop();
                                Console.WriteLine("БД успешно обновлена");
                            }
                            break;
                        // Закрыть больничный
                        case "close":
                            // Айди пациента для закрытия больничного
                            int idPatient = 0;
                            if (comm.Rank == 0)
                            {
                                Console.Write("Введите айди пациента, которому хотите закрыть больничный: ");
                                idPatient = Convert.ToInt32(Console.ReadLine());
                                Console.WriteLine("Закрываем больничный");
                                stopWatch.Restart();
                            }
                            // Разослать всем процессам id пациента
                            comm.Broadcast(ref idPatient, 0);
                            // Закрытие больничного
                            Commands.CloseCertif(localDb, idPatient);
                            if (comm.Rank == 0)
                            {
                                stopWatch.Stop();
                                Console.WriteLine("Больничный успешно закрыт");
                            }
                            break;
                        // Открыть больничный
                        case "open":
                            // Айди пациента для открытия больничного
                            idPatient = 0;
                            // Айди болезни для закрытия больничного
                            int idDisease = 0;
                            // Синхронизация БД перед работой
                            Commands.UpdateDb(localDb);
                            if (comm.Rank == 0)
                            {
                                Console.Write("Введите айди пациента, которому хотите открыть больничный: ");
                                idPatient = Convert.ToInt32(Console.ReadLine());
                                Console.Write("Введите айди болезни: ");
                                idDisease = Convert.ToInt32(Console.ReadLine());
                                Console.WriteLine("Открываем больничный");
                                stopWatch.Restart();

                                // Открытие больничного
                                Commands.OpenCertif(idPatient, idDisease);
                            }

                            // Ожидание добавления больничного
                            comm.Barrier();
                            // Обновление локальной БД
                            localDb = Commands.LoadingDb(comm.Rank, comm.Size);
                            comm.Barrier();
                            if (comm.Rank == 0)
                            {
                                stopWatch.Stop();
                                Console.WriteLine($"Новый больничный открыт");
                            }
                            break;
                        // Зарегистрировать пациента
                        case "reg":
                            // ФИО пациента
                            string fullName = "";
                            // Возраст
                            int age = 0;
                            if (comm.Rank == 0)
                            {
                                Console.Write("Введите ФИО пациента: ");
                                fullName = Console.ReadLine();
                                Console.Write("Введите возраст пациента: ");
                                age = Convert.ToInt32(Console.ReadLine());
                                Console.WriteLine("Регестрируем пациента");
                                stopWatch.Restart();

                                // Регистрация пациента
                                Commands.RegPatient(localDb, fullName, age);

                                stopWatch.Stop();
                                Console.WriteLine("Пациент зарегестрирован");
                            }
                            break;
                        // Посмотреть список пациентов
                        case "patients":
                            if (comm.Rank == 0)
                            {
                                Console.WriteLine("Выводим список пациентов");
                                stopWatch.Restart();

                                // Собрать все списки в 0 процессе
                                string[] patientList = comm.Gather(JsonSerializer.Serialize(localDb.Patients.ToList()), 0);

                                // Превратить данные в единый список
                                List<Patient> patients = patientList
                                                        .Select(x => JsonSerializer.Deserialize<List<Patient>>(x)!)
                                                        .Where(p => p != null)
                                                        .Aggregate((a, b) => a.Concat(b).ToList());


                                if (patients.Count() < 1)
                                {
                                    stopWatch.Stop();
                                    Console.WriteLine("Список пуст");
                                    break;
                                }
                                // Вывести список пациентов
                                foreach (var patient in patients.OrderBy(p => p.Id))
                                {
                                    Console.WriteLine($"Id: {patient.Id}, Age: {patient.Age}, FullName: {patient.FullName}");
                                }
                                stopWatch.Stop();
                            }
                            else
                            {
                                // Переслать локальные списки БД 0 процессу
                                comm.Gather(JsonSerializer.Serialize(localDb.Patients.ToList()), 0);
                            }
                            break;
                        // Посмотреть список врачей
                        case "doctors":
                            if (comm.Rank == 0)
                            {
                                Console.WriteLine("Выводим список врачей");
                                stopWatch.Restart();

                                // Собрать все списки в 0 процессе
                                string[] doctorList = comm.Gather(JsonSerializer.Serialize(localDb.Doctors.ToList()), 0);

                                // Превратить данные в единый список
                                List<Doctor> doctors = doctorList
                                                        .Select(x => JsonSerializer.Deserialize<List<Doctor>>(x)!)
                                                        .Where(p => p != null)
                                                        .Aggregate((a, b) => a.Concat(b).ToList());


                                if (doctors.Count() < 1)
                                {
                                    stopWatch.Stop();
                                    Console.WriteLine("Список пуст");
                                    break;
                                }
                                // Вывести список врачей
                                foreach (var doctor in doctors)
                                {
                                    Console.WriteLine($"Id: {doctor.Id}, Age: {doctor.Age}, FullName: {doctor.FullName}");
                                }
                                stopWatch.Stop();
                            }
                            else
                            {
                                // Переслать локальные списки БД 0 процессу
                                comm.Gather(JsonSerializer.Serialize(localDb.Doctors.ToList()), 0);
                            }
                            break;
                        // Посмотреть список расшифровок болезней
                        case "dses":
                            if (comm.Rank == 0)
                            {
                                Console.WriteLine("Выводим список расшифровок болезней");
                                stopWatch.Restart();

                                // Собрать все списки в 0 процессе
                                string[] diseasesList = comm.Gather(JsonSerializer.Serialize(localDb.Diseases.ToList()), 0);

                                // Превратить данные в единый список
                                List<Disease> diseases = diseasesList
                                                        .Select(x => JsonSerializer.Deserialize<List<Disease>>(x)!)
                                                        .Where(p => p != null)
                                                        .Aggregate((a, b) => a.Concat(b).ToList());

                                if (diseases.Count() < 1)
                                {
                                    stopWatch.Stop();
                                    Console.WriteLine("Список пуст");
                                    break;
                                }
                                // Вывести список болезней
                                foreach (var disease in diseases)
                                {
                                    Console.WriteLine($"Id: {disease.Id}, Decoding: {disease.Decoding}");
                                }
                                stopWatch.Stop();
                            }
                            else
                            {
                                // Переслать локальные списки БД 0 процессу
                                comm.Gather(JsonSerializer.Serialize(localDb.Diseases.ToList()), 0);
                            }
                            break;
                        // Посмотреть справки пациента
                        case "certif":
                            // Айди пациента
                            idPatient = 0;
                            if (comm.Rank == 0)
                            {
                                Console.Write("Введите Id пациента: ");
                                idPatient = Convert.ToInt32(Console.ReadLine());
                            }
                            // Отправить всем процессам Id пациента
                            comm.Broadcast(ref idPatient, 0);

                            if (comm.Rank == 0)
                            {
                                Console.WriteLine("Выводим список справок");
                                stopWatch.Restart();

                                // Собрать все списки в 0 процессе
                                string[] certifList = comm.Gather(JsonSerializer.Serialize(Commands.GetCertificates(localDb, idPatient)), 0);

                                // Превратить данные в единый список
                                List<Certificate> certifs = certifList
                                                        .Select(x => JsonSerializer.Deserialize<List<Certificate>>(x)!)
                                                        .Where(p => p != null)
                                                        .Aggregate((a, b) => a.Concat(b).ToList());

                                if (certifs.Count() < 1)
                                {
                                    stopWatch.Stop();
                                    Console.WriteLine("Список пуст");
                                    break;
                                }
                                // Вывести список болезней
                                foreach (var certif in certifs)
                                {
                                    // Иногда возникает ошибка отслеживания сущности (при открывании нового больничного и последующем сохранении)
                                    // и некоторые поля пустые
                                    string doctor = certif.Doctor is null ? "NoName" : certif.Doctor.FullName;
                                    string patient = certif.Patient is null ? "NoName" : certif.Patient.FullName;

                                    Console.WriteLine(
                                        $"Id: {certif.Id}, " +
                                        $"Doctor: {doctor}, " +
                                        $"Patient: {patient}, " +
                                        $"Disease: {certif.DiseaseId}, " +
                                        $"Condition: {certif.Condition}");
                                }
                                stopWatch.Stop();
                            }
                            else
                            {
                                // Переслать локальные списки БД 0 процессу
                                comm.Gather(JsonSerializer.Serialize(Commands.GetCertificates(localDb, idPatient)), 0);
                            }
                            break;
                        // Посмотреть пациентов, болеющих определенной болезнью
                        case "ds":
                            // Айди болезни
                            idDisease = 0;
                            if (comm.Rank == 0)
                            {
                                Console.Write("Введите Id болезни: ");
                                idDisease = Convert.ToInt32(Console.ReadLine());
                            }
                            // Отправить всем процессам Id пациента
                            comm.Broadcast(ref idDisease, 0);

                            if (comm.Rank == 0)
                            {
                                Console.WriteLine("Выводим список пациентов");
                                stopWatch.Restart();

                                // Собрать все списки в 0 процессе
                                string[] patientList = comm.Gather(JsonSerializer.Serialize(Commands.GetDisease(localDb, idDisease)), 0);

                                // Превратить данные в единый список
                                List<Patient> patients = patientList
                                                        .Select(x => JsonSerializer.Deserialize<List<Patient>>(x)!)
                                                        .Where(p => p != null)
                                                        .Aggregate((a, b) => a.Concat(b).ToList());


                                if (patients.Count() < 1)
                                {
                                    stopWatch.Stop();
                                    Console.WriteLine("Список пуст");
                                    break;
                                }
                                // Вывести список болеющих пациентов
                                foreach (var patient in patients)
                                {
                                    Console.WriteLine($"Id: {patient.Id}, Age: {patient.Age}, FullName: {patient.FullName}");
                                }
                                stopWatch.Stop();
                            }
                            else
                            {
                                // Переслать локальные списки БД 0 процессу
                                comm.Gather(JsonSerializer.Serialize(Commands.GetDisease(localDb, idDisease)), 0);
                            }
                            break;
                        default:
                            if (comm.Rank == 0 && command != "quit")
                                Console.WriteLine("Неизвестная команда");
                            break;
                    }
                    if (comm.Rank == 0)
                    {
                        // Вывод времени выполнения
                        TimeSpan ts = stopWatch.Elapsed;
                        string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                        ts.Hours, ts.Minutes, ts.Seconds,
                        ts.Milliseconds / 10);
                        Console.WriteLine($"RunTime {comm.Rank} " + elapsedTime);
                    }
                    // Барьер, чтобы все процессы подождали, пока 0 процесс выведет время работы
                    comm.Barrier();
                }
            });
        }
    }
}