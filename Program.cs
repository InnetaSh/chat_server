//Сложный уровень: Создать чат-сервер, где несколько клиентов могут общаться между собой. Сервер должен ретранслировать сообщения всех клиентов.
//Цель: реализация сложного многопользовательского взаимодействия через сеть.
//Результат: полноценный чат, где сообщения одного клиента видны другим клиентам.







//Реализуйте асинхронный TCP-сервер, который позволяет клиентам регистрироваться и входить в систему перед тем, как начать обмен сообщениями.
//Сервер должен хранить имена пользователей и пароли (в локальной базе данных или в файле).
//После успешной аутентификации клиенты могут отправлять сообщения друг другу, как в чате.

//Требования:

//Клиенты должны пройти регистрацию (отправить имя и пароль) или войти, если регистрация уже выполнена.
//Сервер должен хранить данные о пользователях (например, в текстовом файле или базе данных).
//Только после успешной аутентификации клиенту разрешается участвовать в чате.
//Сообщения от одного клиента должны передаваться всем остальным клиентам в чате.
//Реализовать логику выхода клиентов из системы.


using Microsoft.Data.SqlClient;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

string connectionString = @"Data Source = DESKTOP-ITRLGSN; Initial Catalog = master; Trusted_Connection=True; TrustServerCertificate = True";

//using (SqlConnection connection = new SqlConnection(connectionString))
//{

//    await connection.OpenAsync();

//    SqlCommand command = new SqlCommand();

//    command.CommandText = "CREATE DATABASE ClientServer";

//    command.Connection = connection;

//    await command.ExecuteNonQueryAsync();
//    Console.WriteLine("DB created");


//}



bool tableExists = await CheckIfTableExistsAsync("Accounts");
if (!tableExists)
{
    string sqlQuery = "CREATE TABLE Accounts (AccountId  INT PRIMARY KEY IDENTITY, Login NVARCHAR(20) NOT NULL UNIQUE, Password NVARCHAR(20) NOT NULL)";
    await ExecuteCommand(sqlQuery);
}

static async Task<bool> CheckIfTableExistsAsync(string tableName)
{
    string query = @"IF EXISTS( SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @tableName ) SELECT CAST(1 AS BIT) ELSE SELECT CAST(0 AS BIT)";

    return await ExecuteQueryAsync(query, new SqlParameter("@TableName", tableName));
}
static async Task<bool> ExecuteQueryAsync(string query, SqlParameter parameter)
{
    string connectionString = @"Data Source=DESKTOP-ITRLGSN; Initial Catalog = ClientServer; Trusted_Connection=True; TrustServerCertificate=True";

    using (SqlConnection connection = new SqlConnection(connectionString))
    {
        await connection.OpenAsync();

        using (SqlCommand command = new SqlCommand(query, connection))
        {
            if (parameter != null)
                command.Parameters.Add(parameter);

            object result = await command.ExecuteScalarAsync();
            return Convert.ToBoolean(result);
        }
    }
}
static async Task ExecuteCommand(string sqlQuery)
{
    string connectionString = @"Data Source = DESKTOP-ITRLGSN; Initial Catalog = ClientServer; Trusted_Connection=True; TrustServerCertificate = True";
    using (SqlConnection connection = new SqlConnection(connectionString))
    {

        await connection.OpenAsync();

        SqlCommand command = new SqlCommand(sqlQuery, connection);

        await command.ExecuteNonQueryAsync();

    }

}






ServerObject listener = new ServerObject();
await listener.ListenAsync(); 

class ServerObject
{
    TcpListener tcpListener = new TcpListener(IPAddress.Any, 13000); 
    List<ClientObject> clients = new List<ClientObject>(); 
 
  
    protected internal async Task ListenAsync()
    {
        try
        {
            tcpListener.Start();
            Console.WriteLine("Сервер запущен. Ожидание подключений...");

            while (true)
            {
                TcpClient tcpClient = await tcpListener.AcceptTcpClientAsync();

                ClientObject clientObject = new ClientObject(tcpClient, this);
                clients.Add(clientObject);
                Task.Run(clientObject.ProcessAsync);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        finally
        {
            Disconnect();
        }
    }

    
    protected internal async Task MessageAsync(string message, string id)
    {
        foreach (var client in clients)
        {
            if (client.Id != id) 
            {
                await client.Writer.WriteLineAsync(message); 
                await client.Writer.FlushAsync();
            }
        }
    }
 
    protected internal void Disconnect()
    {
        foreach (var client in clients)
        {
            client.Close(); 
        }
        tcpListener.Stop(); 
    }
}




class ClientObject
{
    protected internal string Id { get; } = Guid.NewGuid().ToString();
    protected internal StreamWriter Writer { get; }
    protected internal StreamReader Reader { get; }

    TcpClient client;
    ServerObject server;
    NetworkStream? stream;
    public ClientObject(TcpClient tcpClient, ServerObject serverObject)
    {
        client = tcpClient;
        server = serverObject;
       
         stream = client.GetStream();
       
        Reader = new StreamReader(stream);
        
        Writer = new StreamWriter(stream);
    }

    public async Task ProcessAsync()
    {
        try
        {

            Console.WriteLine($"Клиенту {client.Client.RemoteEndPoint} отправлен запрос имени");
            
            byte[] loginRequest = Encoding.UTF8.GetBytes("Введите логин: ");
            stream.Write(loginRequest, 0, loginRequest.Length);

            
            byte[] loginBuffer = new byte[256];
            int loginBytes = stream.Read(loginBuffer, 0, loginBuffer.Length);
            string login = Encoding.UTF8.GetString(loginBuffer, 0, loginBytes).Trim();
            Console.WriteLine($"Логин: {login}");

            
            
            byte[] passwordRequest = Encoding.UTF8.GetBytes("Введите пароль: ");
            stream.Write(passwordRequest, 0, passwordRequest.Length);

            
            byte[] passwordBuffer = new byte[256];
            int passwordBytes = stream.Read(passwordBuffer, 0, passwordBuffer.Length);
            string password = Encoding.UTF8.GetString(passwordBuffer, 0, passwordBytes).Trim();
            Console.WriteLine($"Пароль: {password}");



            bool ExistsLogin = await loginExists(login,password);


            if (!ExistsLogin)
            {

                await Registration(login, password);
              
                byte[] mesRegistration = Encoding.UTF8.GetBytes($"{login} успешно добавлен в базу.\nДобро пожаловать {login} в чат!\n");
                stream.Write(mesRegistration, 0, mesRegistration.Length);

                
                Console.WriteLine($"{login} зарегестрирован в чате");
            }
            else
            {
               

                byte[] mesRegistration = Encoding.UTF8.GetBytes($"Добро пожаловать {login} в чат!\n");
                stream.Write(mesRegistration, 0, mesRegistration.Length);

                Console.WriteLine($"Добро пожаловать {login}!");
            }



            string? message = $"{login} вошел в чат";

            await server.MessageAsync(message, Id);
            Console.WriteLine(message);

            while (true)
            {
                try
                {
                    message = await Reader.ReadLineAsync();
                    if (message == null) continue;
                    message = $"{login}: {message}";
                    Console.WriteLine(message);
                    await server.MessageAsync(message, Id);
                }
                catch
                {
                    message = $"{login} покинул чат";
                    Console.WriteLine(message);
                    await server.MessageAsync(message, Id);
                    break;
                }
            }

        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }
    protected internal void Close()
    {
        Writer.Close();
        Reader.Close();
        client.Close();
    }



    static async Task Registration(string login,string password)
    {


        string connectionString = @"Data Source = DESKTOP-ITRLGSN; Initial Catalog = ClientServer; Trusted_Connection=True; TrustServerCertificate = True";

        string sqlExpression = @"INSERT INTO Accounts (Login,Password) VALUES (@login,@password)";

        using (SqlConnection connection = new SqlConnection(connectionString))
        {

            await connection.OpenAsync();

            SqlCommand command = new SqlCommand(sqlExpression, connection);

            command.Parameters.AddWithValue("@login", login);
            command.Parameters.AddWithValue("@password", password);

            await command.ExecuteNonQueryAsync();
            Console.WriteLine($"Добавлено в базу данных: {login}");


        }


    }

    private static async Task<bool> loginExists(string login, string password)
    {
        string connectionString = @"Data Source=DESKTOP-ITRLGSN; Initial Catalog = ClientServer; Trusted_Connection=True; TrustServerCertificate=True";
        string sqlQuery = @"SELECT COUNT(1) FROM Accounts WHERE Login = @login  and Password = @password";

        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            await connection.OpenAsync();
            using (SqlCommand command = new SqlCommand(sqlQuery, connection))
            {
                command.Parameters.AddWithValue("@login", login);
                command.Parameters.AddWithValue("@password", password);
                int count = (int)await command.ExecuteScalarAsync();
                return count > 0;
            }
        }
    }
}




