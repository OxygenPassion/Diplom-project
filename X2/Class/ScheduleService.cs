using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace X2.Class
{
    internal static class ScheduleService
    {
        private const string PhysicalLessonType = "Физкультура";
        private const string LabLessonType = "ЛБ";
        public static ScheduleOperationResult AddEntry(ScheduleEntryData entry)
        {
            DataBase.EnsureExcelLikeScheduleSchema();

            ScheduleOperationResult validation = ValidateEntry(entry, null);
            if (!validation.Success)
                return validation;

            object newId = DataBase.ExecuteScalarInTransaction(@"
INSERT INTO dbo.РасписаниеЗанятий
    (IDРасписание, НомерНедели, ДеньНедели, НомерПары, НазваниеГруппы, IDПредмет, ТипЗанятия, IDПреподаватель, IDПреподаватель2, НомерАудитории, СвязкаЗанятия, IDУчебныйПлан)
VALUES
    (@sid, @week, @day, @pair, @groupName, @subjectId, @lessonType, @teacherId, @secondTeacherId, @room, @seriesId, @planId);
SELECT CAST(SCOPE_IDENTITY() AS int);",
                new SqlParameter("@sid", entry.ScheduleId),
                new SqlParameter("@week", entry.Week),
                new SqlParameter("@day", entry.Day),
                new SqlParameter("@pair", entry.Pair),
                new SqlParameter("@groupName", entry.GroupName),
                new SqlParameter("@subjectId", entry.SubjectId),
                new SqlParameter("@lessonType", entry.LessonType),
                new SqlParameter("@teacherId", (object)entry.TeacherId ?? DBNull.Value),
                new SqlParameter("@secondTeacherId", (object)entry.SecondTeacherId ?? DBNull.Value),
                new SqlParameter("@room", string.IsNullOrWhiteSpace(entry.Room) ? (object)DBNull.Value : entry.Room),
                new SqlParameter("@seriesId", (object)entry.SeriesId ?? DBNull.Value),
                new SqlParameter("@planId", (object)entry.CurriculumPlanId ?? DBNull.Value));

            return ScheduleOperationResult.Ok(Convert.ToInt32(newId));
        }

        public static ScheduleOperationResult UpdateEntry(ScheduleEntryData entry)
        {
            DataBase.EnsureExcelLikeScheduleSchema();

            if (entry == null || !entry.EntryId.HasValue)
                return ScheduleOperationResult.Fail("Выберите запись в таблице.");

            ScheduleOperationResult validation = ValidateEntry(entry, entry.EntryId);
            if (!validation.Success)
                return validation;

            DataBase.ExecuteNonQuery(@"
UPDATE dbo.РасписаниеЗанятий
SET НомерНедели = @week,
    ДеньНедели = @day,
    НомерПары = @pair,
    НазваниеГруппы = @groupName,
    IDПредмет = @subjectId,
    ТипЗанятия = @lessonType,
    IDПреподаватель = @teacherId,
    IDПреподаватель2 = @secondTeacherId,
    НомерАудитории = @room,
    СвязкаЗанятия = @seriesId,
    IDУчебныйПлан = @planId
WHERE IDЗапись = @id;",
                new SqlParameter("@week", entry.Week),
                new SqlParameter("@day", entry.Day),
                new SqlParameter("@pair", entry.Pair),
                new SqlParameter("@groupName", entry.GroupName),
                new SqlParameter("@subjectId", entry.SubjectId),
                new SqlParameter("@lessonType", entry.LessonType),
                new SqlParameter("@teacherId", (object)entry.TeacherId ?? DBNull.Value),
                new SqlParameter("@secondTeacherId", (object)entry.SecondTeacherId ?? DBNull.Value),
                new SqlParameter("@room", string.IsNullOrWhiteSpace(entry.Room) ? (object)DBNull.Value : entry.Room),
                new SqlParameter("@seriesId", (object)entry.SeriesId ?? DBNull.Value),
                new SqlParameter("@planId", (object)entry.CurriculumPlanId ?? DBNull.Value),
                new SqlParameter("@id", entry.EntryId.Value));

            return ScheduleOperationResult.Ok(entry.EntryId.Value);
        }

        public static void DeleteEntry(int entryId)
        {
            DataBase.ExecuteNonQuery(
                "DELETE FROM dbo.РасписаниеЗанятий WHERE IDЗапись = @id;",
                new SqlParameter("@id", entryId));
        }

        public static ScheduleOperationResult ValidateEntry(ScheduleEntryData entry, int? ignoreEntryId)
        {
            DataBase.EnsureExcelLikeScheduleSchema();

            if (entry == null)
                return ScheduleOperationResult.Fail("Данные записи расписания не заполнены.");

            if (!DataBase.SubjectAllowedForGroupAndSemester(entry.SubjectId, entry.GroupName, entry.Semester))
                return ScheduleOperationResult.Fail("Предмет не назначен для профиля группы и выбранного семестра.");

            if (entry.TeacherId.HasValue && !DataBase.TeacherCanTeachSubject(entry.TeacherId.Value, entry.SubjectId))
                return ScheduleOperationResult.Fail("Выбранный преподаватель не закреплен за этим предметом.");

            if (entry.SecondTeacherId.HasValue
                && !string.Equals(entry.LessonType, LabLessonType, StringComparison.OrdinalIgnoreCase))
                return ScheduleOperationResult.Fail("Второго преподавателя можно указывать только для ЛБ.");

            if (entry.SecondTeacherId.HasValue && entry.TeacherId.HasValue && entry.SecondTeacherId.Value == entry.TeacherId.Value)
                return ScheduleOperationResult.Fail("Второй преподаватель должен отличаться от основного.");

            if (entry.SecondTeacherId.HasValue && !DataBase.TeacherCanTeachSubject(entry.SecondTeacherId.Value, entry.SubjectId))
                return ScheduleOperationResult.Fail("Второй преподаватель не закреплен за этим предметом.");

            if (ScheduleConflictChecker.HasScheduleConflict(
                entry.ScheduleId,
                entry.Week,
                entry.Day,
                entry.Pair,
                entry.GroupName,
                entry.TeacherId,
                entry.Room,
                ignoreEntryId,
                entry.SecondTeacherId))
            {
                return ScheduleOperationResult.Fail("Конфликт: группа, преподаватель или аудитория уже заняты в это время.");
            }

            return ScheduleOperationResult.Ok(entry.EntryId);
        }

        public static List<int> LoadSeriesWeeks(ScheduleSeriesData series)
        {
            if (series == null)
                return new List<int>();

            series.Pair = GetSeriesStartPair(series);

            DataTable dt = DataBase.Query(@"
SELECT НомерНедели
FROM dbo.РасписаниеЗанятий
WHERE IDРасписание = @sid
  AND ДеньНедели = @day
  AND НомерПары = @pair
  AND НазваниеГруппы = @groupName
  AND IDПредмет = @subjectId
  AND ТипЗанятия = @lessonType
  AND ((@teacherId IS NULL AND IDПреподаватель IS NULL) OR IDПреподаватель = @teacherId)
  AND ((@secondTeacherId IS NULL AND IDПреподаватель2 IS NULL) OR IDПреподаватель2 = @secondTeacherId)
  AND ISNULL(НомерАудитории, N'') = ISNULL(@room, N'')
ORDER BY НомерНедели;",
                new SqlParameter("@sid", series.ScheduleId),
                new SqlParameter("@day", series.Day),
                new SqlParameter("@pair", series.Pair),
                new SqlParameter("@groupName", series.GroupName),
                new SqlParameter("@subjectId", series.SubjectId),
                new SqlParameter("@lessonType", series.LessonType),
                new SqlParameter("@teacherId", (object)series.TeacherId ?? DBNull.Value),
                new SqlParameter("@secondTeacherId", (object)series.SecondTeacherId ?? DBNull.Value),
                new SqlParameter("@room", string.IsNullOrWhiteSpace(series.Room) ? (object)DBNull.Value : series.Room));

            return dt.AsEnumerable()
                .Select(r => Convert.ToInt32(r["НомерНедели"]))
                .Distinct()
                .OrderBy(x => x)
                .ToList();
        }

        public static int GetSeriesStartPair(ScheduleSeriesData series)
        {
            if (series == null || !IsLabSeries(series) || series.Pair <= 1)
                return series == null ? 0 : series.Pair;

            object value = DataBase.ExecuteScalar(@"
SELECT COUNT(*)
FROM dbo.РасписаниеЗанятий
WHERE IDРасписание = @sid
  AND ДеньНедели = @day
  AND НомерПары = @previousPair
  AND НазваниеГруппы = @groupName
  AND IDПредмет = @subjectId
  AND ТипЗанятия = @lessonType
  AND ((@teacherId IS NULL AND IDПреподаватель IS NULL) OR IDПреподаватель = @teacherId)
  AND ((@secondTeacherId IS NULL AND IDПреподаватель2 IS NULL) OR IDПреподаватель2 = @secondTeacherId)
  AND ISNULL(НомерАудитории, N'') = ISNULL(@room, N'');",
                new SqlParameter("@sid", series.ScheduleId),
                new SqlParameter("@day", series.Day),
                new SqlParameter("@previousPair", series.Pair - 1),
                new SqlParameter("@groupName", series.GroupName),
                new SqlParameter("@subjectId", series.SubjectId),
                new SqlParameter("@lessonType", series.LessonType),
                new SqlParameter("@teacherId", (object)series.TeacherId ?? DBNull.Value),
                new SqlParameter("@secondTeacherId", (object)series.SecondTeacherId ?? DBNull.Value),
                new SqlParameter("@room", string.IsNullOrWhiteSpace(series.Room) ? (object)DBNull.Value : series.Room));

            return Convert.ToInt32(value) > 0 ? series.Pair - 1 : series.Pair;
        }

        public static bool HasSeriesConflict(
            ScheduleSeriesData series,
            int targetDay,
            int targetPair,
            List<int> targetWeeks,
            out string conflictMessage)
        {
            conflictMessage = null;
            var weekSet = new HashSet<int>(targetWeeks);
            int length = GetSeriesLength(series);
            int sourceStartPair = GetSeriesStartPair(series);

            DataTable dt = DataBase.Query(@"
SELECT
    IDРасписание,
    НомерНедели,
    ДеньНедели,
    НомерПары,
    НазваниеГруппы,
    IDПредмет,
    ТипЗанятия,
    IDПреподаватель,
    IDПреподаватель2,
    НомерАудитории
FROM dbo.РасписаниеЗанятий
WHERE IDРасписание = @sid
  AND ДеньНедели = @day
  AND НомерПары BETWEEN @pair AND @lastPair;",
                new SqlParameter("@sid", series.ScheduleId),
                new SqlParameter("@day", targetDay),
                new SqlParameter("@pair", targetPair),
                new SqlParameter("@lastPair", targetPair + length - 1));

            foreach (DataRow row in dt.Rows)
            {
                int week = Convert.ToInt32(row["НомерНедели"]);
                if (!weekSet.Contains(week))
                    continue;

                if (IsSameSeriesBlockRow(series, sourceStartPair, row))
                    continue;

                string rowGroup = Convert.ToString(row["НазваниеГруппы"]);
                int? rowTeacherId = ToNullableInt(row["IDПреподаватель"]);
                int? rowSecondTeacherId = ToNullableInt(row["IDПреподаватель2"]);
                string rowRoom = Convert.ToString(row["НомерАудитории"]);

                if (string.Equals(rowGroup, series.GroupName, StringComparison.OrdinalIgnoreCase))
                {
                    conflictMessage = "Конфликт по группе на неделе " + week + ".";
                    return true;
                }

                if (HasTeacherConflict(rowTeacherId, rowSecondTeacherId, series.TeacherId, series.SecondTeacherId))
                {
                    conflictMessage = "Конфликт по преподавателю на неделе " + week + ".";
                    return true;
                }

                if (string.Equals(rowRoom ?? string.Empty, series.Room ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                {
                    conflictMessage = "Конфликт по аудитории на неделе " + week + ".";
                    return true;
                }
            }

            return false;
        }

        public static void SaveSeriesChanges(ScheduleSeriesData series, int targetDay, int targetPair, List<int> targetWeeks, int? targetSecondTeacherId)
        {
            int length = GetSeriesLength(series);
            int sourceStartPair = GetSeriesStartPair(series);
            Guid targetSeriesId = series.SeriesId ?? Guid.NewGuid();

            DataBase.ExecuteInTransaction((con, tx) =>
            {
                DataBase.ExecuteNonQuery(con, tx, @"
DELETE FROM dbo.РасписаниеЗанятий
WHERE IDРасписание = @sid
  AND ДеньНедели = @day
  AND НомерПары BETWEEN @pair AND @lastPair
  AND НазваниеГруппы = @groupName
  AND IDПредмет = @subjectId
  AND ТипЗанятия = @lessonType
  AND ((@teacherId IS NULL AND IDПреподаватель IS NULL) OR IDПреподаватель = @teacherId)
  AND ((@secondTeacherId IS NULL AND IDПреподаватель2 IS NULL) OR IDПреподаватель2 = @secondTeacherId)
  AND ISNULL(НомерАудитории, N'') = ISNULL(@room, N'');",
                    new SqlParameter("@sid", series.ScheduleId),
                    new SqlParameter("@day", series.Day),
                    new SqlParameter("@pair", sourceStartPair),
                    new SqlParameter("@lastPair", sourceStartPair + length - 1),
                    new SqlParameter("@groupName", series.GroupName),
                    new SqlParameter("@subjectId", series.SubjectId),
                    new SqlParameter("@lessonType", series.LessonType),
                    new SqlParameter("@teacherId", (object)series.TeacherId ?? DBNull.Value),
                    new SqlParameter("@secondTeacherId", (object)series.SecondTeacherId ?? DBNull.Value),
                    new SqlParameter("@room", string.IsNullOrWhiteSpace(series.Room) ? (object)DBNull.Value : series.Room));

                foreach (int week in targetWeeks)
                {
                    for (int offset = 0; offset < length; offset++)
                    {
                        DataBase.ExecuteNonQuery(con, tx, @"
INSERT INTO dbo.РасписаниеЗанятий
    (IDРасписание, НомерНедели, ДеньНедели, НомерПары, НазваниеГруппы, IDПредмет, ТипЗанятия, IDПреподаватель, IDПреподаватель2, НомерАудитории, СвязкаЗанятия, IDУчебныйПлан)
VALUES
    (@sid, @week, @day, @pair, @groupName, @subjectId, @lessonType, @teacherId, @secondTeacherId, @room, @seriesId, @planId);",
                            new SqlParameter("@sid", series.ScheduleId),
                            new SqlParameter("@week", week),
                            new SqlParameter("@day", targetDay),
                            new SqlParameter("@pair", targetPair + offset),
                            new SqlParameter("@groupName", series.GroupName),
                            new SqlParameter("@subjectId", series.SubjectId),
                            new SqlParameter("@lessonType", series.LessonType),
                            new SqlParameter("@teacherId", (object)series.TeacherId ?? DBNull.Value),
                            new SqlParameter("@secondTeacherId", (object)targetSecondTeacherId ?? DBNull.Value),
                            new SqlParameter("@room", string.IsNullOrWhiteSpace(series.Room) ? (object)DBNull.Value : series.Room),
                            new SqlParameter("@seriesId", targetSeriesId),
                            new SqlParameter("@planId", (object)series.CurriculumPlanId ?? DBNull.Value));
                    }
                }
            });
        }

        public static bool IsSameSeriesRow(ScheduleSeriesData series, DataRow row)
        {
            return Convert.ToInt32(row["IDРасписание"]) == series.ScheduleId
                && Convert.ToInt32(row["ДеньНедели"]) == series.Day
                && Convert.ToInt32(row["НомерПары"]) == series.Pair
                && string.Equals(Convert.ToString(row["НазваниеГруппы"]), series.GroupName, StringComparison.OrdinalIgnoreCase)
                && Convert.ToInt32(row["IDПредмет"]) == series.SubjectId
                && string.Equals(Convert.ToString(row["ТипЗанятия"]), series.LessonType, StringComparison.OrdinalIgnoreCase)
                && NullableIntEquals(ToNullableInt(row["IDПреподаватель"]), series.TeacherId)
                && NullableIntEquals(ToNullableInt(row["IDПреподаватель2"]), series.SecondTeacherId)
                && string.Equals(Convert.ToString(row["НомерАудитории"]) ?? string.Empty, series.Room ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        public static int GetSeriesLength(ScheduleSeriesData series)
        {
            return IsLabSeries(series) ? 2 : 1;
        }

        public static bool IsLabSeries(ScheduleSeriesData series)
        {
            return series != null
                && string.Equals(series.LessonType, LabLessonType, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsSameSeriesBlockRow(ScheduleSeriesData series, int sourceStartPair, DataRow row)
        {
            if (series == null || row == null)
                return false;

            int rowPair = Convert.ToInt32(row["НомерПары"]);
            int length = GetSeriesLength(series);

            return Convert.ToInt32(row["IDРасписание"]) == series.ScheduleId
                && Convert.ToInt32(row["ДеньНедели"]) == series.Day
                && rowPair >= sourceStartPair
                && rowPair < sourceStartPair + length
                && string.Equals(Convert.ToString(row["НазваниеГруппы"]), series.GroupName, StringComparison.OrdinalIgnoreCase)
                && Convert.ToInt32(row["IDПредмет"]) == series.SubjectId
                && string.Equals(Convert.ToString(row["ТипЗанятия"]), series.LessonType, StringComparison.OrdinalIgnoreCase)
                && NullableIntEquals(ToNullableInt(row["IDПреподаватель"]), series.TeacherId)
                && NullableIntEquals(ToNullableInt(row["IDПреподаватель2"]), series.SecondTeacherId)
                && string.Equals(Convert.ToString(row["НомерАудитории"]) ?? string.Empty, series.Room ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        private static bool HasTeacherConflict(int? rowTeacherId, int? rowSecondTeacherId, int? teacherId, int? secondTeacherId)
        {
            return HasOneTeacherConflict(rowTeacherId, teacherId, secondTeacherId)
                || HasOneTeacherConflict(rowSecondTeacherId, teacherId, secondTeacherId);
        }

        private static bool HasOneTeacherConflict(int? rowTeacherId, int? teacherId, int? secondTeacherId)
        {
            if (!rowTeacherId.HasValue)
                return false;

            return (teacherId.HasValue && rowTeacherId.Value == teacherId.Value)
                || (secondTeacherId.HasValue && rowTeacherId.Value == secondTeacherId.Value);
        }

        private static int? ToNullableInt(object value)
        {
            if (value == null || value == DBNull.Value)
                return null;

            return Convert.ToInt32(value);
        }

        private static bool NullableIntEquals(int? left, int? right)
        {
            return left.HasValue == right.HasValue
                && (!left.HasValue || left.Value == right.Value);
        }
    }
}
