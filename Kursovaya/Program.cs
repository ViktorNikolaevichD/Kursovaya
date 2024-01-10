using System.Diagnostics;

namespace Kursovaya
{
    public class Program
    {
        static void Main(string[] args)
        {
            MPI.Environment.Run(ref args, comm =>
            {
                // Создаю объект класса, чтобы он был доступени из всех контекстов
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
                        Console.Write("Введите команду \nsave - сохранить базу данных;" +
                                                      "\nsoff - установить статус offline;" +
                                                      "\nage - увеличить возраст пользователей на 1;" +
                                                      "\nonl - посчитать количество пользователей online;" +
                                                      "\nmax - найти максимальный возраст менеджеров;" +
                                                      "\nmin - найти минимальный возраст менеджеров;" +
                                                      "\nsum - посчитать количество заявок пользователей;" +
                                                      "\ncreate - генерация базы данных: ");
                        command = Console.ReadLine();
                    }

                    // Рассылка команды по всем процессам
                    comm.Broadcast(ref command, 0);

                    switch (command)
                    {
                        default:
                            break;
                    }
                }
            });
        }
    }
}