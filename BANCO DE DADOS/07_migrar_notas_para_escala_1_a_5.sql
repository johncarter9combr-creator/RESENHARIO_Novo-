USE resenhario_db;

-- MIGRAÇÃO OPCIONAL: use apenas se a base já tiver resenhas antigas com notas de 0 a 10.
-- Faça um backup antes. A conversão aproxima a escala antiga à nova escala de 1 a 5.
-- Não execute repetidamente: o script identifica valores acima de 5 para evitar reconversão.
UPDATE Reviews
SET NotaLivro = GREATEST(1, LEAST(5, ROUND(NotaLivro / 2.0)))
WHERE NotaLivro IS NOT NULL AND NotaLivro > 5 AND NotaLivro <= 10;

-- A nota de fidelidade é armazenada na coluna NotaFidelidade após executar 08_alinhar_escopo.sql.
-- Este script não converte notas antigas de fidelidade automaticamente; revise os dados legados.
