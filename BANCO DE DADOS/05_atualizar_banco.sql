USE resenhario_db;

-- Execute uma vez para habilitar filmes e séries na estante.
-- Não apaga registros existentes.
CREATE TABLE IF NOT EXISTS Series (
    IdSerie INT AUTO_INCREMENT PRIMARY KEY,
    Titulo VARCHAR(200) NOT NULL,
    Sinopse TEXT NULL,
    ImagemUrl VARCHAR(500) NULL,
    DataEstreia VARCHAR(30) NULL,
    FonteId VARCHAR(100) NULL
);

ALTER TABLE Filmes MODIFY COLUMN TmdbId VARCHAR(100) NULL;
ALTER TABLE Estantes MODIFY COLUMN IdLivros INT NULL;

SET @col_filme = (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Estantes' AND COLUMN_NAME = 'IdFilme'
);
SET @sql_filme = IF(@col_filme = 0,
    'ALTER TABLE Estantes ADD COLUMN IdFilme INT NULL',
    'SELECT ''IdFilme já existe'' AS aviso');
PREPARE stmt_filme FROM @sql_filme;
EXECUTE stmt_filme;
DEALLOCATE PREPARE stmt_filme;

SET @col_serie = (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Estantes' AND COLUMN_NAME = 'IdSerie'
);
SET @sql_serie = IF(@col_serie = 0,
    'ALTER TABLE Estantes ADD COLUMN IdSerie INT NULL',
    'SELECT ''IdSerie já existe'' AS aviso');
PREPARE stmt_serie FROM @sql_serie;
EXECUTE stmt_serie;
DEALLOCATE PREPARE stmt_serie;

-- Chaves estrangeiras opcionais: adicionadas somente se ainda não existem.
SET @fk_filme = (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
    WHERE CONSTRAINT_SCHEMA = DATABASE() AND TABLE_NAME = 'Estantes'
      AND CONSTRAINT_NAME = 'FK_Estantes_Filmes'
);
SET @sql_fk_filme = IF(@fk_filme = 0,
    'ALTER TABLE Estantes ADD CONSTRAINT FK_Estantes_Filmes FOREIGN KEY (IdFilme) REFERENCES Filmes(IdFilme)',
    'SELECT ''FK de filme já existe'' AS aviso');
PREPARE stmt_fk_filme FROM @sql_fk_filme;
EXECUTE stmt_fk_filme;
DEALLOCATE PREPARE stmt_fk_filme;

SET @fk_serie = (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
    WHERE CONSTRAINT_SCHEMA = DATABASE() AND TABLE_NAME = 'Estantes'
      AND CONSTRAINT_NAME = 'FK_Estantes_Series'
);
SET @sql_fk_serie = IF(@fk_serie = 0,
    'ALTER TABLE Estantes ADD CONSTRAINT FK_Estantes_Series FOREIGN KEY (IdSerie) REFERENCES Series(IdSerie)',
    'SELECT ''FK de série já existe'' AS aviso');
PREPARE stmt_fk_serie FROM @sql_fk_serie;
EXECUTE stmt_fk_serie;
DEALLOCATE PREPARE stmt_fk_serie;
