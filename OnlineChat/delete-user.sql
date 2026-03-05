-- Удаление пользователя и всех связанных данных
-- Замените 'novozhilov.vv@dvfu.ru' на нужный email

-- Сначала найдем ID пользователя
SELECT Id, Email, Username FROM Users WHERE Email = 'novozhilov.vv@dvfu.ru';

-- Удаляем сообщения где пользователь отправитель
DELETE FROM Messages WHERE SenderId IN (SELECT Id FROM Users WHERE Email = 'novozhilov.vv@dvfu.ru');

-- Удаляем сообщения где пользователь получатель
DELETE FROM Messages WHERE ReceiverId IN (SELECT Id FROM Users WHERE Email = 'novozhilov.vv@dvfu.ru');

-- Удаляем самого пользователя
DELETE FROM Users WHERE Email = 'novozhilov.vv@dvfu.ru';

-- Проверка что удалено
SELECT Id, Email, Username FROM Users WHERE Email = 'novozhilov.vv@dvfu.ru';
