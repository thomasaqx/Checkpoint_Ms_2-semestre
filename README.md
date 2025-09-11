Gerenciamento de Sessões com C#, MongoDB e Redis
Este projeto implementa uma API em ASP.NET Core para gerenciar sessões de usuário, 
otimizando o desempenho com uma estratégia de cache. 
Ele utiliza MongoDB para armazenar dados e Redis como camada de cache de alta velocidade.

Como Funciona
Modelo de Dados: A classe User define a estrutura das informações do usuário.

Conexões: A aplicação se conecta a um banco de dados MongoDB e a um servidor Redis.

Lógica de Cache:

Prioridade ao Cache: Ao buscar um usuário, a API primeiro verifica o Redis.

Fallback: Se o usuário não estiver no cache, a API o busca no MongoDB e o salva no Redis com um tempo de expiração (TTL) de 15 minutos.

Invalidação: Em operações de escrita POST, PUT, o cache é invalidado para garantir que a próxima leitura busque os dados atualizados do MongoDB.


Teste de Post e Get

<img width="932" height="571" alt="image" src="https://github.com/user-attachments/assets/6ef69ae9-d387-49bb-b1f5-1e272d6998d3" />

<img width="1051" height="575" alt="image" src="https://github.com/user-attachments/assets/169e0d3f-44b2-44d4-9c25-5fefff6fe867" />
