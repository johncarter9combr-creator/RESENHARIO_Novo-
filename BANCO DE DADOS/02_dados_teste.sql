USE resenhario_db;

-- =========================================
-- 1. USUÁRIOS
-- =========================================

INSERT INTO Usuarios
    (Nome, Email, SenhaHash, DataCadastro)
VALUES
    ('John Silva', 'john@email.com', 'hash_teste_123', NOW()),
    ('Lucas Santos', 'lucas@email.com', 'hash_teste_456', NOW()),
    ('Zoe Oliveira', 'zoe@email.com', 'hash_teste_789', NOW());


-- =========================================
-- 2. LIVROS
-- =========================================

INSERT INTO Livros
    (GoogleBId, Titulo, Autor, Sinopse, Isbn, CapaUrl)
VALUES
    (
        'google_001',
        'O Hobbit',
        'J.R.R. Tolkien',
        'A aventura de Bilbo Bolseiro pela Terra-média.',
        '9780007525515',
        'https://exemplo.com/hobbit.jpg'
    ),
    (
        'google_002',
        'Harry Potter e a Pedra Filosofal',
        'J.K. Rowling',
        'Um jovem descobre que é um bruxo e começa seus estudos em Hogwarts.',
        '9788532530783',
        'https://exemplo.com/harrypotter.jpg'
    ),
    (
        'google_003',
        'Jogos Vorazes',
        'Suzanne Collins',
        'Katniss Everdeen é escolhida para participar dos Jogos Vorazes.',
        '9788579800245',
        'https://exemplo.com/jogosvorazes.jpg'
    );


-- =========================================
-- 3. FILMES
-- =========================================

INSERT INTO Filmes
    (TmdbId, Titulo, Diretor, AnoLancamento, PosterUrl)
VALUES
    (
        001,
        'O Hobbit: Uma Jornada Inesperada',
        'Peter Jackson',
        2012,
        'https://exemplo.com/hobbit-filme.jpg'
    ),
    (
        002,
        'Harry Potter e a Pedra Filosofal',
        'Chris Columbus',
        2001,
        'https://exemplo.com/harry-filme.jpg'
    ),
    (
        003,
        'Jogos Vorazes',
        'Gary Ross',
        2012,
        'https://exemplo.com/jogos-filme.jpg'
    );


-- =========================================
-- 4. ADAPTAÇÕES
-- =========================================

INSERT INTO Adaptacoes
    (IdLivro, IdFilme, DataVinculo)
VALUES
    (1, 1, NOW()),
    (2, 2, NOW()),
    (3, 3, NOW());


-- =========================================
-- 5. ESTANTES
-- =========================================

INSERT INTO Estantes
    (IdUsuario, IdLivros, Status, DataAtualizacao)
VALUES
    (1, 1, 'Lido', NOW()),
    (1, 2, 'Lendo', NOW()),
    (2, 1, 'Quero ler', NOW()),
    (2, 3, 'Lido', NOW()),
    (3, 2, 'Quero ler', NOW());


-- =========================================
-- 6. REVIEWS
-- =========================================

INSERT INTO Reviews
    (
        IdUsuario,
        IdLivro,
        IdAdaptacao,
        NotaLivro,
        Comentario,
        AssistiuFilme,
        NotaFidelidade,
        Voto,
        DataCriacao
    )
VALUES
    (
        1,
        1,
        1,
        5,
        'Uma aventura incrível.',
        TRUE,
        4,
        'Gostei',
        NOW()
    ),
    (
        1,
        2,
        2,
        5,
        'Uma história muito marcante.',
        TRUE,
        5,
        'Gostei muito',
        NOW()
    ),
    (
        2,
        3,
        3,
        4,
        'Gostei bastante da história.',
        TRUE,
        4,
        'Gostei',
        NOW()
    ),
    (
        3,
        2,
        NULL,
        5,
        'Livro muito divertido.',
        FALSE,
        NULL,
        NULL,
        NOW()
    );