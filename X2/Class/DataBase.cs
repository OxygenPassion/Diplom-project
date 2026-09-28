using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace X2.Class
{
    internal static class DataBase
    {
        private static readonly string _cs =
            ConfigurationManager.ConnectionStrings["Db"]?.ConnectionString
            ?? throw new InvalidOperationException("Не найдена строка подключения 'Db' в App.config");
        private const string AllProfilesMarker = "*";
        private static readonly object _teacherSubjectSchemaSync = new object();
        private static bool _teacherSubjectSchemaEnsured;
        private static readonly object _subjectProfileSemesterSchemaSync = new object();
        private static bool _subjectProfileSemesterSchemaEnsured;
        private static readonly object _groupFlowSchemaSync = new object();
        private static bool _groupFlowSchemaEnsured;
        private static readonly object _curriculumRoomRequirementSchemaSync = new object();
        private static bool _curriculumRoomRequirementSchemaEnsured;
        private static readonly object _scheduleRoomNullableSchemaSync = new object();
        private static bool _scheduleRoomNullableSchemaEnsured;
        private static readonly object _excelLikeScheduleSchemaSync = new object();
        private static bool _excelLikeScheduleSchemaEnsured;

        public static object ExecuteScalar(string sql, params SqlParameter[] parameters)
        {
            using (var con = new SqlConnection(_cs))
            using (var cmd = new SqlCommand(sql, con))
            {
                if (parameters != null && parameters.Length > 0)
                    cmd.Parameters.AddRange(parameters);

                con.Open();
                return cmd.ExecuteScalar();
            }
        }

        public static int ExecuteNonQuery(string sql, params SqlParameter[] parameters)
        {
            int affected = 0;
            ExecuteInTransaction((con, tx) =>
            {
                affected = ExecuteNonQuery(con, tx, sql, parameters);
            });

            return affected;
        }

        public static object ExecuteScalarInTransaction(string sql, params SqlParameter[] parameters)
        {
            object result = null;
            ExecuteInTransaction((con, tx) =>
            {
                result = ExecuteScalar(con, tx, sql, parameters);
            });

            return result;
        }

        public static DataTable Query(string sql, params SqlParameter[] parameters)
        {
            using (var con = new SqlConnection(_cs))
            using (var cmd = new SqlCommand(sql, con))
            {
                if (parameters != null && parameters.Length > 0)
                    cmd.Parameters.AddRange(parameters);

                using (var da = new SqlDataAdapter(cmd))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    return dt;
                }
            }
        }

        public static void ExecuteInTransaction(Action<SqlConnection, SqlTransaction> action)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            using (var con = new SqlConnection(_cs))
            {
                con.Open();

                using (var tx = con.BeginTransaction())
                {
                    try
                    {
                        action(con, tx);
                        tx.Commit();
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }
        }

        public static object ExecuteScalar(SqlConnection con, SqlTransaction tx, string sql, params SqlParameter[] parameters)
        {
            using (var cmd = new SqlCommand(sql, con, tx))
            {
                if (parameters != null && parameters.Length > 0)
                    cmd.Parameters.AddRange(parameters);

                return cmd.ExecuteScalar();
            }
        }

        public static int ExecuteNonQuery(SqlConnection con, SqlTransaction tx, string sql, params SqlParameter[] parameters)
        {
            using (var cmd = new SqlCommand(sql, con, tx))
            {
                if (parameters != null && parameters.Length > 0)
                    cmd.Parameters.AddRange(parameters);

                return cmd.ExecuteNonQuery();
            }
        }

        private static void ExecuteMigrationCommand(SqlConnection con, string sql)
        {
            using (var cmd = new SqlCommand(sql, con))
            {
                cmd.ExecuteNonQuery();
            }
        }

        public static void EnsureTeacherSubjectSchema()
        {
            if (_teacherSubjectSchemaEnsured)
                return;

            lock (_teacherSubjectSchemaSync)
            {
                if (_teacherSubjectSchemaEnsured)
                    return;

                using (var con = new SqlConnection(_cs))
                {
                    con.Open();
                    EnsureTeacherSubjectSchemaCore(con, null);
                }

                _teacherSubjectSchemaEnsured = true;
            }
        }

        public static List<int> GetSubjectIdsForTeacher(int teacherId)
        {
            EnsureTeacherSubjectSchema();

            DataTable dt = Query(@"
SELECT IDПредмет
FROM dbo.ПреподавателиПредметы
WHERE IDПреподаватель = @teacherId;",
                new SqlParameter("@teacherId", teacherId));

            var result = new List<int>();
            foreach (DataRow row in dt.Rows)
                result.Add(Convert.ToInt32(row["IDПредмет"]));

            return result;
        }

        public static DataTable GetTeachersForSubject(int subjectId, int? includeTeacherId = null)
        {
            EnsureTeacherSubjectSchema();

            return Query(@"
SELECT IDПреподаватель, ФИО
FROM
(
    SELECT
        t.IDПреподаватель,
        t.ФИО,
        0 AS SortOrder
    FROM dbo.Преподаватели t
    INNER JOIN dbo.ПреподавателиПредметы tp
        ON tp.IDПреподаватель = t.IDПреподаватель
    WHERE tp.IDПредмет = @subjectId

    UNION ALL

    SELECT
        t.IDПреподаватель,
        t.ФИО + N' (не назначен для предмета)' AS ФИО,
        1 AS SortOrder
    FROM dbo.Преподаватели t
    WHERE @includeTeacherId IS NOT NULL
      AND t.IDПреподаватель = @includeTeacherId
      AND NOT EXISTS
      (
          SELECT 1
          FROM dbo.ПреподавателиПредметы tp
          WHERE tp.IDПредмет = @subjectId
            AND tp.IDПреподаватель = t.IDПреподаватель
      )
) src
ORDER BY SortOrder, ФИО;",
                new SqlParameter("@subjectId", subjectId),
                new SqlParameter("@includeTeacherId", (object)includeTeacherId ?? DBNull.Value));
        }

        public static bool TeacherCanTeachSubject(int teacherId, int subjectId)
        {
            EnsureTeacherSubjectSchema();

            object value = ExecuteScalar(@"
SELECT COUNT(*)
FROM dbo.ПреподавателиПредметы
WHERE IDПреподаватель = @teacherId
  AND IDПредмет = @subjectId;",
                new SqlParameter("@teacherId", teacherId),
                new SqlParameter("@subjectId", subjectId));

            return Convert.ToInt32(value) > 0;
        }

        public static int? FindTeacherIdByFio(string fio, int? excludeTeacherId = null)
        {
            object value = ExecuteScalar(@"
SELECT TOP (1) IDПреподаватель
FROM dbo.Преподаватели
WHERE UPPER(LTRIM(RTRIM(ФИО))) = UPPER(LTRIM(RTRIM(@fio)))
  AND (@excludeTeacherId IS NULL OR IDПреподаватель <> @excludeTeacherId)
ORDER BY IDПреподаватель;",
                new SqlParameter("@fio", fio ?? string.Empty),
                new SqlParameter("@excludeTeacherId", SqlDbType.Int) { Value = (object)excludeTeacherId ?? DBNull.Value });

            if (value == null || value == DBNull.Value)
                return null;

            return Convert.ToInt32(value);
        }

        public static void AddSubjectsToTeacher(int teacherId, IEnumerable<int> subjectIds)
        {
            using (var con = new SqlConnection(_cs))
            {
                con.Open();

                using (var tx = con.BeginTransaction())
                {
                    try
                    {
                        EnsureTeacherSubjectSchemaCore(con, tx);
                        AddTeacherSubjects(con, tx, teacherId, subjectIds);
                        tx.Commit();
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }
        }

        public static int InsertTeacherWithSubjects(string fio, IEnumerable<int> subjectIds)
        {
            using (var con = new SqlConnection(_cs))
            {
                con.Open();

                using (var tx = con.BeginTransaction())
                {
                    try
                    {
                        EnsureTeacherSubjectSchemaCore(con, tx);

                        int? existingTeacherId = FindTeacherIdByFio(con, tx, fio, null);
                        if (existingTeacherId != null)
                        {
                            AddTeacherSubjects(con, tx, existingTeacherId.Value, subjectIds);
                            tx.Commit();
                            return existingTeacherId.Value;
                        }

                        int teacherId;
                        using (var cmd = new SqlCommand(
                            "INSERT INTO dbo.Преподаватели(ФИО) VALUES (@fio); SELECT CAST(SCOPE_IDENTITY() AS int);",
                            con, tx))
                        {
                            cmd.Parameters.AddWithValue("@fio", fio);
                            teacherId = Convert.ToInt32(cmd.ExecuteScalar());
                        }

                        ReplaceTeacherSubjects(con, tx, teacherId, subjectIds);
                        tx.Commit();

                        return teacherId;
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }
        }

        public static void UpdateTeacherWithSubjects(int teacherId, string fio, IEnumerable<int> subjectIds)
        {
            using (var con = new SqlConnection(_cs))
            {
                con.Open();

                using (var tx = con.BeginTransaction())
                {
                    try
                    {
                        EnsureTeacherSubjectSchemaCore(con, tx);

                        int? existingTeacherId = FindTeacherIdByFio(con, tx, fio, teacherId);
                        if (existingTeacherId != null)
                            throw new InvalidOperationException("Преподаватель с таким ФИО уже существует.");

                        using (var cmd = new SqlCommand(@"
UPDATE dbo.Преподаватели
SET ФИО = @fio
WHERE IDПреподаватель = @teacherId;", con, tx))
                        {
                            cmd.Parameters.AddWithValue("@fio", fio);
                            cmd.Parameters.AddWithValue("@teacherId", teacherId);
                            cmd.ExecuteNonQuery();
                        }

                        ReplaceTeacherSubjects(con, tx, teacherId, subjectIds);
                        tx.Commit();
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }
        }

        public static void EnsureSubjectProfileSemesterSchema()
        {
            if (_subjectProfileSemesterSchemaEnsured)
                return;

            lock (_subjectProfileSemesterSchemaSync)
            {
                if (_subjectProfileSemesterSchemaEnsured)
                    return;

                using (var con = new SqlConnection(_cs))
                {
                    con.Open();
                    EnsureSubjectProfileSemesterSchemaCore(con, null);
                }

                _subjectProfileSemesterSchemaEnsured = true;
            }
        }

        public static void EnsureGroupFlowSchema()
        {
            if (_groupFlowSchemaEnsured)
                return;

            lock (_groupFlowSchemaSync)
            {
                if (_groupFlowSchemaEnsured)
                    return;

                using (var con = new SqlConnection(_cs))
                {
                    con.Open();
                    EnsureGroupFlowSchemaCore(con, null);
                }

                _groupFlowSchemaEnsured = true;
            }
        }

        public static void EnsureCurriculumRoomRequirementSchema()
        {
            if (_curriculumRoomRequirementSchemaEnsured)
                return;

            lock (_curriculumRoomRequirementSchemaSync)
            {
                if (_curriculumRoomRequirementSchemaEnsured)
                    return;

                using (var con = new SqlConnection(_cs))
                {
                    con.Open();
                    EnsureCurriculumRoomRequirementSchemaCore(con, null);
                }

                _curriculumRoomRequirementSchemaEnsured = true;
            }
        }

        public static void EnsureExcelLikeScheduleSchema()
        {
            if (_excelLikeScheduleSchemaEnsured)
                return;

            lock (_excelLikeScheduleSchemaSync)
            {
                if (_excelLikeScheduleSchemaEnsured)
                    return;

                EnsureGroupFlowSchema();
                EnsureLessonTypeSchema();
                EnsureScheduleRoomNullableSchema();

                using (var con = new SqlConnection(_cs))
                {
                    con.Open();

                    ExecuteMigrationCommand(con, @"
IF OBJECT_ID(N'dbo.Группы', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.Группы', N'КоличествоПодгрупп') IS NOT NULL
BEGIN
    DECLARE @dropGroupsSubgroupsSql nvarchar(max) = N'';

    SELECT @dropGroupsSubgroupsSql = @dropGroupsSubgroupsSql + N'ALTER TABLE dbo.Группы DROP CONSTRAINT ' + QUOTENAME(dc.name) + N';'
    FROM sys.default_constraints dc
    JOIN sys.columns c
        ON c.object_id = dc.parent_object_id
       AND c.column_id = dc.parent_column_id
    WHERE dc.parent_object_id = OBJECT_ID(N'dbo.Группы')
      AND c.name = N'КоличествоПодгрупп';

    SELECT @dropGroupsSubgroupsSql = @dropGroupsSubgroupsSql + N'ALTER TABLE dbo.Группы DROP CONSTRAINT ' + QUOTENAME(cc.name) + N';'
    FROM sys.check_constraints cc
    WHERE cc.parent_object_id = OBJECT_ID(N'dbo.Группы')
      AND cc.definition LIKE N'%КоличествоПодгрупп%';

    IF @dropGroupsSubgroupsSql <> N''
        EXEC sp_executesql @dropGroupsSubgroupsSql;

    ALTER TABLE dbo.Группы DROP COLUMN КоличествоПодгрупп;
END;");

                    ExecuteMigrationCommand(con, @"
IF OBJECT_ID(N'dbo.УчебныйПлан', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.УчебныйПлан', N'НомерПодгруппы') IS NOT NULL
BEGIN
    DECLARE @dropPlanSubgroupsSql nvarchar(max) = N'';

    SELECT @dropPlanSubgroupsSql = @dropPlanSubgroupsSql + N'ALTER TABLE dbo.УчебныйПлан DROP CONSTRAINT ' + QUOTENAME(dc.name) + N';'
    FROM sys.default_constraints dc
    JOIN sys.columns c
        ON c.object_id = dc.parent_object_id
       AND c.column_id = dc.parent_column_id
    WHERE dc.parent_object_id = OBJECT_ID(N'dbo.УчебныйПлан')
      AND c.name = N'НомерПодгруппы';

    SELECT @dropPlanSubgroupsSql = @dropPlanSubgroupsSql + N'ALTER TABLE dbo.УчебныйПлан DROP CONSTRAINT ' + QUOTENAME(cc.name) + N';'
    FROM sys.check_constraints cc
    WHERE cc.parent_object_id = OBJECT_ID(N'dbo.УчебныйПлан')
      AND cc.definition LIKE N'%НомерПодгруппы%';

    IF @dropPlanSubgroupsSql <> N''
        EXEC sp_executesql @dropPlanSubgroupsSql;

    ALTER TABLE dbo.УчебныйПлан DROP COLUMN НомерПодгруппы;
END;");

                    ExecuteMigrationCommand(con, @"
IF OBJECT_ID(N'dbo.РасписаниеЗанятий', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.РасписаниеЗанятий', N'НомерПодгруппы') IS NOT NULL
BEGIN
    DECLARE @dropScheduleSubgroupsSql nvarchar(max) = N'';

    SELECT @dropScheduleSubgroupsSql = @dropScheduleSubgroupsSql + N'ALTER TABLE dbo.РасписаниеЗанятий DROP CONSTRAINT ' + QUOTENAME(dc.name) + N';'
    FROM sys.default_constraints dc
    JOIN sys.columns c
        ON c.object_id = dc.parent_object_id
       AND c.column_id = dc.parent_column_id
    WHERE dc.parent_object_id = OBJECT_ID(N'dbo.РасписаниеЗанятий')
      AND c.name = N'НомерПодгруппы';

    SELECT @dropScheduleSubgroupsSql = @dropScheduleSubgroupsSql + N'ALTER TABLE dbo.РасписаниеЗанятий DROP CONSTRAINT ' + QUOTENAME(cc.name) + N';'
    FROM sys.check_constraints cc
    WHERE cc.parent_object_id = OBJECT_ID(N'dbo.РасписаниеЗанятий')
      AND cc.definition LIKE N'%НомерПодгруппы%';

    IF @dropScheduleSubgroupsSql <> N''
        EXEC sp_executesql @dropScheduleSubgroupsSql;

    ALTER TABLE dbo.РасписаниеЗанятий DROP COLUMN НомерПодгруппы;
END;");

                    ExecuteMigrationCommand(con, @"
IF OBJECT_ID(N'dbo.УчебныйПлан', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.УчебныйПлан', N'ПотоковоеЗанятие') IS NULL
    BEGIN
        ALTER TABLE dbo.УчебныйПлан
        ADD ПотоковоеЗанятие bit NOT NULL
            CONSTRAINT DF_УчебныйПлан_ПотоковоеЗанятие DEFAULT (0);
    END;

    IF COL_LENGTH(N'dbo.УчебныйПлан', N'ШаблонНедель') IS NULL
    BEGIN
        ALTER TABLE dbo.УчебныйПлан
        ADD ШаблонНедель nvarchar(100) NULL;
    END;
END;");

                    ExecuteMigrationCommand(con, @"
IF OBJECT_ID(N'dbo.РасписаниеЗанятий', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.РасписаниеЗанятий', N'СвязкаЗанятия') IS NULL
    BEGIN
        ALTER TABLE dbo.РасписаниеЗанятий
        ADD СвязкаЗанятия uniqueidentifier NULL;
    END;

    IF COL_LENGTH(N'dbo.РасписаниеЗанятий', N'IDУчебныйПлан') IS NULL
    BEGIN
        ALTER TABLE dbo.РасписаниеЗанятий
        ADD IDУчебныйПлан int NULL;
    END;

    IF COL_LENGTH(N'dbo.РасписаниеЗанятий', N'IDПреподаватель2') IS NULL
    BEGIN
        ALTER TABLE dbo.РасписаниеЗанятий
        ADD IDПреподаватель2 int NULL;
    END;
END;");

                    ExecuteMigrationCommand(con, @"
SET QUOTED_IDENTIFIER ON;

IF OBJECT_ID(N'dbo.РасписаниеЗанятий', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.РасписаниеЗанятий', N'IDПреподаватель2') IS NOT NULL
       AND NOT EXISTS
       (
           SELECT 1
           FROM sys.foreign_keys
           WHERE parent_object_id = OBJECT_ID(N'dbo.РасписаниеЗанятий')
             AND name = N'FK_РасписаниеЗанятий_Преподаватели2'
       )
    BEGIN
        ALTER TABLE dbo.РасписаниеЗанятий WITH CHECK
        ADD CONSTRAINT FK_РасписаниеЗанятий_Преподаватели2
            FOREIGN KEY (IDПреподаватель2)
            REFERENCES dbo.Преподаватели(IDПреподаватель);
    END;
END;");

                    ExecuteMigrationCommand(con, @"
IF OBJECT_ID(N'dbo.РасписаниеЗанятий', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.РасписаниеЗанятий', N'IDПреподаватель2') IS NOT NULL
       AND NOT EXISTS
       (
           SELECT 1
           FROM sys.indexes
           WHERE object_id = OBJECT_ID(N'dbo.РасписаниеЗанятий')
             AND name = N'IX_РасписаниеЗанятий_Преподаватель2Время'
       )
    BEGIN
        CREATE INDEX IX_РасписаниеЗанятий_Преподаватель2Время
        ON dbo.РасписаниеЗанятий(IDРасписание, НомерНедели, ДеньНедели, НомерПары, IDПреподаватель2)
        WHERE IDПреподаватель2 IS NOT NULL;
    END;
END;");

                    ExecuteMigrationCommand(con, @"
IF OBJECT_ID(N'dbo.РасписаниеЗанятий', N'U') IS NOT NULL
BEGIN
    IF EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.РасписаниеЗанятий')
          AND name = N'UQ_РасписаниеЗанятий_ГруппаВремя'
    )
    BEGIN
        DROP INDEX UQ_РасписаниеЗанятий_ГруппаВремя ON dbo.РасписаниеЗанятий;
    END;

    IF EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.РасписаниеЗанятий')
          AND name = N'UX_Schedule_GroupTime'
    )
    BEGIN
        DROP INDEX UX_Schedule_GroupTime ON dbo.РасписаниеЗанятий;
    END;

    IF EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.РасписаниеЗанятий')
          AND name = N'UX_Группа_Время'
    )
    BEGIN
        DROP INDEX UX_Группа_Время ON dbo.РасписаниеЗанятий;
    END;

    IF EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.РасписаниеЗанятий')
          AND name = N'UX_Schedule_RoomTime'
    )
    BEGIN
        DROP INDEX UX_Schedule_RoomTime ON dbo.РасписаниеЗанятий;
    END;

    IF EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.РасписаниеЗанятий')
          AND name = N'UX_Аудитория_Время'
    )
    BEGIN
        DROP INDEX UX_Аудитория_Время ON dbo.РасписаниеЗанятий;
    END;

    IF EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.РасписаниеЗанятий')
          AND name = N'UX_Schedule_TeacherTime'
    )
    BEGIN
        DROP INDEX UX_Schedule_TeacherTime ON dbo.РасписаниеЗанятий;
    END;

    IF EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.РасписаниеЗанятий')
          AND name = N'UX_Преподаватель_Время'
    )
    BEGIN
        DROP INDEX UX_Преподаватель_Время ON dbo.РасписаниеЗанятий;
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.РасписаниеЗанятий')
          AND name = N'IX_РасписаниеЗанятий_ГруппаВремя'
    )
    BEGIN
        CREATE INDEX IX_РасписаниеЗанятий_ГруппаВремя
        ON dbo.РасписаниеЗанятий(IDРасписание, НомерНедели, ДеньНедели, НомерПары, НазваниеГруппы);
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.РасписаниеЗанятий')
          AND name = N'IX_РасписаниеЗанятий_Связка'
    )
    BEGIN
        CREATE INDEX IX_РасписаниеЗанятий_Связка
        ON dbo.РасписаниеЗанятий(СвязкаЗанятия)
        WHERE СвязкаЗанятия IS NOT NULL;
    END;
END;");
                }

                _excelLikeScheduleSchemaEnsured = true;
            }
        }

        public static string GetGroupFlow(string groupName)
        {
            EnsureGroupFlowSchema();

            object value = ExecuteScalar(@"
SELECT TOP 1 Поток
FROM dbo.Группы
WHERE НазваниеГруппы = @groupName;",
                new SqlParameter("@groupName", groupName));

            if (value == null || value == DBNull.Value)
                return string.Empty;

            return Convert.ToString(value).Trim();
        }

        public static DataTable GetCurriculumCopySources(string targetGroupName, int semester)
        {
            EnsureGroupFlowSchema();

            return Query(@"
;WITH targetGroup AS
(
    SELECT
        НазваниеГруппы,
        Курс,
        Направление,
        ISNULL(NULLIF(LTRIM(RTRIM(Поток)), N''), Направление) AS ПотокКопирования
    FROM dbo.Группы
    WHERE НазваниеГруппы = @targetGroup
)
SELECT
    g.НазваниеГруппы,
    g.Курс,
    g.Направление,
    ISNULL(NULLIF(LTRIM(RTRIM(g.Поток)), N''), g.Направление) AS Поток,
    COUNT(up.IDУчебныйПлан) AS СтрокПлана,
    g.НазваниеГруппы
        + N' | '
        + CAST(COUNT(up.IDУчебныйПлан) AS nvarchar(10))
        + N' строк' AS Отображение
FROM dbo.Группы g
CROSS JOIN targetGroup tg
JOIN dbo.УчебныйПлан up
    ON up.НазваниеГруппы = g.НазваниеГруппы
   AND up.Семестр = @semester
WHERE g.НазваниеГруппы <> tg.НазваниеГруппы
  AND g.Курс = tg.Курс
  AND ISNULL(NULLIF(LTRIM(RTRIM(g.Поток)), N''), g.Направление) = tg.ПотокКопирования
GROUP BY g.НазваниеГруппы, g.Курс, g.Направление, ISNULL(NULLIF(LTRIM(RTRIM(g.Поток)), N''), g.Направление)
HAVING COUNT(up.IDУчебныйПлан) > 0
ORDER BY COUNT(up.IDУчебныйПлан) DESC, g.НазваниеГруппы;",
                new SqlParameter("@targetGroup", targetGroupName),
                new SqlParameter("@semester", semester));
        }

        public static int CopyCurriculumPlan(string sourceGroupName, string targetGroupName, int semester)
        {
            EnsureGroupFlowSchema();
            EnsureSubjectProfileSemesterSchema();
            EnsureLessonTypeSchema();
            EnsureCurriculumRoomRequirementSchema();
            EnsureExcelLikeScheduleSchema();

            int inserted = 0;

            ExecuteInTransaction((con, tx) =>
            {
                string targetProfile = Convert.ToString(ExecuteScalar(con, tx, @"
SELECT TOP 1 Направление
FROM dbo.Группы
WHERE НазваниеГруппы = @targetGroup;",
                    new SqlParameter("@targetGroup", targetGroupName)) ?? string.Empty).Trim();

                DataTable sourceRows = new DataTable();
                using (var cmd = new SqlCommand(@"
SELECT
    src.IDУчебныйПлан,
    src.IDПредмет,
    src.IDПреподаватель,
    src.ТипЗанятия,
    src.ЧасыВСеместре,
    src.ТребуемыйТипАудитории,
    src.ТребуемаяАудитория,
    src.ПотоковоеЗанятие,
    src.ШаблонНедель
FROM dbo.УчебныйПлан src
WHERE src.НазваниеГруппы = @sourceGroup
  AND src.Семестр = @semester
  AND EXISTS
  (
      SELECT 1
      FROM dbo.ПредметыПрофилиСеместры ps
      WHERE ps.IDПредмет = src.IDПредмет
        AND ps.Семестр = @semester
        AND
        (
            ps.Профиль = @allProfilesMarker
            OR (@targetProfile <> N'' AND ps.Профиль = @targetProfile)
        )
  )
  AND NOT EXISTS
  (
      SELECT 1
      FROM dbo.ПредметыИсключенныеГруппы ex
      WHERE ex.IDПредмет = src.IDПредмет
        AND ex.НазваниеГруппы = @targetGroup
  )
ORDER BY src.IDУчебныйПлан;", con, tx))
                {
                    cmd.Parameters.AddWithValue("@sourceGroup", sourceGroupName);
                    cmd.Parameters.AddWithValue("@targetGroup", targetGroupName);
                    cmd.Parameters.AddWithValue("@semester", semester);
                    cmd.Parameters.AddWithValue("@targetProfile", targetProfile);
                    cmd.Parameters.AddWithValue("@allProfilesMarker", AllProfilesMarker);

                    using (var adapter = new SqlDataAdapter(cmd))
                    {
                        adapter.Fill(sourceRows);
                    }
                }

                foreach (DataRow row in sourceRows.Rows)
                {
                    int sourcePlanId = Convert.ToInt32(row["IDУчебныйПлан"]);
                    int subjectId = Convert.ToInt32(row["IDПредмет"]);
                    object teacherId = row["IDПреподаватель"] == DBNull.Value ? (object)DBNull.Value : row["IDПреподаватель"];
                    string lessonType = Convert.ToString(row["ТипЗанятия"]);
                    int hours = Convert.ToInt32(row["ЧасыВСеместре"]);
                    object requiredRoomType = row["ТребуемыйТипАудитории"] == DBNull.Value ? (object)DBNull.Value : row["ТребуемыйТипАудитории"];
                    object requiredRoom = row["ТребуемаяАудитория"] == DBNull.Value ? (object)DBNull.Value : row["ТребуемаяАудитория"];
                    object isFlowLesson = row["ПотоковоеЗанятие"] == DBNull.Value ? (object)false : row["ПотоковоеЗанятие"];
                    object weekPattern = row["ШаблонНедель"] == DBNull.Value ? (object)DBNull.Value : row["ШаблонНедель"];

                    object existingId = ExecuteScalar(con, tx, @"
SELECT TOP 1 IDУчебныйПлан
FROM dbo.УчебныйПлан
WHERE НазваниеГруппы = @targetGroup
  AND Семестр = @semester
  AND IDПредмет = @subjectId
  AND ТипЗанятия = @lessonType
  AND ((@teacherId IS NULL AND IDПреподаватель IS NULL) OR IDПреподаватель = @teacherId);",
                        new SqlParameter("@targetGroup", targetGroupName),
                        new SqlParameter("@semester", semester),
                        new SqlParameter("@subjectId", subjectId),
                        new SqlParameter("@lessonType", lessonType),
                        new SqlParameter("@teacherId", teacherId));

                    if (existingId != null && existingId != DBNull.Value)
                    {
                        ExecuteNonQuery(con, tx, @"
INSERT INTO dbo.ПланНедели(IDУчебныйПлан, НомерНедели)
SELECT @existingPlanId, srcWeeks.НомерНедели
FROM dbo.ПланНедели srcWeeks
WHERE srcWeeks.IDУчебныйПлан = @sourcePlanId
  AND NOT EXISTS
  (
      SELECT 1
      FROM dbo.ПланНедели targetWeeks
      WHERE targetWeeks.IDУчебныйПлан = @existingPlanId
        AND targetWeeks.НомерНедели = srcWeeks.НомерНедели
  );",
                            new SqlParameter("@existingPlanId", Convert.ToInt32(existingId)),
                            new SqlParameter("@sourcePlanId", sourcePlanId));
                        continue;
                    }

                    int newPlanId = Convert.ToInt32(ExecuteScalar(con, tx, @"
INSERT INTO dbo.УчебныйПлан
    (НазваниеГруппы, IDПредмет, IDПреподаватель, Семестр, ТипЗанятия, ЧасыВСеместре, ТребуемыйТипАудитории, ТребуемаяАудитория, ПотоковоеЗанятие, ШаблонНедель)
VALUES
    (@targetGroup, @subjectId, @teacherId, @semester, @lessonType, @hours, @requiredRoomType, @requiredRoom, @isFlowLesson, @weekPattern);
SELECT CAST(SCOPE_IDENTITY() AS int);",
                        new SqlParameter("@targetGroup", targetGroupName),
                        new SqlParameter("@subjectId", subjectId),
                        new SqlParameter("@teacherId", teacherId),
                        new SqlParameter("@semester", semester),
                        new SqlParameter("@lessonType", lessonType),
                        new SqlParameter("@hours", hours),
                        new SqlParameter("@requiredRoomType", requiredRoomType),
                        new SqlParameter("@requiredRoom", requiredRoom),
                        new SqlParameter("@isFlowLesson", isFlowLesson),
                        new SqlParameter("@weekPattern", weekPattern)));

                    ExecuteNonQuery(con, tx, @"
INSERT INTO dbo.ПланНедели(IDУчебныйПлан, НомерНедели)
SELECT @newPlanId, НомерНедели
FROM dbo.ПланНедели
WHERE IDУчебныйПлан = @sourcePlanId;",
                        new SqlParameter("@newPlanId", newPlanId),
                        new SqlParameter("@sourcePlanId", sourcePlanId));

                    inserted++;
                }
            });

            return inserted;
        }

        public static DataTable GetAvailableProfiles()
        {
            EnsureSubjectProfileSemesterSchema();

            return Query(@"
SELECT DISTINCT LTRIM(RTRIM(Направление)) AS Профиль
FROM dbo.Группы
WHERE Направление IS NOT NULL
  AND LTRIM(RTRIM(Направление)) <> N''
ORDER BY Профиль;");
        }

        public static DataTable GetAvailableGroups()
        {
            EnsureSubjectProfileSemesterSchema();

            return Query(@"
SELECT
    НазваниеГруппы,
    CAST(Курс AS nvarchar(10)) + N' курс | '
        + ISNULL(NULLIF(LTRIM(RTRIM(Направление)), N''), N'Без направления')
        + N' | '
        + НазваниеГруппы AS Отображение
FROM dbo.Группы
ORDER BY Курс, Поток, НазваниеГруппы;");
        }

        public static DataTable GetSubjectProfileSemesterAssignments(int subjectId)
        {
            EnsureSubjectProfileSemesterSchema();

            return Query(@"
SELECT Профиль, Семестр
FROM dbo.ПредметыПрофилиСеместры
WHERE IDПредмет = @subjectId
ORDER BY Профиль, Семестр;",
                new SqlParameter("@subjectId", subjectId));
        }

        public static DataTable GetSubjectExcludedGroups(int subjectId)
        {
            EnsureSubjectProfileSemesterSchema();

            return Query(@"
SELECT НазваниеГруппы
FROM dbo.ПредметыИсключенныеГруппы
WHERE IDПредмет = @subjectId
ORDER BY НазваниеГруппы;",
                new SqlParameter("@subjectId", subjectId));
        }

        public static int? FindSubjectIdByName(string name, int? excludeSubjectId = null)
        {
            object value = ExecuteScalar(@"
SELECT TOP (1) IDПредмет
FROM dbo.Предметы
WHERE UPPER(LTRIM(RTRIM(Название))) = UPPER(LTRIM(RTRIM(@name)))
  AND (@excludeSubjectId IS NULL OR IDПредмет <> @excludeSubjectId)
ORDER BY IDПредмет;",
                new SqlParameter("@name", name ?? string.Empty),
                new SqlParameter("@excludeSubjectId", SqlDbType.Int) { Value = (object)excludeSubjectId ?? DBNull.Value });

            if (value == null || value == DBNull.Value)
                return null;

            return Convert.ToInt32(value);
        }

        public static int InsertSubjectWithProfileSemesters(
            string name,
            bool allProfiles,
            IEnumerable<string> profiles,
            IEnumerable<int> semesters,
            IEnumerable<string> excludedGroups)
        {
            using (var con = new SqlConnection(_cs))
            {
                con.Open();

                using (var tx = con.BeginTransaction())
                {
                    try
                    {
                        EnsureSubjectProfileSemesterSchemaCore(con, tx);

                        int subjectId;
                        using (var cmd = new SqlCommand(
                            "INSERT INTO dbo.Предметы(Название) VALUES (@name); SELECT CAST(SCOPE_IDENTITY() AS int);",
                            con, tx))
                        {
                            cmd.Parameters.AddWithValue("@name", name);
                            subjectId = Convert.ToInt32(cmd.ExecuteScalar());
                        }

                        ReplaceSubjectProfileSemesters(con, tx, subjectId, allProfiles, profiles, semesters);
                        ReplaceSubjectExcludedGroups(con, tx, subjectId, excludedGroups);
                        tx.Commit();

                        return subjectId;
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }
        }

        public static void UpdateSubjectWithProfileSemesters(
            int subjectId,
            string name,
            bool allProfiles,
            IEnumerable<string> profiles,
            IEnumerable<int> semesters,
            IEnumerable<string> excludedGroups)
        {
            using (var con = new SqlConnection(_cs))
            {
                con.Open();

                using (var tx = con.BeginTransaction())
                {
                    try
                    {
                        EnsureSubjectProfileSemesterSchemaCore(con, tx);

                        using (var cmd = new SqlCommand(@"
UPDATE dbo.Предметы
SET Название = @name
WHERE IDПредмет = @subjectId;", con, tx))
                        {
                            cmd.Parameters.AddWithValue("@name", name);
                            cmd.Parameters.AddWithValue("@subjectId", subjectId);
                            cmd.ExecuteNonQuery();
                        }

                        ReplaceSubjectProfileSemesters(con, tx, subjectId, allProfiles, profiles, semesters);
                        ReplaceSubjectExcludedGroups(con, tx, subjectId, excludedGroups);
                        tx.Commit();
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }
        }

        public static DataTable GetSubjectsForGroupAndSemester(string groupName, int semester)
        {
            EnsureSubjectProfileSemesterSchema();

            string profile = GetGroupDirection(groupName);
            DataTable dt = Query(@"
SELECT DISTINCT
    p.IDПредмет,
    p.Название
FROM dbo.Предметы p
WHERE EXISTS
(
    SELECT 1
    FROM dbo.ПредметыПрофилиСеместры ps
    WHERE ps.IDПредмет = p.IDПредмет
      AND ps.Семестр = @semester
      AND
      (
          ps.Профиль = @allProfilesMarker
          OR (@profile <> N'' AND ps.Профиль = @profile)
      )
)
AND NOT EXISTS
(
    SELECT 1
    FROM dbo.ПредметыИсключенныеГруппы ex
    WHERE ex.IDПредмет = p.IDПредмет
      AND ex.НазваниеГруппы = @groupName
)
ORDER BY p.Название;",
                new SqlParameter("@semester", semester),
                new SqlParameter("@groupName", groupName),
                new SqlParameter("@profile", profile ?? string.Empty),
                new SqlParameter("@allProfilesMarker", AllProfilesMarker));

            return dt;
        }

        public static bool SubjectAllowedForGroupAndSemester(int subjectId, string groupName, int semester)
        {
            EnsureSubjectProfileSemesterSchema();

            string profile = GetGroupDirection(groupName);
            object value = ExecuteScalar(@"
SELECT COUNT(*)
FROM dbo.Предметы p
WHERE p.IDПредмет = @subjectId
  AND EXISTS
  (
      SELECT 1
      FROM dbo.ПредметыПрофилиСеместры ps
      WHERE ps.IDПредмет = p.IDПредмет
        AND ps.Семестр = @semester
        AND
        (
            ps.Профиль = @allProfilesMarker
            OR (@profile <> N'' AND ps.Профиль = @profile)
        )
  )
  AND NOT EXISTS
  (
      SELECT 1
      FROM dbo.ПредметыИсключенныеГруппы ex
      WHERE ex.IDПредмет = p.IDПредмет
        AND ex.НазваниеГруппы = @groupName
  );",
                new SqlParameter("@subjectId", subjectId),
                new SqlParameter("@semester", semester),
                new SqlParameter("@groupName", groupName),
                new SqlParameter("@profile", profile ?? string.Empty),
                new SqlParameter("@allProfilesMarker", AllProfilesMarker));

            return Convert.ToInt32(value) > 0;
        }

        public static string GetGroupDirection(string groupName)
        {
            object value = ExecuteScalar(@"
SELECT TOP 1 Направление
FROM dbo.Группы
WHERE НазваниеГруппы = @groupName;",
                new SqlParameter("@groupName", groupName));

            if (value == null || value == DBNull.Value)
                return "";

            return Convert.ToString(value).Trim();
        }

        public static int GetWeeksForGroup(string groupName, int semester, int defaultWeeks = 16)
        {
            object obj = ExecuteScalar(@"
IF OBJECT_ID(N'dbo.ГруппаСеместр', N'U') IS NOT NULL
BEGIN
    SELECT TOP 1 КоличествоНедель
    FROM dbo.ГруппаСеместр
    WHERE НазваниеГруппы = @g AND Семестр = @s;
END
ELSE IF OBJECT_ID(N'dbo.НастройкиСеместраГруппы', N'U') IS NOT NULL
BEGIN
    SELECT TOP 1 НедельВСеместре
    FROM dbo.НастройкиСеместраГруппы
    WHERE НазваниеГруппы = @g AND Семестр = @s;
END",
                new SqlParameter("@g", groupName),
                new SqlParameter("@s", semester));

            if (obj == null || obj == DBNull.Value)
                return defaultWeeks;

            return Convert.ToInt32(obj);
        }

        public static bool IsPhysicalEducationSubject(int subjectId)
        {
            object value = ExecuteScalar(@"
SELECT COUNT(*)
FROM dbo.Предметы
WHERE IDПредмет = @subjectId
  AND LTRIM(RTRIM(Название)) IN
  (
      N'Физкультура',
      N'Физ-ра',
      N'Элективные курсы по физической культуре и спорту'
  );",
                new SqlParameter("@subjectId", subjectId));

            return Convert.ToInt32(value) > 0;
        }

        public static void EnsureRoomTypeSchema()
        {
            using (var con = new SqlConnection(_cs))
            {
                con.Open();

                using (var cmd = new SqlCommand(@"
IF OBJECT_ID(N'dbo.Аудитории', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.Аудитории', N'ТипАудитории') IS NOT NULL
BEGIN
    DECLARE @sql nvarchar(max) = N'';

    SELECT @sql = @sql + N'ALTER TABLE dbo.Аудитории DROP CONSTRAINT ' + QUOTENAME(cc.name) + N';'
    FROM sys.check_constraints cc
    WHERE cc.parent_object_id = OBJECT_ID(N'dbo.Аудитории')
      AND cc.definition LIKE N'%ТипАудитории%';

    IF @sql <> N''
        EXEC sp_executesql @sql;

    ALTER TABLE dbo.Аудитории
        ALTER COLUMN ТипАудитории nvarchar(100) NOT NULL;

    UPDATE dbo.Аудитории
    SET ТипАудитории = N'Упражнения'
    WHERE ТипАудитории = N'Физкультурные залы';

    UPDATE dbo.Аудитории
    SET ТипАудитории = N'Спец класс'
    WHERE ТипАудитории = N'Класс с электроникой';

    ALTER TABLE dbo.Аудитории WITH CHECK
    ADD CONSTRAINT CK_Аудитории_ТипАудитории
    CHECK (ТипАудитории IN
    (
        N'Лекции',
        N'Упражнения',
        N'Компьютерный класс',
        N'Спец класс',
        N'Мультимедийные аудитории'
    ));
END;", con))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static void EnsureLessonTypeSchema()
        {
            using (var con = new SqlConnection(_cs))
            {
                con.Open();

                using (var cmd = new SqlCommand(@"
IF OBJECT_ID(N'dbo.УчебныйПлан', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.УчебныйПлан', N'ТипЗанятия') IS NOT NULL
BEGIN
    DECLARE @planSql nvarchar(max) = N'';

    SELECT @planSql = @planSql + N'ALTER TABLE dbo.УчебныйПлан DROP CONSTRAINT ' + QUOTENAME(cc.name) + N';'
    FROM sys.check_constraints cc
    WHERE cc.parent_object_id = OBJECT_ID(N'dbo.УчебныйПлан')
      AND cc.definition LIKE N'%ТипЗанятия%';

    IF @planSql <> N''
        EXEC sp_executesql @planSql;

    UPDATE dbo.УчебныйПлан
    SET ТипЗанятия = N'Физкультура'
    WHERE ТипЗанятия = N'Физ-ра';

    UPDATE dbo.УчебныйПлан
    SET ТипЗанятия = N'У'
    WHERE ТипЗанятия = N'ПЗ';

    ALTER TABLE dbo.УчебныйПлан
        ALTER COLUMN ТипЗанятия nvarchar(20) NOT NULL;

    IF EXISTS
    (
        SELECT 1
        FROM sys.columns
        WHERE object_id = OBJECT_ID(N'dbo.УчебныйПлан')
          AND name = N'IDПреподаватель'
          AND is_nullable = 0
    )
    BEGIN
        ALTER TABLE dbo.УчебныйПлан
            ALTER COLUMN IDПреподаватель int NULL;
    END;

    ALTER TABLE dbo.УчебныйПлан WITH CHECK
    ADD CONSTRAINT CK_УчебныйПлан_ТипЗанятия
    CHECK (ТипЗанятия IN (N'ЛК', N'У', N'ЛБ', N'КР', N'Физкультура'));
END;

IF OBJECT_ID(N'dbo.РасписаниеЗанятий', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.РасписаниеЗанятий', N'ТипЗанятия') IS NOT NULL
BEGIN
    DECLARE @scheduleSql nvarchar(max) = N'';

    SELECT @scheduleSql = @scheduleSql + N'ALTER TABLE dbo.РасписаниеЗанятий DROP CONSTRAINT ' + QUOTENAME(cc.name) + N';'
    FROM sys.check_constraints cc
    WHERE cc.parent_object_id = OBJECT_ID(N'dbo.РасписаниеЗанятий')
      AND cc.definition LIKE N'%ТипЗанятия%';

    IF @scheduleSql <> N''
        EXEC sp_executesql @scheduleSql;

    UPDATE dbo.РасписаниеЗанятий
    SET ТипЗанятия = N'Физкультура'
    WHERE ТипЗанятия = N'Физ-ра';

    UPDATE dbo.РасписаниеЗанятий
    SET ТипЗанятия = N'У'
    WHERE ТипЗанятия = N'ПЗ';

    ALTER TABLE dbo.РасписаниеЗанятий
        ALTER COLUMN ТипЗанятия nvarchar(20) NOT NULL;

    IF EXISTS
    (
        SELECT 1
        FROM sys.columns
        WHERE object_id = OBJECT_ID(N'dbo.РасписаниеЗанятий')
          AND name = N'IDПреподаватель'
          AND is_nullable = 0
    )
    BEGIN
        IF EXISTS
        (
            SELECT 1
            FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'dbo.РасписаниеЗанятий')
              AND name = N'UQ_РасписаниеЗанятий_ПреподавательВремя'
        )
        BEGIN
            DROP INDEX UQ_РасписаниеЗанятий_ПреподавательВремя ON dbo.РасписаниеЗанятий;
        END;

        ALTER TABLE dbo.РасписаниеЗанятий
            ALTER COLUMN IDПреподаватель int NULL;
    END;

    ALTER TABLE dbo.РасписаниеЗанятий WITH CHECK
    ADD CONSTRAINT CK_РасписаниеЗанятий_ТипЗанятия
    CHECK (ТипЗанятия IN (N'ЛК', N'У', N'ЛБ', N'КР', N'Физкультура'));
END;", con))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static void EnsureScheduleRoomNullableSchema()
        {
            if (_scheduleRoomNullableSchemaEnsured)
                return;

            lock (_scheduleRoomNullableSchemaSync)
            {
                if (_scheduleRoomNullableSchemaEnsured)
                    return;

                using (var con = new SqlConnection(_cs))
                {
                    con.Open();

                    using (var cmd = new SqlCommand(@"
IF OBJECT_ID(N'dbo.РасписаниеЗанятий', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.РасписаниеЗанятий', N'НомерАудитории') IS NOT NULL
BEGIN
    DECLARE @sql nvarchar(max) = N'';

    IF EXISTS
    (
        SELECT 1
        FROM sys.columns
        WHERE object_id = OBJECT_ID(N'dbo.РасписаниеЗанятий')
          AND name = N'НомерАудитории'
          AND is_nullable = 0
    )
    BEGIN
        SELECT @sql = @sql + N'ALTER TABLE dbo.РасписаниеЗанятий DROP CONSTRAINT ' + QUOTENAME(fk.name) + N';'
        FROM sys.foreign_keys fk
        JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
        JOIN sys.columns c ON c.object_id = fkc.parent_object_id AND c.column_id = fkc.parent_column_id
        WHERE fk.parent_object_id = OBJECT_ID(N'dbo.РасписаниеЗанятий')
          AND c.name = N'НомерАудитории';

        IF @sql <> N''
            EXEC sp_executesql @sql;

        IF EXISTS
        (
            SELECT 1
            FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'dbo.РасписаниеЗанятий')
              AND name = N'UQ_РасписаниеЗанятий_АудиторияВремя'
        )
        BEGIN
            DROP INDEX UQ_РасписаниеЗанятий_АудиторияВремя ON dbo.РасписаниеЗанятий;
        END;

        ALTER TABLE dbo.РасписаниеЗанятий
            ALTER COLUMN НомерАудитории nvarchar(20) NULL;
    END;

    UPDATE rz
    SET НомерАудитории = NULL
    FROM dbo.РасписаниеЗанятий rz
    LEFT JOIN dbo.Аудитории a ON a.НомерАудитории = rz.НомерАудитории
    WHERE rz.НомерАудитории IS NOT NULL
      AND
      (
          rz.ТипЗанятия = N'Физкультура'
          OR LTRIM(RTRIM(rz.НомерАудитории)) = N''
          OR a.НомерАудитории IS NULL
      );

    IF EXISTS
    (
        SELECT 1
        FROM sys.foreign_keys
        WHERE parent_object_id = OBJECT_ID(N'dbo.РасписаниеЗанятий')
          AND name = N'FK_РасписаниеЗанятий_Аудитории'
          AND update_referential_action <> 1
    )
    BEGIN
        ALTER TABLE dbo.РасписаниеЗанятий
        DROP CONSTRAINT FK_РасписаниеЗанятий_Аудитории;
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.foreign_keys
        WHERE parent_object_id = OBJECT_ID(N'dbo.РасписаниеЗанятий')
          AND name = N'FK_РасписаниеЗанятий_Аудитории'
    )
    BEGIN
        SELECT @sql = N'';

        SELECT @sql = @sql + N'ALTER TABLE dbo.РасписаниеЗанятий DROP CONSTRAINT ' + QUOTENAME(fk.name) + N';'
        FROM sys.foreign_keys fk
        JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
        JOIN sys.columns c ON c.object_id = fkc.parent_object_id AND c.column_id = fkc.parent_column_id
        WHERE fk.parent_object_id = OBJECT_ID(N'dbo.РасписаниеЗанятий')
          AND c.name = N'НомерАудитории';

        IF @sql <> N''
            EXEC sp_executesql @sql;

        ALTER TABLE dbo.РасписаниеЗанятий WITH CHECK
        ADD CONSTRAINT FK_РасписаниеЗанятий_Аудитории
            FOREIGN KEY (НомерАудитории)
            REFERENCES dbo.Аудитории(НомерАудитории)
            ON UPDATE CASCADE;
    END;

    IF EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.РасписаниеЗанятий')
          AND name = N'UQ_РасписаниеЗанятий_АудиторияВремя'
    )
    BEGIN
        DROP INDEX UQ_РасписаниеЗанятий_АудиторияВремя ON dbo.РасписаниеЗанятий;
    END;

    IF EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.РасписаниеЗанятий')
          AND name = N'UX_Schedule_RoomTime'
    )
    BEGIN
        DROP INDEX UX_Schedule_RoomTime ON dbo.РасписаниеЗанятий;
    END;

    IF EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.РасписаниеЗанятий')
          AND name = N'UX_Аудитория_Время'
    )
    BEGIN
        DROP INDEX UX_Аудитория_Время ON dbo.РасписаниеЗанятий;
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.РасписаниеЗанятий')
          AND name = N'IX_РасписаниеЗанятий_АудиторияВремя'
    )
    BEGIN
        CREATE INDEX IX_РасписаниеЗанятий_АудиторияВремя
        ON dbo.РасписаниеЗанятий(IDРасписание, НомерНедели, ДеньНедели, НомерПары, НомерАудитории)
        WHERE НомерАудитории IS NOT NULL;
    END;

    IF COL_LENGTH(N'dbo.РасписаниеЗанятий', N'IDПреподаватель') IS NOT NULL
    BEGIN
        IF EXISTS
        (
            SELECT 1
            FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'dbo.РасписаниеЗанятий')
              AND name = N'UQ_РасписаниеЗанятий_ПреподавательВремя'
        )
        BEGIN
            DROP INDEX UQ_РасписаниеЗанятий_ПреподавательВремя ON dbo.РасписаниеЗанятий;
        END;

        IF EXISTS
        (
            SELECT 1
            FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'dbo.РасписаниеЗанятий')
              AND name = N'UX_Schedule_TeacherTime'
        )
        BEGIN
            DROP INDEX UX_Schedule_TeacherTime ON dbo.РасписаниеЗанятий;
        END;

        IF EXISTS
        (
            SELECT 1
            FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'dbo.РасписаниеЗанятий')
              AND name = N'UX_Преподаватель_Время'
        )
        BEGIN
            DROP INDEX UX_Преподаватель_Время ON dbo.РасписаниеЗанятий;
        END;

        IF NOT EXISTS
        (
            SELECT 1
            FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'dbo.РасписаниеЗанятий')
              AND name = N'IX_РасписаниеЗанятий_ПреподавательВремя'
        )
        BEGIN
            CREATE INDEX IX_РасписаниеЗанятий_ПреподавательВремя
            ON dbo.РасписаниеЗанятий(IDРасписание, НомерНедели, ДеньНедели, НомерПары, IDПреподаватель)
            WHERE IDПреподаватель IS NOT NULL;
        END;
    END;
END;", con))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }

                _scheduleRoomNullableSchemaEnsured = true;
            }
        }

        private static void EnsureSubjectProfileSemesterSchemaCore(SqlConnection con, SqlTransaction tx)
        {
            using (var cmd = new SqlCommand(@"
IF OBJECT_ID(N'dbo.ПредметыПрофилиСеместры', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ПредметыПрофилиСеместры
    (
        IDПредмет int NOT NULL,
        Профиль nvarchar(120) NOT NULL,
        Семестр int NOT NULL,
        CONSTRAINT PK_ПредметыПрофилиСеместры PRIMARY KEY (IDПредмет, Профиль, Семестр),
        CONSTRAINT FK_ПредметыПрофилиСеместры_Предметы FOREIGN KEY (IDПредмет)
            REFERENCES dbo.Предметы(IDПредмет) ON DELETE CASCADE,
        CONSTRAINT CK_ПредметыПрофилиСеместры_Семестр CHECK (Семестр BETWEEN 1 AND 10)
    );
END;

IF OBJECT_ID(N'dbo.Группы', N'U') IS NOT NULL
   AND NOT EXISTS
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

IF OBJECT_ID(N'dbo.ПредметыИсключенныеГруппы', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ПредметыИсключенныеГруппы
    (
        IDПредмет int NOT NULL,
        НазваниеГруппы nvarchar(50) NOT NULL,
        CONSTRAINT PK_ПредметыИсключенныеГруппы PRIMARY KEY (IDПредмет, НазваниеГруппы),
        CONSTRAINT FK_ПредметыИсключенныеГруппы_Предметы FOREIGN KEY (IDПредмет)
            REFERENCES dbo.Предметы(IDПредмет) ON DELETE CASCADE,
        CONSTRAINT FK_ПредметыИсключенныеГруппы_Группы FOREIGN KEY (НазваниеГруппы)
            REFERENCES dbo.Группы(НазваниеГруппы) ON DELETE CASCADE
    );
END;", con, tx))
            {
                cmd.ExecuteNonQuery();
            }
        }

        private static void EnsureGroupFlowSchemaCore(SqlConnection con, SqlTransaction tx)
        {
            using (var cmd = new SqlCommand(@"
IF OBJECT_ID(N'dbo.Группы', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.Группы', N'Поток') IS NULL
BEGIN
    ALTER TABLE dbo.Группы
        ADD Поток nvarchar(120) NULL;
END;", con, tx))
            {
                cmd.ExecuteNonQuery();
            }

            using (var cmd = new SqlCommand(@"
IF OBJECT_ID(N'dbo.Группы', N'U') IS NOT NULL
BEGIN
    UPDATE dbo.Группы
    SET Поток = Направление
    WHERE Поток IS NULL OR LTRIM(RTRIM(Поток)) = N'';
END;", con, tx))
            {
                cmd.ExecuteNonQuery();
            }

            using (var cmd = new SqlCommand(@"
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
END;", con, tx))
            {
                cmd.ExecuteNonQuery();
            }
        }

        private static void EnsureCurriculumRoomRequirementSchemaCore(SqlConnection con, SqlTransaction tx)
        {
            using (var cmd = new SqlCommand(@"
IF OBJECT_ID(N'dbo.УчебныйПлан', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.УчебныйПлан', N'ТребуемыйТипАудитории') IS NULL
BEGIN
    ALTER TABLE dbo.УчебныйПлан
        ADD ТребуемыйТипАудитории nvarchar(100) NULL;
END;", con, tx))
            {
                cmd.ExecuteNonQuery();
            }

            using (var cmd = new SqlCommand(@"
IF OBJECT_ID(N'dbo.УчебныйПлан', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.УчебныйПлан', N'ТребуемаяАудитория') IS NULL
BEGIN
    ALTER TABLE dbo.УчебныйПлан
        ADD ТребуемаяАудитория nvarchar(20) NULL;
END;", con, tx))
            {
                cmd.ExecuteNonQuery();
            }

            using (var cmd = new SqlCommand(@"
IF OBJECT_ID(N'dbo.УчебныйПлан', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.Аудитории', N'U') IS NOT NULL
BEGIN
    UPDATE up
    SET ТребуемаяАудитория = NULL
    FROM dbo.УчебныйПлан up
    LEFT JOIN dbo.Аудитории a ON a.НомерАудитории = up.ТребуемаяАудитория
    WHERE up.ТребуемаяАудитория IS NOT NULL
      AND a.НомерАудитории IS NULL;
END;", con, tx))
            {
                cmd.ExecuteNonQuery();
            }

            using (var cmd = new SqlCommand(@"
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
END;", con, tx))
            {
                cmd.ExecuteNonQuery();
            }

            using (var cmd = new SqlCommand(@"
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
END;", con, tx))
            {
                cmd.ExecuteNonQuery();
            }
        }

        private static void EnsureTeacherSubjectSchemaCore(SqlConnection con, SqlTransaction tx)
        {
            using (var cmd = new SqlCommand(@"
IF OBJECT_ID(N'dbo.ПреподавателиПредметы', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ПреподавателиПредметы
    (
        IDПреподаватель int NOT NULL,
        IDПредмет int NOT NULL,
        CONSTRAINT PK_ПреподавателиПредметы PRIMARY KEY (IDПреподаватель, IDПредмет),
        CONSTRAINT FK_ПреподавателиПредметы_Преподаватели FOREIGN KEY (IDПреподаватель)
            REFERENCES dbo.Преподаватели(IDПреподаватель) ON DELETE CASCADE,
        CONSTRAINT FK_ПреподавателиПредметы_Предметы FOREIGN KEY (IDПредмет)
            REFERENCES dbo.Предметы(IDПредмет) ON DELETE CASCADE
    );
END;

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
END;", con, tx))
            {
                cmd.ExecuteNonQuery();
            }

            using (var cmd = new SqlCommand(@"
IF COL_LENGTH(N'dbo.Преподаватели', N'ЗакрепленнаяАудитория') IS NULL
BEGIN
    ALTER TABLE dbo.Преподаватели
        ADD ЗакрепленнаяАудитория nvarchar(20) NULL;
END;", con, tx))
            {
                cmd.ExecuteNonQuery();
            }

            using (var cmd = new SqlCommand(@"
IF OBJECT_ID(N'dbo.Аудитории', N'U') IS NOT NULL
BEGIN
    UPDATE t
    SET ЗакрепленнаяАудитория = NULL
    FROM dbo.Преподаватели t
    LEFT JOIN dbo.Аудитории a ON a.НомерАудитории = t.ЗакрепленнаяАудитория
    WHERE t.ЗакрепленнаяАудитория IS NOT NULL
      AND a.НомерАудитории IS NULL;

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.foreign_keys
        WHERE name = N'FK_Преподаватели_Аудитории_ЗакрепленнаяАудитория'
          AND parent_object_id = OBJECT_ID(N'dbo.Преподаватели')
    )
    BEGIN
        ALTER TABLE dbo.Преподаватели WITH CHECK
        ADD CONSTRAINT FK_Преподаватели_Аудитории_ЗакрепленнаяАудитория
            FOREIGN KEY (ЗакрепленнаяАудитория)
            REFERENCES dbo.Аудитории(НомерАудитории)
            ON DELETE SET NULL
            ON UPDATE CASCADE;
    END;
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_Преподаватели_ЗакрепленнаяАудитория'
      AND object_id = OBJECT_ID(N'dbo.Преподаватели')
)
BEGIN
    CREATE INDEX IX_Преподаватели_ЗакрепленнаяАудитория
        ON dbo.Преподаватели(ЗакрепленнаяАудитория);
END;", con, tx))
            {
                cmd.ExecuteNonQuery();
            }
        }

        private static void ReplaceSubjectProfileSemesters(
            SqlConnection con,
            SqlTransaction tx,
            int subjectId,
            bool allProfiles,
            IEnumerable<string> profiles,
            IEnumerable<int> semesters)
        {
            using (var deleteCmd = new SqlCommand(
                "DELETE FROM dbo.ПредметыПрофилиСеместры WHERE IDПредмет = @subjectId;",
                con, tx))
            {
                deleteCmd.Parameters.AddWithValue("@subjectId", subjectId);
                deleteCmd.ExecuteNonQuery();
            }

            var uniqueSemesters = new HashSet<int>();
            if (semesters != null)
            {
                foreach (int semester in semesters)
                {
                    if (semester >= 1 && semester <= 10)
                        uniqueSemesters.Add(semester);
                }
            }

            if (uniqueSemesters.Count == 0)
                return;

            var uniqueProfiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (allProfiles)
            {
                uniqueProfiles.Add(AllProfilesMarker);
            }
            else if (profiles != null)
            {
                foreach (string profile in profiles)
                {
                    string trimmed = (profile ?? "").Trim();
                    if (!string.IsNullOrWhiteSpace(trimmed))
                        uniqueProfiles.Add(trimmed);
                }
            }

            if (uniqueProfiles.Count == 0)
                return;

            using (var insertCmd = new SqlCommand(
                "INSERT INTO dbo.ПредметыПрофилиСеместры(IDПредмет, Профиль, Семестр) VALUES (@subjectId, @profile, @semester);",
                con, tx))
            {
                insertCmd.Parameters.AddWithValue("@subjectId", subjectId);
                var profileParam = insertCmd.Parameters.Add("@profile", SqlDbType.NVarChar, 120);
                var semesterParam = insertCmd.Parameters.Add("@semester", SqlDbType.Int);

                foreach (string profile in uniqueProfiles)
                {
                    foreach (int semester in uniqueSemesters)
                    {
                        profileParam.Value = profile;
                        semesterParam.Value = semester;
                        insertCmd.ExecuteNonQuery();
                    }
                }
            }
        }

        private static void ReplaceSubjectExcludedGroups(
            SqlConnection con,
            SqlTransaction tx,
            int subjectId,
            IEnumerable<string> excludedGroups)
        {
            using (var deleteCmd = new SqlCommand(
                "DELETE FROM dbo.ПредметыИсключенныеГруппы WHERE IDПредмет = @subjectId;",
                con, tx))
            {
                deleteCmd.Parameters.AddWithValue("@subjectId", subjectId);
                deleteCmd.ExecuteNonQuery();
            }

            var uniqueGroups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (excludedGroups != null)
            {
                foreach (string group in excludedGroups)
                {
                    string trimmed = (group ?? string.Empty).Trim();
                    if (!string.IsNullOrWhiteSpace(trimmed))
                        uniqueGroups.Add(trimmed);
                }
            }

            if (uniqueGroups.Count == 0)
                return;

            using (var insertCmd = new SqlCommand(
                "INSERT INTO dbo.ПредметыИсключенныеГруппы(IDПредмет, НазваниеГруппы) VALUES (@subjectId, @groupName);",
                con, tx))
            {
                insertCmd.Parameters.AddWithValue("@subjectId", subjectId);
                var groupParam = insertCmd.Parameters.Add("@groupName", SqlDbType.NVarChar, 50);

                foreach (string group in uniqueGroups)
                {
                    groupParam.Value = group;
                    insertCmd.ExecuteNonQuery();
                }
            }
        }

        private static void ReplaceTeacherSubjects(SqlConnection con, SqlTransaction tx, int teacherId, IEnumerable<int> subjectIds)
        {
            using (var deleteCmd = new SqlCommand(
                "DELETE FROM dbo.ПреподавателиПредметы WHERE IDПреподаватель = @teacherId;",
                con, tx))
            {
                deleteCmd.Parameters.AddWithValue("@teacherId", teacherId);
                deleteCmd.ExecuteNonQuery();
            }

            using (var insertCmd = new SqlCommand(
                "INSERT INTO dbo.ПреподавателиПредметы(IDПреподаватель, IDПредмет) VALUES (@teacherId, @subjectId);",
                con, tx))
            {
                insertCmd.Parameters.AddWithValue("@teacherId", teacherId);
                var subjectParam = insertCmd.Parameters.Add("@subjectId", SqlDbType.Int);
                var uniqueSubjects = new HashSet<int>();

                if (subjectIds == null)
                    return;

                foreach (int subjectId in subjectIds)
                {
                    if (!uniqueSubjects.Add(subjectId))
                        continue;

                    subjectParam.Value = subjectId;
                    insertCmd.ExecuteNonQuery();
                }
            }
        }

        private static int? FindTeacherIdByFio(SqlConnection con, SqlTransaction tx, string fio, int? excludeTeacherId)
        {
            using (var cmd = new SqlCommand(@"
SELECT TOP (1) IDПреподаватель
FROM dbo.Преподаватели
WHERE UPPER(LTRIM(RTRIM(ФИО))) = UPPER(LTRIM(RTRIM(@fio)))
  AND (@excludeTeacherId IS NULL OR IDПреподаватель <> @excludeTeacherId)
ORDER BY IDПреподаватель;", con, tx))
            {
                cmd.Parameters.AddWithValue("@fio", fio ?? string.Empty);
                cmd.Parameters.Add("@excludeTeacherId", SqlDbType.Int).Value = (object)excludeTeacherId ?? DBNull.Value;

                object value = cmd.ExecuteScalar();
                if (value == null || value == DBNull.Value)
                    return null;

                return Convert.ToInt32(value);
            }
        }

        private static void AddTeacherSubjects(SqlConnection con, SqlTransaction tx, int teacherId, IEnumerable<int> subjectIds)
        {
            if (subjectIds == null)
                return;

            using (var insertCmd = new SqlCommand(@"
IF NOT EXISTS
(
    SELECT 1
    FROM dbo.ПреподавателиПредметы
    WHERE IDПреподаватель = @teacherId
      AND IDПредмет = @subjectId
)
BEGIN
    INSERT INTO dbo.ПреподавателиПредметы(IDПреподаватель, IDПредмет)
    VALUES (@teacherId, @subjectId);
END;", con, tx))
            {
                insertCmd.Parameters.AddWithValue("@teacherId", teacherId);
                var subjectParam = insertCmd.Parameters.Add("@subjectId", SqlDbType.Int);
                var uniqueSubjects = new HashSet<int>();

                foreach (int subjectId in subjectIds)
                {
                    if (!uniqueSubjects.Add(subjectId))
                        continue;

                    subjectParam.Value = subjectId;
                    insertCmd.ExecuteNonQuery();
                }
            }
        }

        private static DataTable CreateSubjectLookupTable()
        {
            var dt = new DataTable();
            dt.Columns.Add("IDПредмет", typeof(int));
            dt.Columns.Add("Название", typeof(string));
            return dt;
        }
    }
}
