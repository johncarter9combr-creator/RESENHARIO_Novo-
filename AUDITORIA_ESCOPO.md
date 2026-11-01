# Auditoria de alinhamento — Resenhário

## Requisitos implementados nesta revisão

- BCrypt.Net para novas senhas; login mantém compatibilidade e migra hashes legados do ASP.NET Identity após autenticação bem-sucedida.
- Status de leitura: Quero ler, Lendo, Lido e Abandonado.
- Pesquisa de livros pelo Google Books, com proxy no backend.
- Pesquisa de filmes pelo TMDB, com pôster e diretor quando a API de créditos responde.
- Pesquisa bônus de séries pelo TVmaze, também via proxy no backend.
- Notas literárias e de fidelidade validadas entre 1 e 5. A coluna `Nota` foi padronizada para `NotaFidelidade` por migração não destrutiva.
- Feed de resenhas paginado e página própria.
- Ranking de adaptações por média de fidelidade e página própria.
- Vínculo livro-filme mantido.

## Configuração externa obrigatória

- TMDB: definir `Tmdb__ReadAccessToken` com um Read Access Token válido. Sem isso, a rota de filmes responde HTTP 503 com mensagem de configuração.
- Google Books: normalmente não exige chave; se o projeto tiver cota bloqueada, ajustar a cota no Google Cloud e, se necessário, definir `GoogleBooks__ApiKey`. Código não consegue corrigir cota zerada.
- TVmaze: sem chave; chamada agora é feita pelo backend.

## Verificações realizadas no pacote

- Conferência estática dos nomes das propriedades, rotas e chamadas frontend/backend.
- Validação sintática do JavaScript e dos arquivos JSON.
- Conferência de que nenhum segredo real foi incluído no pacote.

## Verificações pendentes no ambiente do usuário

Este ambiente de edição não possui o .NET SDK nem acesso ao MySQL do usuário. Portanto, a compilação .NET, a migração SQL e os testes reais de integração precisam ser executados no computador do usuário após configurar a senha do MySQL e o token do TMDB. Não afirmar que esses testes foram aprovados antes dessa execução.


## Divergências e pontos de atenção identificados na conferência

- **Nome do produto:** o PDF de escopo chama a plataforma de “LivreFlix”, mas o código, as páginas e o README usam “Resenhário”. É necessário padronizar o nome antes da entrega.
- **Framework:** o PDF especifica .NET 10, e o projeto também está configurado para `net10.0`. A afirmação anterior do README de que o PDF citava .NET 8 estava incorreta e foi corrigida.
- **Banco de dados:** o projeto depende de MySQL e da execução dos scripts `01_criar_banco.sql` e `05_atualizar_banco.sql`; em bases antigas, também é necessária a migração `08_alinhar_escopo.sql`. A aplicação não vai funcionar com a senha placeholder `COLOQUE_SUA_SENHA`.
- **TMDB:** pesquisa de filmes depende de `Tmdb__ReadAccessToken`; sem token, a rota retorna HTTP 503 por configuração ausente.
- **Séries:** TVmaze e a estante de séries são funcionalidades extras, não exigências explícitas do escopo principal.
- **Segurança:** as rotas de escrita do catálogo e de vínculo de adaptações não exigem autenticação/autorização; para uma publicação pública, devem ser protegidas. As rotas da estante e as operações de criação/exclusão de resenhas já verificam o usuário autenticado.
- **Teste de execução:** o JavaScript passou na verificação sintática (`node --check`) e os arquivos JSON foram inspecionados. Não foi possível compilar/executar a API porque o ambiente de conferência não possui o .NET SDK nem acesso ao MySQL. Portanto, a compilação, conexão real, migrações e integrações externas continuam pendentes no computador de desenvolvimento.
