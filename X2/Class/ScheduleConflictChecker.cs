using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace X2.Class
{
    internal sealed class BusyScheduleSlot
    {
        public BusyScheduleSlot(int week, int day, int pair, int? teacherId, string room)
        {
            Week = week;
            Day = day;
            Pair = pair;
            TeacherId = teacherId;
            Room = room;
        }

        public int Week { get; private set; }
        public int Day { get; private set; }
        public int Pair { get; private set; }
        public int? TeacherId { get; private set; }
        public string Room { get; private set; }
    }

    internal static class ScheduleConflictChecker
    {
        public static bool HasScheduleConflict(
            int scheduleId,
            int week,
            int day,
            int pair,
            string groupName,
            int? teacherId,
            string room,
            int? ignoreEntryId = null,
            int? secondTeacherId = null)
        {
            DataBase.EnsureExcelLikeScheduleSchema();

            object result = DataBase.ExecuteScalar(@"
SELECT COUNT(*)
FROM dbo.РасписаниеЗанятий
WHERE IDРасписание = @sid
  AND НомерНедели = @week
  AND ДеньНедели = @day
  AND НомерПары = @pair
  AND
  (
      НазваниеГруппы = @groupName
      OR (@teacherId IS NOT NULL AND (IDПреподаватель = @teacherId OR IDПреподаватель2 = @teacherId))
      OR (@secondTeacherId IS NOT NULL AND (IDПреподаватель = @secondTeacherId OR IDПреподаватель2 = @secondTeacherId))
      OR (@room <> N'' AND НомерАудитории = @room)
  )
  AND (@ignoreId IS NULL OR IDЗапись <> @ignoreId);",
                new SqlParameter("@sid", scheduleId),
                new SqlParameter("@week", week),
                new SqlParameter("@day", day),
                new SqlParameter("@pair", pair),
                new SqlParameter("@groupName", groupName),
                new SqlParameter("@teacherId", (object)teacherId ?? DBNull.Value),
                new SqlParameter("@secondTeacherId", (object)secondTeacherId ?? DBNull.Value),
                new SqlParameter("@room", string.IsNullOrWhiteSpace(room) ? string.Empty : room),
                new SqlParameter("@ignoreId", (object)ignoreEntryId ?? DBNull.Value));

            return Convert.ToInt32(result) > 0;
        }

        public static List<BusyScheduleSlot> LoadGlobalBusySlotsForSeason(int currentScheduleId, int semester, IEnumerable<int> ignoredScheduleIds = null)
        {
            DataBase.EnsureExcelLikeScheduleSchema();

            var result = new List<BusyScheduleSlot>();
            if (semester < 1 || semester > 10)
                return result;

            var excludedIds = new HashSet<int>();
            if (ignoredScheduleIds != null)
            {
                foreach (int scheduleId in ignoredScheduleIds)
                {
                    if (scheduleId > 0)
                        excludedIds.Add(scheduleId);
                }
            }

            if (currentScheduleId > 0)
                excludedIds.Add(currentScheduleId);

            int seasonParity = Math.Abs(semester % 2);
            string excludedScheduleIds = string.Join("|", excludedIds);
            DataTable dt = DataBase.Query(@"
SELECT
    rz.НомерНедели,
    rz.ДеньНедели,
    rz.НомерПары,
    teachers.IDПреподаватель,
    rz.НомерАудитории
FROM dbo.РасписаниеЗанятий rz
JOIN dbo.Расписания r ON r.IDРасписание = rz.IDРасписание
CROSS APPLY
(
    VALUES (rz.IDПреподаватель), (rz.IDПреподаватель2)
) teachers(IDПреподаватель)
WHERE CHARINDEX(N'|' + CONVERT(nvarchar(20), r.IDРасписание) + N'|', N'|' + @excludedScheduleIds + N'|') = 0
  AND r.Семестр BETWEEN 1 AND 10
  AND (r.Семестр % 2) = @seasonParity
  AND
  (
      teachers.IDПреподаватель IS NOT NULL
      OR NULLIF(LTRIM(RTRIM(ISNULL(rz.НомерАудитории, N''))), N'') IS NOT NULL
  );",
                new SqlParameter("@excludedScheduleIds", excludedScheduleIds),
                new SqlParameter("@seasonParity", seasonParity));

            foreach (DataRow row in dt.Rows)
            {
                result.Add(new BusyScheduleSlot(
                    Convert.ToInt32(row["НомерНедели"]),
                    Convert.ToInt32(row["ДеньНедели"]),
                    Convert.ToInt32(row["НомерПары"]),
                    row["IDПреподаватель"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["IDПреподаватель"]),
                    Convert.ToString(row["НомерАудитории"])));
            }

            return result;
        }

        public static bool HasInMemoryConflict(
            ISet<int> groupTimeBusy,
            ISet<long> teacherBusy,
            ISet<string> roomBusy,
            int week,
            int day,
            int pair,
            int? teacherId,
            string room)
        {
            if (groupTimeBusy != null && groupTimeBusy.Contains(BuildTimeKey(week, day, pair)))
                return true;

            if (teacherId.HasValue && teacherBusy != null && teacherBusy.Contains(BuildTeacherKey(week, day, pair, teacherId.Value)))
                return true;

            if (roomBusy != null && !string.IsNullOrWhiteSpace(room) && roomBusy.Contains(BuildRoomKey(week, day, pair, room)))
                return true;

            return false;
        }

        public static void AddBusySlot(
            ISet<int> groupTimeBusy,
            ISet<long> teacherBusy,
            ISet<string> roomBusy,
            int week,
            int day,
            int pair,
            int? teacherId,
            string room)
        {
            if (groupTimeBusy != null)
                groupTimeBusy.Add(BuildTimeKey(week, day, pair));

            if (teacherId.HasValue && teacherBusy != null)
                teacherBusy.Add(BuildTeacherKey(week, day, pair, teacherId.Value));

            if (roomBusy != null && !string.IsNullOrWhiteSpace(room))
                roomBusy.Add(BuildRoomKey(week, day, pair, room));
        }

        public static void RemoveBusySlot(
            ISet<int> groupTimeBusy,
            ISet<long> teacherBusy,
            ISet<string> roomBusy,
            int week,
            int day,
            int pair,
            int? teacherId,
            string room)
        {
            if (groupTimeBusy != null)
                groupTimeBusy.Remove(BuildTimeKey(week, day, pair));

            if (teacherId.HasValue && teacherBusy != null)
                teacherBusy.Remove(BuildTeacherKey(week, day, pair, teacherId.Value));

            if (roomBusy != null && !string.IsNullOrWhiteSpace(room))
                roomBusy.Remove(BuildRoomKey(week, day, pair, room));
        }

        public static int BuildTimeKey(int week, int day, int pair)
        {
            return (week * 10000) + (day * 100) + pair;
        }

        public static long BuildTeacherKey(int week, int day, int pair, int teacherId)
        {
            return ((long)teacherId * 1000000L) + BuildTimeKey(week, day, pair);
        }

        public static string BuildRoomKey(int week, int day, int pair, string room)
        {
            return BuildTimeKey(week, day, pair) + "|" + (room ?? string.Empty);
        }
    }
}
