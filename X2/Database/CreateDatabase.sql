/*
    Скрипт создания базы данных для проекта X2.
    СУБД: Microsoft SQL Server 2019 и выше.

    Перед запуском убедитесь, что приложение в App.config подключается к базе X2:
    Server=.\SQLEXPRESS;Database=X2;Integrated Security=True;TrustServerCertificate=True;
*/

IF DB_ID(N'X2') IS NULL
BEGIN
    CREATE DATABASE X2;
END;
GO

USE X2;
GO

IF OBJECT_ID(N'dbo.Пользователи', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Пользователи
    (
        IDПользователь int IDENTITY(1,1) NOT NULL,
        Логин nvarchar(50) NOT NULL,
        Пароль nvarchar(100) NOT NULL,
        CONSTRAINT PK_Пользователи PRIMARY KEY (IDПользователь),
        CONSTRAINT UQ_Пользователи_Логин UNIQUE (Логин)
    );
END;
GO

IF OBJECT_ID(N'dbo.Группы', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Группы
    (
        НазваниеГруппы nvarchar(50) NOT NULL,
        Курс int NOT NULL,
        Направление nvarchar(120) NOT NULL,
        Поток nvarchar(120) NULL,
        CONSTRAINT PK_Группы PRIMARY KEY (НазваниеГруппы),
        CONSTRAINT CK_Группы_Курс CHECK (Курс BETWEEN 1 AND 5)
    );
END;
GO

IF OBJECT_ID(N'dbo.Группы', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.Группы', N'Поток') IS NULL
BEGIN
    ALTER TABLE dbo.Группы
    ADD Поток nvarchar(120) NULL;
END;
GO

IF OBJECT_ID(N'dbo.Группы', N'U') IS NOT NULL
BEGIN
    UPDATE dbo.Группы
    SET Поток = Направление
    WHERE Поток IS NULL OR LTRIM(RTRIM(Поток)) = N'';
END;
GO

IF OBJECT_ID(N'dbo.Группы', N'U') IS NOT NULL
   AND NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_Группы_Поток'
      AND object_id = OBJECT_ID(N'dbo.Группы')
)
BEGIN
    CREATE INDEX IX_Группы_Поток
    ON dbo.Группы(Поток, Курс, Направление, НазваниеГруппы);
END;
GO

IF OBJECT_ID(N'dbo.Аудитории', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Аудитории
    (
        НомерАудитории nvarchar(20) NULL,
        ТипАудитории nvarchar(100) NOT NULL,
        CONSTRAINT PK_Аудитории PRIMARY KEY (НомерАудитории),
        CONSTRAINT CK_Аудитории_ТипАудитории CHECK (ТипАудитории IN
        (
            N'Лекции',
            N'Упражнения',
            N'Компьютерный класс',
            N'Мультимедийные аудитории',
            N'Спец класс'
        ))
    );
END;
GO

IF OBJECT_ID(N'dbo.Предметы', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Предметы
    (
        IDПредмет int IDENTITY(1,1) NOT NULL,
        Название nvarchar(200) NOT NULL,
        CONSTRAINT PK_Предметы PRIMARY KEY (IDПредмет),
        CONSTRAINT UQ_Предметы_Название UNIQUE (Название)
    );
END;
GO

IF OBJECT_ID(N'dbo.Преподаватели', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Преподаватели
    (
        IDПреподаватель int IDENTITY(1,1) NOT NULL,
        ФИО nvarchar(200) NOT NULL,
        ЗакрепленнаяАудитория nvarchar(20) NULL,
        CONSTRAINT PK_Преподаватели PRIMARY KEY (IDПреподаватель),
        CONSTRAINT FK_Преподаватели_Аудитории_ЗакрепленнаяАудитория
            FOREIGN KEY (ЗакрепленнаяАудитория)
            REFERENCES dbo.Аудитории(НомерАудитории)
            ON DELETE SET NULL
            ON UPDATE CASCADE
    );
END;
GO

IF OBJECT_ID(N'dbo.ПредметыПрофилиСеместры', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ПредметыПрофилиСеместры
    (
        IDПредмет int NOT NULL,
        Профиль nvarchar(120) NOT NULL,
        Семестр int NOT NULL,
        CONSTRAINT PK_ПредметыПрофилиСеместры PRIMARY KEY (IDПредмет, Профиль, Семестр),
        CONSTRAINT FK_ПредметыПрофилиСеместры_Предметы
            FOREIGN KEY (IDПредмет)
            REFERENCES dbo.Предметы(IDПредмет)
            ON DELETE CASCADE,
        CONSTRAINT CK_ПредметыПрофилиСеместры_Семестр CHECK (Семестр BETWEEN 1 AND 10)
    );
END;
GO

IF OBJECT_ID(N'dbo.ПредметыИсключенныеГруппы', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ПредметыИсключенныеГруппы
    (
        IDПредмет int NOT NULL,
        НазваниеГруппы nvarchar(50) NOT NULL,
        CONSTRAINT PK_ПредметыИсключенныеГруппы PRIMARY KEY (IDПредмет, НазваниеГруппы),
        CONSTRAINT FK_ПредметыИсключенныеГруппы_Предметы
            FOREIGN KEY (IDПредмет)
            REFERENCES dbo.Предметы(IDПредмет)
            ON DELETE CASCADE,
        CONSTRAINT FK_ПредметыИсключенныеГруппы_Группы
            FOREIGN KEY (НазваниеГруппы)
            REFERENCES dbo.Группы(НазваниеГруппы)
            ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_ПредметыПрофилиСеместры_ПрофильСеместр'
      AND object_id = OBJECT_ID(N'dbo.ПредметыПрофилиСеместры')
)
BEGIN
    CREATE INDEX IX_ПредметыПрофилиСеместры_ПрофильСеместр
    ON dbo.ПредметыПрофилиСеместры(Профиль, Семестр, IDПредмет);
END;
GO

IF OBJECT_ID(N'dbo.ПреподавателиПредметы', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ПреподавателиПредметы
    (
        IDПреподаватель int NOT NULL,
        IDПредмет int NOT NULL,
        CONSTRAINT PK_ПреподавателиПредметы PRIMARY KEY (IDПреподаватель, IDПредмет),
        CONSTRAINT FK_ПреподавателиПредметы_Преподаватели
            FOREIGN KEY (IDПреподаватель)
            REFERENCES dbo.Преподаватели(IDПреподаватель)
            ON DELETE CASCADE,
        CONSTRAINT FK_ПреподавателиПредметы_Предметы
            FOREIGN KEY (IDПредмет)
            REFERENCES dbo.Предметы(IDПредмет)
            ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_ПреподавателиПредметы_IDПредмет'
      AND object_id = OBJECT_ID(N'dbo.ПреподавателиПредметы')
)
BEGIN
    CREATE INDEX IX_ПреподавателиПредметы_IDПредмет
    ON dbo.ПреподавателиПредметы(IDПредмет, IDПреподаватель);
END;
GO

IF OBJECT_ID(N'dbo.ГруппаСеместр', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ГруппаСеместр
    (
        НазваниеГруппы nvarchar(50) NOT NULL,
        Семестр int NOT NULL,
        КоличествоНедель int NOT NULL CONSTRAINT DF_ГруппаСеместр_КоличествоНедель DEFAULT (16),
        CONSTRAINT PK_ГруппаСеместр PRIMARY KEY (НазваниеГруппы, Семестр),
        CONSTRAINT FK_ГруппаСеместр_Группы
            FOREIGN KEY (НазваниеГруппы)
            REFERENCES dbo.Группы(НазваниеГруппы)
            ON DELETE CASCADE
            ON UPDATE CASCADE,
        CONSTRAINT CK_ГруппаСеместр_Семестр CHECK (Семестр BETWEEN 1 AND 10),
        CONSTRAINT CK_ГруппаСеместр_КоличествоНедель CHECK (КоличествоНедель BETWEEN 1 AND 30)
    );
END;
GO

IF OBJECT_ID(N'dbo.УчебныйПлан', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.УчебныйПлан
    (
        IDУчебныйПлан int IDENTITY(1,1) NOT NULL,
        НазваниеГруппы nvarchar(50) NOT NULL,
        IDПредмет int NOT NULL,
        IDПреподаватель int NULL,
        Семестр int NOT NULL,
        ТипЗанятия nvarchar(20) NOT NULL,
        ЧасыВСеместре int NOT NULL,
        ТребуемыйТипАудитории nvarchar(100) NULL,
        ТребуемаяАудитория nvarchar(20) NULL,
        ПотоковоеЗанятие bit NOT NULL CONSTRAINT DF_УчебныйПлан_ПотоковоеЗанятие DEFAULT (0),
        ШаблонНедель nvarchar(100) NULL,
        CONSTRAINT PK_УчебныйПлан PRIMARY KEY (IDУчебныйПлан),
        CONSTRAINT FK_УчебныйПлан_Группы
            FOREIGN KEY (НазваниеГруппы)
            REFERENCES dbo.Группы(НазваниеГруппы),
        CONSTRAINT FK_УчебныйПлан_Предметы
            FOREIGN KEY (IDПредмет)
            REFERENCES dbo.Предметы(IDПредмет),
        CONSTRAINT FK_УчебныйПлан_Преподаватели
            FOREIGN KEY (IDПреподаватель)
            REFERENCES dbo.Преподаватели(IDПреподаватель),
        CONSTRAINT FK_УчебныйПлан_Аудитории_ТребуемаяАудитория
            FOREIGN KEY (ТребуемаяАудитория)
            REFERENCES dbo.Аудитории(НомерАудитории)
            ON DELETE SET NULL
            ON UPDATE CASCADE,
        CONSTRAINT CK_УчебныйПлан_Семестр CHECK (Семестр BETWEEN 1 AND 10),
        CONSTRAINT CK_УчебныйПлан_ТипЗанятия CHECK (ТипЗанятия IN (N'ЛК', N'У', N'ЛБ', N'КР', N'Физкультура')),
        CONSTRAINT CK_УчебныйПлан_Часы CHECK (ЧасыВСеместре > 0)
    );
END;
GO

IF OBJECT_ID(N'dbo.УчебныйПлан', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.УчебныйПлан', N'ТребуемыйТипАудитории') IS NULL
BEGIN
    ALTER TABLE dbo.УчебныйПлан
    ADD ТребуемыйТипАудитории nvarchar(100) NULL;
END;
GO

IF OBJECT_ID(N'dbo.УчебныйПлан', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.УчебныйПлан', N'ТребуемаяАудитория') IS NULL
BEGIN
    ALTER TABLE dbo.УчебныйПлан
    ADD ТребуемаяАудитория nvarchar(20) NULL;
END;
GO

IF OBJECT_ID(N'dbo.УчебныйПлан', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.Аудитории', N'U') IS NOT NULL
BEGIN
    UPDATE up
    SET ТребуемаяАудитория = NULL
    FROM dbo.УчебныйПлан up
    LEFT JOIN dbo.Аудитории a ON a.НомерАудитории = up.ТребуемаяАудитория
    WHERE up.ТребуемаяАудитория IS NOT NULL
      AND a.НомерАудитории IS NULL;
END;
GO

IF OBJECT_ID(N'dbo.УчебныйПлан', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.Аудитории', N'U') IS NOT NULL
   AND NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_УчебныйПлан_Аудитории_ТребуемаяАудитория'
      AND parent_object_id = OBJECT_ID(N'dbo.УчебныйПлан')
)
BEGIN
    ALTER TABLE dbo.УчебныйПлан WITH CHECK
    ADD CONSTRAINT FK_УчебныйПлан_Аудитории_ТребуемаяАудитория
        FOREIGN KEY (ТребуемаяАудитория)
        REFERENCES dbo.Аудитории(НомерАудитории)
        ON DELETE SET NULL
        ON UPDATE CASCADE;
END;
GO

IF OBJECT_ID(N'dbo.УчебныйПлан', N'U') IS NOT NULL
   AND NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_УчебныйПлан_ТребованияАудитории'
      AND object_id = OBJECT_ID(N'dbo.УчебныйПлан')
)
BEGIN
    CREATE INDEX IX_УчебныйПлан_ТребованияАудитории
    ON dbo.УчебныйПлан(ТребуемыйТипАудитории, ТребуемаяАудитория);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_УчебныйПлан_ГруппаСеместр'
      AND object_id = OBJECT_ID(N'dbo.УчебныйПлан')
)
BEGIN
    CREATE INDEX IX_УчебныйПлан_ГруппаСеместр
    ON dbo.УчебныйПлан(НазваниеГруппы, Семестр);
END;
GO

IF OBJECT_ID(N'dbo.ПланНедели', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ПланНедели
    (
        IDУчебныйПлан int NOT NULL,
        НомерНедели int NOT NULL,
        CONSTRAINT PK_ПланНедели PRIMARY KEY (IDУчебныйПлан, НомерНедели),
        CONSTRAINT FK_ПланНедели_УчебныйПлан
            FOREIGN KEY (IDУчебныйПлан)
            REFERENCES dbo.УчебныйПлан(IDУчебныйПлан)
            ON DELETE CASCADE,
        CONSTRAINT CK_ПланНедели_НомерНедели CHECK (НомерНедели BETWEEN 1 AND 30)
    );
END;
GO

IF OBJECT_ID(N'dbo.Расписания', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Расписания
    (
        IDРасписание int IDENTITY(1,1) NOT NULL,
        Название nvarchar(200) NOT NULL,
        Семестр int NOT NULL,
        ДатаСоздания datetime NOT NULL CONSTRAINT DF_Расписания_ДатаСоздания DEFAULT (GETDATE()),
        CONSTRAINT PK_Расписания PRIMARY KEY (IDРасписание),
        CONSTRAINT CK_Расписания_Семестр CHECK (Семестр BETWEEN 1 AND 10)
    );
END;
GO

IF OBJECT_ID(N'dbo.РасписаниеЗанятий', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.РасписаниеЗанятий
    (
        IDЗапись int IDENTITY(1,1) NOT NULL,
        IDРасписание int NOT NULL,
        НомерНедели int NOT NULL,
        ДеньНедели int NOT NULL,
        НомерПары int NOT NULL,
        НазваниеГруппы nvarchar(50) NOT NULL,
        IDПредмет int NOT NULL,
        ТипЗанятия nvarchar(20) NOT NULL,
        IDПреподаватель int NULL,
        IDПреподаватель2 int NULL,
        НомерАудитории nvarchar(20) NULL,
        СвязкаЗанятия uniqueidentifier NULL,
        IDУчебныйПлан int NULL,
        CONSTRAINT PK_РасписаниеЗанятий PRIMARY KEY (IDЗапись),
        CONSTRAINT FK_РасписаниеЗанятий_Расписания
            FOREIGN KEY (IDРасписание)
            REFERENCES dbo.Расписания(IDРасписание)
            ON DELETE CASCADE,
        CONSTRAINT FK_РасписаниеЗанятий_Группы
            FOREIGN KEY (НазваниеГруппы)
            REFERENCES dbo.Группы(НазваниеГруппы),
        CONSTRAINT FK_РасписаниеЗанятий_Предметы
            FOREIGN KEY (IDПредмет)
            REFERENCES dbo.Предметы(IDПредмет),
        CONSTRAINT FK_РасписаниеЗанятий_Преподаватели
            FOREIGN KEY (IDПреподаватель)
            REFERENCES dbo.Преподаватели(IDПреподаватель),
        CONSTRAINT FK_РасписаниеЗанятий_Преподаватели2
            FOREIGN KEY (IDПреподаватель2)
            REFERENCES dbo.Преподаватели(IDПреподаватель),
        CONSTRAINT FK_РасписаниеЗанятий_Аудитории
            FOREIGN KEY (НомерАудитории)
            REFERENCES dbo.Аудитории(НомерАудитории),
        CONSTRAINT CK_РасписаниеЗанятий_Неделя CHECK (НомерНедели BETWEEN 1 AND 30),
        CONSTRAINT CK_РасписаниеЗанятий_День CHECK (ДеньНедели BETWEEN 1 AND 6),
        CONSTRAINT CK_РасписаниеЗанятий_Пара CHECK (НомерПары BETWEEN 1 AND 10),
        CONSTRAINT CK_РасписаниеЗанятий_ТипЗанятия CHECK (ТипЗанятия IN (N'ЛК', N'У', N'ЛБ', N'КР', N'Физкультура'))
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_РасписаниеЗанятий_Преподаватель2Время'
      AND object_id = OBJECT_ID(N'dbo.РасписаниеЗанятий')
)
BEGIN
    CREATE INDEX IX_РасписаниеЗанятий_Преподаватель2Время
    ON dbo.РасписаниеЗанятий(IDРасписание, НомерНедели, ДеньНедели, НомерПары, IDПреподаватель2)
    WHERE IDПреподаватель2 IS NOT NULL;
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_РасписаниеЗанятий_ГруппаВремя'
      AND object_id = OBJECT_ID(N'dbo.РасписаниеЗанятий')
)
BEGIN
    CREATE INDEX IX_РасписаниеЗанятий_ГруппаВремя
    ON dbo.РасписаниеЗанятий(IDРасписание, НомерНедели, ДеньНедели, НомерПары, НазваниеГруппы);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_РасписаниеЗанятий_ПреподавательВремя'
      AND object_id = OBJECT_ID(N'dbo.РасписаниеЗанятий')
)
BEGIN
    CREATE INDEX IX_РасписаниеЗанятий_ПреподавательВремя
    ON dbo.РасписаниеЗанятий(IDРасписание, НомерНедели, ДеньНедели, НомерПары, IDПреподаватель)
    WHERE IDПреподаватель IS NOT NULL;
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_РасписаниеЗанятий_АудиторияВремя'
      AND object_id = OBJECT_ID(N'dbo.РасписаниеЗанятий')
)
BEGIN
    CREATE INDEX IX_РасписаниеЗанятий_АудиторияВремя
    ON dbo.РасписаниеЗанятий(IDРасписание, НомерНедели, ДеньНедели, НомерПары, НомерАудитории)
    WHERE НомерАудитории IS NOT NULL;
END;
GO

IF OBJECT_ID(N'dbo.Звонки', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Звонки
    (
        НомерПары int NOT NULL,
        ВремяНачала time NOT NULL,
        ВремяОкончания time NOT NULL,
        CONSTRAINT PK_Звонки PRIMARY KEY (НомерПары),
        CONSTRAINT CK_Звонки_НомерПары CHECK (НомерПары BETWEEN 1 AND 10),
        CONSTRAINT CK_Звонки_Время CHECK (ВремяНачала < ВремяОкончания)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Звонки)
BEGIN
    INSERT INTO dbo.Звонки(НомерПары, ВремяНачала, ВремяОкончания)
    VALUES
        (1, '08:30', '10:00'),
        (2, '10:10', '11:40'),
        (3, '12:10', '13:40'),
        (4, '13:50', '15:20'),
        (5, '15:30', '17:00');
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Пользователи WHERE Логин = N'admin')
BEGIN
    INSERT INTO dbo.Пользователи(Логин, Пароль)
    VALUES (N'admin', N'admin');
END;
GO
