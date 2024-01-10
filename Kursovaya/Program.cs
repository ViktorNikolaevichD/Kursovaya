using System.Diagnostics;
using System.Linq;
using System.Text.Json;

namespace Kursovaya
{
    public class Program
    {
        static void Main(string[] args)
        {
            MPI.Environment.Run(ref args, comm =>
            {
                // Объект для хранения добавленных данных в локальную БД
                AddedDb addedDb = new AddedDb();
                // Объект для хранения удаленных данных из локальной БД
                DeletedDb deletedDb = new DeletedDb();
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
                                                      "\nclose - закрыть пациенту больничный;" +
                                                      "\nopen - открыть пациенту больничный;" +
                                                      "\nreg - зарегистрировать пациента;" +

                                                      "\nmin - найти минимальный возраст менеджеров;" +
                                                      "\nsum - посчитать количество заявок пользователей;" +
                                                      "\ncreate - генерация базы данных: ");
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
                            Commands.UpdateDb(localDb, addedDb, deletedDb, comm);
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
                            Commands.UpdateDb(localDb, addedDb, deletedDb, comm);
                            comm.Barrier();
                            if (comm.Rank == 0)
                            {
                                stopWatch.Stop();
                                Console.WriteLine("БД успешно обновлена");
                            }
                            break;
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
                        case "open":
                            // Айди пациента для открытия больничного
                            idPatient = 0;
                            // Айди болезни для закрытия больничного
                            int idDesease = 0;
                            if (comm.Rank == 0)
                            {
                                Console.Write("Введите айди пациента, которому хотите открыть больничный: ");
                                idPatient = Convert.ToInt32(Console.ReadLine());
                                Console.Write("Введите айди болезни: ");
                                idDesease = Convert.ToInt32(Console.ReadLine());
                                Console.WriteLine("Открываем больничный");
                                stopWatch.Restart();

                                // Собрать все БД в 0 процессе
                                string[] dbases = comm.Gather(JsonSerializer.Serialize(localDb), 0);
                                // Переменная для хранения общей БД
                                LocalDb generalDb = new LocalDb();
                                // Добавить в список в классе врачей
                                generalDb.Doctors = dbases
                                            .Select(x => JsonSerializer.Deserialize<LocalDb>(x)!.Doctors)
                                            .Where(p => p != null)
                                            .Aggregate((a, b) => a.Concat(b).ToList());
                                // Добавить в список в классе болезни
                                generalDb.Diseases = dbases
                                            .Select(x => JsonSerializer.Deserialize<LocalDb>(x)!.Diseases)
                                            .Where(p => p != null)
                                            .Aggregate((a, b) => a.Concat(b).ToList());
                                // Добавить в список в классе пациентов
                                generalDb.Patients = dbases
                                            .Select(x => JsonSerializer.Deserialize<LocalDb>(x)!.Patients)
                                            .Where(p => p != null)
                                            .Aggregate((a, b) => a.Concat(b).ToList());
                                // Добавить в список в классе больничные
                                generalDb.Сertificates = dbases
                                            .Select(x => JsonSerializer.Deserialize<LocalDb>(x)!.Сertificates)
                                            .Where(p => p != null)
                                            .Aggregate((a, b) => a.Concat(b).ToList());
                                // Открытие больничного
                                Commands.OpenCertif(localDb, addedDb, generalDb, idPatient, idDesease);
                                stopWatch.Stop();
                            }
                            else
                            {
                                // Переслать локальные списки БД 0 процессу
                                comm.Gather(JsonSerializer.Serialize(localDb), 0);
                            }
                            break;
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
                                Commands.RegPatient(localDb, addedDb, fullName, age);

                                stopWatch.Stop();
                                Console.WriteLine("Пациент зарегестрирован");
                            }
                            break;
                        default:
                            break;
                    }
                }
            });
        }
    }
}