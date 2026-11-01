USE resenhario_db;

-- =========================================
-- 1. LISTAR TODOS OS USUÁRIOS
-- =========================================

SELECT *
FROM Usuarios;


-- =========================================
-- 2. LISTAR TODOS OS LIVROS
-- =========================================

SELECT *
FROM Livros;


-- =========================================
-- 3. LISTAR TODOS OS FILMES
-- =========================================

SELECT *
FROM Filmes;


-- =========================================
-- 4. VER A ESTANTE DOS USUÁRIOS
-- =========================================

SELECT
    u.Nome AS Usuario,
    l.Titulo AS Livro,
    e.Status
FROM Estantes e
INNER JOIN Usuarios u
    ON e.IdUsuario = u.IdUsuario
INNER JOIN Livros l
    ON e.IdLivros = l.IdLivros;


-- =========================================
-- 5. VER LIVROS E SUAS ADAPTAÇÕES
-- =========================================

SELECT
    l.Titulo AS Livro,
    f.Titulo AS Filme,
    f.Diretor,
    f.AnoLancamento
FROM Adaptacoes a
INNER JOIN Livros l
    ON a.IdLivro = l.IdLivros
INNER JOIN Filmes f
    ON a.IdFilme = f.IdFilme;


-- =========================================
-- 6. VER REVIEWS COM USUÁRIO E LIVRO
-- =========================================

SELECT
    u.Nome AS Usuario,
    l.Titulo AS Livro,
    r.NotaLivro,
    r.Comentario
FROM Reviews r
INNER JOIN Usuarios u
    ON r.IdUsuario = u.IdUsuario
LEFT JOIN Livros l
    ON r.IdLivro = l.IdLivros;


-- =========================================
-- 7. VER REVIEWS COMPLETAS
-- =========================================

SELECT
    u.Nome AS Usuario,
    l.Titulo AS Livro,
    f.Titulo AS Filme,
    r.NotaLivro,
    r.Comentario,
    r.AssistiuFilme,
    r.NotaFidelidade AS NotaFidelidade,
    r.Voto
FROM Reviews r
INNER JOIN Usuarios u
    ON r.IdUsuario = u.IdUsuario
LEFT JOIN Livros l
    ON r.IdLivro = l.IdLivros
LEFT JOIN Adaptacoes a
    ON r.IdAdaptacao = a.IdAdaptacoes
LEFT JOIN Filmes f
    ON a.IdFilme = f.IdFilme;