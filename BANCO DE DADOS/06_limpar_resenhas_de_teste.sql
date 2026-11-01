USE resenhario_db;

-- OPCIONAL: execute apenas se quiser remover os três comentários fictícios
-- que foram inseridos para teste. Resenhas de outros textos são preservadas.
DELETE FROM Reviews
WHERE Comentario IN (
    'Uma aventura incrivel.',
    'Uma historia muito marcante.',
    'Livro muito divertido.'
);
