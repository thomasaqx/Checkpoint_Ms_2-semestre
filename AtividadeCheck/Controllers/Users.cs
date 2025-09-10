using MySqlConnector;
using Dapper;
using StackExchange.Redis;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

[Route("api/[controller]")]
[ApiController]
public class UsersController : ControllerBase
{
    private static ConnectionMultiplexer redis;

    public UsersController()
    {
        // Garante que a conexão com o Redis seja feita apenas uma vez
        if (redis == null)
        {
            redis = ConnectionMultiplexer.Connect("localhost:6379");
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        IDatabase db = redis.GetDatabase();
        string key = $"userSession:{id}";

        try
        {
            // Tenta pegar o valor do cache (Redis)
            string userJson = await db.StringGetAsync(key);

            if (!string.IsNullOrEmpty(userJson))
            {
                // Retorna do cache se encontrado
                var user = JsonConvert.DeserializeObject<User>(userJson);
                return Ok(user);
            }

            // Se não houver cache, busca no MySQL
            using var connection = new MySqlConnection("Server=localhost;Database=fiap;User=root;Password=123;");
            await connection.OpenAsync();

            string query = "SELECT Id, Name, Email, LastAccess FROM users WHERE Id = @Id";
            var userFromDb = await connection.QueryFirstOrDefaultAsync<User>(query, new { Id = id });

            if (userFromDb != null)
            {
                // Salva os dados no cache com tempo de expiração de 15 minutos
                userJson = JsonConvert.SerializeObject(userFromDb);
                await db.StringSetAsync(key, userJson, TimeSpan.FromMinutes(15));
                return Ok(userFromDb);
            }

            return NotFound();
        }
        catch (Exception ex)
        {
            // Logar a exceção para fins de depuração
            // Console.WriteLine($"Erro ao processar a requisição GET: {ex.Message}");
            return StatusCode(500, "Erro interno do servidor");
        }
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] User user)
    {
        try
        {
            // Conecta e insere no MySQL
            using var connection = new MySqlConnection("Server=localhost;Database=fiap;User=root;Password=123;");
            await connection.OpenAsync();

            string query = "INSERT INTO users (Id, Name, Email, LastAccess) VALUES (@Id, @Name, @Email, @LastAccess)";
            await connection.ExecuteAsync(query, user);

            // Invalida o cache para que a próxima busca seja atualizada
            IDatabase db = redis.GetDatabase();
            await db.KeyDeleteAsync($"userSession:{user.Id}");

            return Ok();
        }
        catch (Exception ex)
        {
            // Logar a exceção
            // Console.WriteLine($"Erro ao processar a requisição POST: {ex.Message}");
            return StatusCode(500, "Erro interno do servidor");
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Put(int id, [FromBody] User user)
    {
        try
        {
            // Conecta e atualiza o usuário no MySQL
            using var connection = new MySqlConnection("Server=localhost;Database=fiap;User=root;Password=123;");
            await connection.OpenAsync();

            string query = "UPDATE users SET Name = @Name, Email = @Email, LastAccess = @LastAccess WHERE Id = @Id";
            int rowsAffected = await connection.ExecuteAsync(query, user);

            if (rowsAffected > 0)
            {
                // Invalida o cache para garantir que os dados atualizados sejam buscados na próxima requisição
                IDatabase db = redis.GetDatabase();
                await db.KeyDeleteAsync($"userSession:{id}");
                return Ok();
            }

            return NotFound();
        }
        catch (Exception ex)
        {
            // Logar a exceção
            // Console.WriteLine($"Erro ao processar a requisição PUT: {ex.Message}");
            return StatusCode(500, "Erro interno do servidor");
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            // Conecta e deleta o usuário no MySQL
            using var connection = new MySqlConnection("Server=localhost;Database=fiap;User=root;Password=123;");
            await connection.OpenAsync();

            string query = "DELETE FROM users WHERE Id = @Id";
            int rowsAffected = await connection.ExecuteAsync(query, new { Id = id });

            if (rowsAffected > 0)
            {
                // Invalida o cache para que a remoção seja refletida
                IDatabase db = redis.GetDatabase();
                await db.KeyDeleteAsync($"userSession:{id}");
                return Ok();
            }

            return NotFound();
        }
        catch (Exception ex)
        {
            // Logar a exceção
            // Console.WriteLine($"Erro ao processar a requisição DELETE: {ex.Message}");
            return StatusCode(500, "Erro interno do servidor");
        }
    }
}