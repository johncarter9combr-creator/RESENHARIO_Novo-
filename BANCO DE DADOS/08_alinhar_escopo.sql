USE resenhario_db;

-- Migração não destrutiva: renomeia Nota para NotaFidelidade sem apagar avaliações.
-- Pode ser executada mais de uma vez. Faça backup antes de alterar o banco.
SET @tem_nota_antiga = (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Reviews' AND COLUMN_NAME = 'Nota'
);
SET @tem_nota_nova = (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Reviews' AND COLUMN_NAME = 'NotaFidelidade'
);
SET @sql_nota = IF(@tem_nota_antiga = 1 AND @tem_nota_nova = 0,
    'ALTER TABLE Reviews CHANGE COLUMN Nota NotaFidelidade INT NULL',
    'SELECT ''Coluna NotaFidelidade já está alinhada; nenhuma alteração necessária'' AS aviso');
PREPARE stmt_nota FROM @sql_nota;
EXECUTE stmt_nota;
DEALLOCATE PREPARE stmt_nota;

-- Garante a escala de 1 a 5 para dados futuros. Valores antigos não são convertidos
-- automaticamente porque podem ter significado diferente; revise-os antes, se houver.
