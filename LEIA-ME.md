# Resenhário — guia de execução e testes

O Resenhário é uma plataforma web acadêmica de catálogo literário social. Atenção ao alinhamento documental: o PDF de escopo apresenta o nome “LivreFlix”, enquanto o código e a interface utilizam “Resenhário”; a equipe deve padronizar o nome na documentação ou confirmar formalmente que houve mudança de nome. A versão atual preserva o frontend HTML/CSS/JavaScript, a API ASP.NET Core e o banco MySQL existentes, acrescentando autenticação JWT, hashing BCrypt, integração Google Books, pesquisa de filmes pelo TMDB, vínculo livro/filme, avaliações duplas, feed de resenhas e ranking de fidelidade.

## Funcionalidades

- Cadastro e login com senha protegida por BCrypt e token JWT.
- Compatibilidade de login com hashes da versão anterior: se a senha antiga for validada, o hash é migrado para BCrypt durante o login.
- Catálogo de livros com Google Books API.
- Catálogo de filmes com TMDB API, incluindo pôsteres e tentativa de obter o diretor.
- Catálogo de séries com TVmaze.
- Estante por usuário com status de leitura `Quero ler`, `Lendo`, `Lido` e `Abandonado`; filmes e séries usam status de visualização.
- Vínculo entre livro e adaptação cinematográfica.
- Resenhas públicas com nota literária de 1 a 5 estrelas e avaliação opcional da fidelidade de 1 a 5 estrelas.
- Feed de resenhas recentes e ranking de adaptações calculado a partir de avaliações registradas.
- Validação no backend para impedir que um usuário altere a estante ou exclua resenhas de outra conta apenas trocando um ID enviado pelo navegador.

## 1. Banco de dados

1. Abra o MySQL Workbench e conecte ao servidor onde está o banco `resenhario_db`.
2. Se o banco ainda não existir, execute `BANCO DE DADOS/01_criar_banco.sql`. O script foi ajustado para não apagar tabelas ou registros existentes.
3. Execute `BANCO DE DADOS/05_atualizar_banco.sql` para criar a tabela de séries e ajustar as colunas da estante. O script é destinado a ser executado no banco existente e não recria os dados.
4. Em bancos já existentes, faça backup e execute `BANCO DE DADOS/08_alinhar_escopo.sql` para renomear `Reviews.Nota` para `Reviews.NotaFidelidade`, preservando os valores. Em bancos novos, a coluna já é criada com o nome correto.
5. Se a base tiver resenhas antigas com notas literárias de 0 a 10, faça backup antes de considerar executar `BANCO DE DADOS/07_migrar_notas_para_escala_1_a_5.sql`. Essa migração é opcional e converte notas literárias antigas acima de 5 para a escala de 1 a 5.
6. Não execute `02_dados_teste.sql` repetidamente sem revisar o conteúdo: ele pode inserir registros de demonstração duplicados.
7. `06_limpar_resenhas_de_teste.sql` remove resenhas de teste; execute somente se tiver certeza de que deseja excluí-las.

## 2. Configuração do backend

Abra `BACKEND/Resenhario.Api/appsettings.json` e substitua `COLOQUE_SUA_SENHA` pela senha local do MySQL. Não publique uma senha real no GitHub.

O projeto usa .NET 10 no arquivo atual, em concordância com o PDF do escopo, que também especifica C# com .NET 10. Portanto, não há necessidade de downgrade do framework.

### JWT

O ambiente `Development` possui uma chave exclusivamente local em `appsettings.Development.json`. Ela não deve ser usada em produção. Fora do ambiente de desenvolvimento, configure uma chave forte por variável de ambiente `Jwt__Key` (mínimo de 32 bytes), além de configurar `Jwt__Issuer` e `Jwt__Audience` se necessário.

### TMDB

O TMDB exige uma credencial de API. Para ativar a pesquisa de filmes, configure o token de leitura no terminal do PowerShell antes de iniciar a API:

```powershell
$env:Tmdb__ReadAccessToken = "COLE_SEU_TOKEN_DE_LEITURA_DO_TMDB"
cd BACKEND/Resenhario.Api
dotnet restore
dotnet run --launch-profile http
```

O token fica no backend e não é enviado ao JavaScript. Sem essa configuração, a API retorna uma mensagem explicando que a integração precisa ser configurada. A pesquisa Google Books e TVmaze não exige chave no frontend atual.

A string de conexão permanece local em `appsettings.json` para facilitar a execução acadêmica. Antes de hospedar o sistema publicamente, mova-a para uma variável de ambiente e configure segredos de produção.

## 3. Executar o sistema

No terminal do VS Code:

```powershell
cd BACKEND/Resenhario.Api
dotnet restore
dotnet run --launch-profile http
```

A API deverá ficar disponível em `http://localhost:5131`. Mantenha o terminal aberto enquanto utiliza o frontend.

Abra a pasta `frontend` com o Live Server do VS Code. As origens `localhost` e `127.0.0.1` nas portas 5500, 5501 e 5502 estão liberadas pelo CORS de desenvolvimento. Se o Live Server usar outra porta, ajuste a política CORS em `Program.cs`.

## 4. Roteiro de teste manual

1. Abra `paginas/login.html` e crie uma conta com senha de pelo menos 8 caracteres.
2. Saia da conta e entre novamente para confirmar a autenticação.
3. Pesquise um livro no Google Books e adicione-o ao catálogo.
4. Pesquise um filme no TMDB e adicione-o ao catálogo.
5. Na página Filmes, vincule um livro a um filme cadastrado.
6. Na estante, adicione obras, atualize status e remova um item.
7. Em Resenhas, publique uma avaliação literária de 1 a 5 estrelas.
8. Selecione uma adaptação vinculada e informe também a nota de fidelidade de 1 a 5.
9. Confirme que o feed mostra as notas e que o ranking aparece depois que houver avaliações de fidelidade.
10. Teste com duas contas diferentes para confirmar que cada uma vê apenas a própria estante e não consegue excluir a resenha da outra.

## 5. Limitações e segurança

- O ambiente de edição utilizado para preparar esta versão não possui o SDK .NET instalado. Por isso, a API não pôde ser compilada nem executada aqui; é obrigatório executar `dotnet restore` e `dotnet build` no computador de desenvolvimento e realizar o roteiro de teste acima.
- A autenticação JWT protege as rotas de estante e as operações de resenhas. O token é guardado no `localStorage` no protótipo atual; isso é prático para a apresentação, mas um XSS poderia expô-lo. Para produção, considere autenticação por cookie `HttpOnly`, `Secure` e `SameSite` ou uma arquitetura backend-for-frontend. As rotas de catálogo continuam abertas para facilitar a gestão do catálogo no protótipo acadêmico; antes de uma publicação pública, restrinja as operações de escrita do catálogo a usuários autorizados.
- A política CORS configurada é destinada ao frontend local. Para hospedagem, substitua-a pelas origens reais.
- A integração TMDB não funciona até que o token de leitura seja configurado no backend.
- Os serviços externos podem ficar temporariamente indisponíveis ou limitar requisições.
- A chave JWT presente em `appsettings.Development.json` é apenas para desenvolvimento local. Não reutilize essa chave em produção.


## Configuração das APIs externas (necessária para pesquisa)

As integrações são chamadas pelo backend; nenhuma chave deve ser colocada no JavaScript.

- **Google Books:** normalmente funciona sem chave. Se a cota do projeto Google estiver bloqueada/zerada, configure uma chave válida em `GoogleBooks:ApiKey` (variável de ambiente `GoogleBooks__ApiKey`) e habilite a Google Books API no projeto correspondente. Uma chave não contorna uma cota diária definida como zero: é necessário ajustar a cota no Google Cloud.
- **TMDB:** exige um Read Access Token válido. Configure a variável de ambiente `Tmdb__ReadAccessToken`. Não publique esse token no GitHub nem o coloque no frontend.
- **TVmaze:** não exige chave; agora a pesquisa também passa pelo backend, evitando chamada direta do navegador.

No PowerShell, para a sessão atual:

```powershell
$env:Tmdb__ReadAccessToken = "COLE_SEU_TOKEN_DE_LEITURA_DO_TMDB"
# Opcional, se sua cota do Google Books exigir chave:
$env:GoogleBooks__ApiKey = "SUA_CHAVE_GOOGLE_BOOKS"
dotnet run
```

A API fica em `http://localhost:5131`. Abra o frontend com a extensão Live Server do VS Code (porta 5500, 5501 ou 5502). Não abra as páginas por `file://`.

## Atualização do banco

Antes de alterar um banco que já contém dados, faça um backup. Execute `BANCO DE DADOS/08_alinhar_escopo.sql` uma vez no MySQL Workbench para renomear `Reviews.Nota` para `Reviews.NotaFidelidade`, preservando os valores existentes. O script é não destrutivo e pode ser executado novamente. Não execute os scripts de dados de teste ou limpeza se quiser preservar seus registros.

## Funcionalidades alinhadas ao escopo

- Cadastro e login com BCrypt e token JWT.
- Estante de livros com Quero ler, Lendo, Lido e Abandonado.
- Pesquisa de livros (Google Books), filmes (TMDB) e séries bônus (TVmaze) via backend.
- Vínculo entre livro e filme; resenha com nota literária de 1 a 5 e nota opcional de fidelidade de 1 a 5.
- Feed comunitário paginado em `paginas/feed.html` e ranking em `paginas/ranking.html`.

Rotas de pesquisa: `GET /api/Livro/buscar?q=...`, `GET /api/Filme/buscar?q=...` e `GET /api/Serie/buscar?q=...`. O feed usa `GET /api/Review/feed?pagina=1&tamanho=10`; o ranking usa `GET /api/Adaptacao/ranking`.
