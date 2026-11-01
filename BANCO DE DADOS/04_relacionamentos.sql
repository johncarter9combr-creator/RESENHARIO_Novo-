USE resenhario_db;

-- USUARIO possui várias REVIEWS
-- USUARIO possui vários LIVROS em sua ESTANTE

-- LIVRO pode possuir várias ADAPTAÇÕES
-- FILME pode estar relacionado a várias ADAPTAÇÕES

-- LIVRO pode possuir várias REVIEWS
-- ADAPTACAO pode possuir várias REVIEWS

-- Ver usuários e suas reviews
SELECT
    u.Nome AS Usuario,
    r.IdReviews,
    r.NotaLivro,
    r.Comentario
FROM Usuarios u
INNER JOIN Reviews r
    ON u.IdUsuario = r.IdUsuario;

    -- Ver livros presentes nas estantes dos usuários
SELECT
    u.Nome AS Usuario,
    l.Titulo AS Livro,
    e.Status
FROM Estantes e
INNER JOIN Usuarios u
    ON e.IdUsuario = u.IdUsuario
INNER JOIN Livros l
    ON e.IdLivros = l.IdLivros;

    -- Ver livros e suas adaptações para filmes
SELECT
    l.Titulo AS Livro,
    f.Titulo AS Filme
FROM Adaptacoes a
INNER JOIN Livros l
    ON a.IdLivro = l.IdLivros
INNER JOIN Filmes f
    ON a.IdFilme = f.IdFilme;