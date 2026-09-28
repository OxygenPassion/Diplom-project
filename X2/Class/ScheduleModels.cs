using System;

namespace X2.Class
{
    internal sealed class ScheduleEntryData
    {
        public int? EntryId { get; set; }
        public int ScheduleId { get; set; }
        public int Semester { get; set; }
        public int Week { get; set; }
        public int Day { get; set; }
        public int Pair { get; set; }
        public string GroupName { get; set; }
        public int SubjectId { get; set; }
        public string LessonType { get; set; }
        public int? TeacherId { get; set; }
        public int? SecondTeacherId { get; set; }
        public string Room { get; set; }
        public Guid? SeriesId { get; set; }
        public int? CurriculumPlanId { get; set; }
    }

    internal sealed class ScheduleSeriesData
    {
        public int EntryId { get; set; }
        public int ScheduleId { get; set; }
        public int Day { get; set; }
        public int Pair { get; set; }
        public string GroupName { get; set; }
        public int SubjectId { get; set; }
        public string LessonType { get; set; }
        public int? TeacherId { get; set; }
        public int? SecondTeacherId { get; set; }
        public string Room { get; set; }
        public Guid? SeriesId { get; set; }
        public int? CurriculumPlanId { get; set; }
    }

    internal sealed class ScheduleOperationResult
    {
        private ScheduleOperationResult(bool success, string message, int? entryId)
        {
            Success = success;
            Message = message;
            EntryId = entryId;
        }

        public bool Success { get; private set; }
        public string Message { get; private set; }
        public int? EntryId { get; private set; }

        public static ScheduleOperationResult Ok(int? entryId = null)
        {
            return new ScheduleOperationResult(true, string.Empty, entryId);
        }

        public static ScheduleOperationResult Fail(string message)
        {
            return new ScheduleOperationResult(false, message ?? string.Empty, null);
        }
    }
}
