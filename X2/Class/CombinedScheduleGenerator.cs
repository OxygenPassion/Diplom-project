using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace X2.Class
{
    internal sealed class CombinedScheduleGenerationRequest
    {
        public DataTable PlanTable { get; set; }
        public int ScheduleId { get; set; }
        public string Group { get; set; }
        public int Semester { get; set; }
        public int Weeks { get; set; }
        public int MaxPairsPerDay { get; set; }
        public int PerWeekLimit { get; set; }
        public Dictionary<string, List<string>> RoomsByType { get; set; }
    }

    internal sealed class CombinedScheduleGenerationResult
    {
        public int InsertedPairs { get; set; }
        public int FailedPairs { get; set; }
    }

    internal sealed class CombinedScheduleGenerator
    {
        private const string PhysicalLessonType = "Физкультура";
        private const int UnplacedPenaltyPerPair = 100000;
        private const int WeekShiftPenalty = 18;
        private const int LatePairPenalty = 3;
        private const int GapPenalty = 24;
        private const int HeavyDayPenalty = 6;
        private const int InconsistentSlotPenalty = 5000;
        private const int InconsistentRoomPenalty = 900;
        private const int SameGroupLabDayPenalty = 30000;
        private const int WeekLoadPenalty = 12;
        private const int NewWeekDayPenalty = 10;
        private const int WeeklyOverflowPenalty = 700;
        private const int MaxImprovementPasses = 1;
        private const int MaxRandomizedCandidates = 3;
        private const int MaxAttempts = 5;
        private const int MaxCandidatesPerTask = 160;

        private readonly CombinedScheduleGenerationRequest _request;
        private readonly List<PlanSpec> _plans;
        private readonly List<ExistingEntry> _existingEntries;
        private readonly List<BusyScheduleSlot> _globalBusySlots;
        private readonly List<Slot> _slots;
        private readonly List<Slot> _flowPhysicalSlots;
        private readonly Dictionary<string, FlowLecturePlacement> _flowLecturePlacements;
        private readonly Dictionary<int, int> _teacherDemand;

        public CombinedScheduleGenerator(CombinedScheduleGenerationRequest request)
        {
            if (request == null) throw new ArgumentNullException("request");
            if (request.PlanTable == null) throw new ArgumentNullException("request.PlanTable");
            if (request.RoomsByType == null) throw new ArgumentNullException("request.RoomsByType");

            _request = request;
            _plans = BuildPlans(request.PlanTable, request.RoomsByType, request.Weeks);
            _existingEntries = LoadExistingEntries(request.ScheduleId, request.Group);
            _globalBusySlots = ScheduleConflictChecker.LoadGlobalBusySlotsForSeason(request.ScheduleId, request.Semester);
            _slots = BuildSlots(request.MaxPairsPerDay);
            _flowPhysicalSlots = LoadFlowPhysicalSlots(request);
            _flowLecturePlacements = LoadFlowLecturePlacements(request);
            _teacherDemand = BuildTeacherDemand(_plans);
        }

        public CombinedScheduleGenerationResult Generate()
        {
            AttemptResult bestAttempt = null;
            int occurrenceCount = _plans.Sum(p => p.OccurrenceCount);
            int attempts = occurrenceCount <= 8 ? 2 : MaxAttempts;

            for (int i = 0; i < attempts; i++)
            {
                AttemptResult attempt = RunAttempt(i + 1);
                if (bestAttempt == null || attempt.TotalScore < bestAttempt.TotalScore)
                    bestAttempt = attempt;
            }

            if (bestAttempt == null)
                bestAttempt = new AttemptResult(new List<PlacedOccurrence>(), new List<OccurrenceTask>(), 0);

            Persist(bestAttempt);

            return new CombinedScheduleGenerationResult
            {
                InsertedPairs = bestAttempt.Placed.Sum(p => p.Length),
                FailedPairs = bestAttempt.Unplaced.Sum(t => t.Plan.Length)
            };
        }

        private AttemptResult RunAttempt(int seedOffset)
        {
            var random = new Random(unchecked(Environment.TickCount + (seedOffset * 397)));
            var state = new SearchState(_request, _existingEntries, _globalBusySlots);
            var tasks = BuildTasks(random);

            foreach (OccurrenceTask task in tasks)
            {
                CandidatePlacement candidate = SelectCandidate(task, state, random, true);
                if (candidate == null)
                    state.MarkUnplaced(task);
                else
                    state.AddPlacement(CreatePlacement(task, candidate));
            }

            Improve(state, random);

            return state.CreateAttemptResult();
        }

        private void Improve(SearchState state, Random random)
        {
            for (int pass = 0; pass < MaxImprovementPasses; pass++)
            {
                bool improved = false;

                foreach (PlacedOccurrence placement in Shuffle(state.GetPlacementsSnapshot(), random))
                {
                    int baselineScore = state.CalculatePenalty();
                    state.RemovePlacement(placement);

                    CandidatePlacement bestCandidate = null;
                    int bestScore = baselineScore;

                    foreach (CandidatePlacement candidate in BuildCandidates(placement.Task, state, random))
                    {
                        var candidatePlacement = CreatePlacement(placement.Task, candidate);
                        state.AddPlacement(candidatePlacement);
                        int score = state.CalculatePenalty();
                        state.RemovePlacement(candidatePlacement);

                        if (score < bestScore)
                        {
                            bestScore = score;
                            bestCandidate = candidate;
                        }
                    }

                    if (bestCandidate != null)
                    {
                        state.AddPlacement(CreatePlacement(placement.Task, bestCandidate));
                        improved = true;
                    }
                    else
                    {
                        state.AddPlacement(placement);
                    }
                }

                if (RepairUnplaced(state, random))
                    improved = true;

                if (!improved)
                    break;
            }
        }

        private bool RepairUnplaced(SearchState state, Random random)
        {
            bool placedAny = false;

            foreach (OccurrenceTask task in OrderTasksByDifficulty(state.GetUnplacedSnapshot(), random))
            {
                CandidatePlacement candidate = SelectCandidate(task, state, random, false);
                if (candidate == null)
                    continue;

                state.RemoveUnplaced(task);
                state.AddPlacement(CreatePlacement(task, candidate));
                placedAny = true;
            }

            return placedAny;
        }

        private CandidatePlacement SelectCandidate(OccurrenceTask task, SearchState state, Random random, bool randomized)
        {
            List<CandidatePlacement> candidates = BuildCandidates(task, state, random);
            if (candidates.Count == 0)
                return null;

            var scored = new List<ScoredCandidate>(candidates.Count);
            Slot preferredSlot = state.GetPreferredSlot(task.SeriesKey);
            foreach (CandidatePlacement candidate in candidates)
            {
                int score = EvaluateCandidate(task, candidate, state, preferredSlot);
                scored.Add(new ScoredCandidate(candidate, score, random.Next()));
            }

            scored.Sort((left, right) =>
            {
                int byScore = left.Score.CompareTo(right.Score);
                return byScore != 0 ? byScore : left.RandomKey.CompareTo(right.RandomKey);
            });

            if (!randomized)
                return scored[0].Candidate;

            int take = Math.Min(MaxRandomizedCandidates, scored.Count);
            return scored[random.Next(take)].Candidate;
        }

        private int EvaluateCandidate(OccurrenceTask task, CandidatePlacement candidate, SearchState state, Slot preferredSlot)
        {
            var placement = CreatePlacement(task, candidate);
            return state.CalculateCandidateScore(placement, preferredSlot);
        }

        private List<CandidatePlacement> BuildCandidates(OccurrenceTask task, SearchState state, Random random)
        {
            var candidates = new List<CandidatePlacement>();
            FlowLecturePlacement flowLecturePlacement = GetFlowLecturePlacement(task);
            Slot flowPhysicalSlot = GetFlowPhysicalSlot(task);

            if (flowLecturePlacement != null
                && flowLecturePlacement.Pair + task.Plan.Length - 1 <= _request.MaxPairsPerDay
                && state.IsFeasible(
                    task.Plan,
                    flowLecturePlacement.Week,
                    flowLecturePlacement.Day,
                    flowLecturePlacement.Pair,
                    flowLecturePlacement.Room))
            {
                candidates.Add(new CandidatePlacement(
                    flowLecturePlacement.Week,
                    flowLecturePlacement.Day,
                    flowLecturePlacement.Pair,
                    flowLecturePlacement.Room));
                return candidates;
            }

            Slot preferredSlot = flowLecturePlacement != null
                ? new Slot(flowLecturePlacement.Day, flowLecturePlacement.Pair)
                : flowPhysicalSlot != null
                ? flowPhysicalSlot
                : state.GetPreferredSlot(task.SeriesKey);
            List<Slot> orderedSlots = GetOrderedSlots(preferredSlot, random);

            string preferredRoom = flowLecturePlacement != null
                ? flowLecturePlacement.Room
                : state.GetPreferredRoom(task.SeriesKey);
            List<string> orderedRooms = !string.IsNullOrWhiteSpace(preferredRoom)
                ? new List<string> { preferredRoom }
                : GetOrderedRooms(task.Plan.PossibleRooms, random, preferredRoom);

            IEnumerable<int> candidateWeeks = task.Plan.IsPhysical
                ? new[] { task.TargetWeek }
                : EnumerateWeeksByDistance(task.TargetWeek, _request.Weeks);

            foreach (int week in candidateWeeks)
            {
                foreach (Slot slot in orderedSlots)
                {
                    if (slot.Pair + task.Plan.Length - 1 > _request.MaxPairsPerDay)
                        continue;

                    foreach (string room in orderedRooms)
                    {
                        if (!state.IsFeasible(task.Plan, week, slot.Day, slot.Pair, room))
                            continue;

                        candidates.Add(new CandidatePlacement(week, slot.Day, slot.Pair, room));
                        if (candidates.Count >= MaxCandidatesPerTask)
                            return candidates;
                    }
                }
            }

            return candidates;
        }

        private FlowLecturePlacement GetFlowLecturePlacement(OccurrenceTask task)
        {
            if (task == null
                || task.Plan == null
                || task.Plan.IsPhysical
                || !IsLectureLessonType(task.Plan.LessonType)
                || !task.Plan.TeacherId.HasValue
                || _flowLecturePlacements == null)
            {
                return null;
            }

            string key = BuildFlowLectureKey(task.Plan.SubjectId, task.Plan.TeacherId.Value, task.TargetWeek);
            FlowLecturePlacement placement;
            return _flowLecturePlacements.TryGetValue(key, out placement) ? placement : null;
        }

        private Slot GetFlowPhysicalSlot(OccurrenceTask task)
        {
            if (task == null || task.Plan == null || !task.Plan.IsPhysical || _flowPhysicalSlots == null || _flowPhysicalSlots.Count == 0)
                return null;

            int part = GetTaskPart(task.SequenceIndex, _request.Weeks);
            return part > 0 && part <= _flowPhysicalSlots.Count ? _flowPhysicalSlots[part - 1] : null;
        }

        private List<OccurrenceTask> BuildTasks(Random random)
        {
            var tasks = new List<OccurrenceTask>();

            foreach (PlanSpec plan in _plans)
            {
                for (int sequenceIndex = 1; sequenceIndex <= plan.OccurrenceCount; sequenceIndex++)
                {
                    tasks.Add(new OccurrenceTask(
                        plan,
                        sequenceIndex,
                        GetTargetWeek(plan, sequenceIndex, _request.Weeks),
                        BuildTaskSeriesKey(plan, sequenceIndex, _request.Weeks),
                        plan.TeacherId.HasValue && _teacherDemand.ContainsKey(plan.TeacherId.Value) ? _teacherDemand[plan.TeacherId.Value] : 0,
                        random.Next()));
                }
            }

            return OrderTasksByDifficulty(tasks, random);
        }

        private List<OccurrenceTask> OrderTasksByDifficulty(IEnumerable<OccurrenceTask> tasks, Random random)
        {
            return tasks
                .Select(task => new
                {
                    Task = task,
                    RandomKey = random.Next()
                })
                .OrderBy(x => GetPlanSchedulingPriority(x.Task.Plan))
                .ThenBy(x => GetFlowLecturePlacement(x.Task) == null ? 1 : 0)
                .ThenByDescending(x => x.Task.Plan.OccurrenceCount)
                .ThenByDescending(x => x.Task.TeacherDemand)
                .ThenBy(x => x.Task.Plan.PossibleRooms.Count == 0 ? int.MaxValue : x.Task.Plan.PossibleRooms.Count)
                .ThenBy(x => x.Task.Plan.PlanId)
                .ThenBy(x => x.Task.SequenceIndex)
                .ThenBy(x => x.RandomKey)
                .Select(x => x.Task)
                .ToList();
        }

        private static int GetPlanSchedulingPriority(PlanSpec plan)
        {
            if (plan == null)
                return 100;

            if (plan.IsPhysical)
                return 0;

            if (string.Equals(plan.LessonType, "ЛК", StringComparison.OrdinalIgnoreCase))
                return 1;

            if (plan.Length > 1)
                return 2;

            return 3;
        }

        private void Persist(AttemptResult result)
        {
            DataBase.ExecuteInTransaction((con, tx) =>
            {
                ExecuteNonQuery(con, tx, @"
DELETE FROM dbo.РасписаниеЗанятий
WHERE IDРасписание = @sid
  AND НазваниеГруппы = @group;",
                new SqlParameter("@sid", _request.ScheduleId),
                new SqlParameter("@group", _request.Group));

            foreach (PlanSpec plan in _plans)
            {
                ExecuteNonQuery(
                    con,
                    tx,
                    "DELETE FROM dbo.ПланНедели WHERE IDУчебныйПлан = @id;",
                    new SqlParameter("@id", plan.PlanId));
            }

            foreach (PlacedOccurrence placement in result.Placed
                .OrderBy(p => p.Week)
                .ThenBy(p => p.Day)
                .ThenBy(p => p.Pair)
                .ThenBy(p => p.PlanId))
            {
                InsertScheduleEntry(con, tx, placement, placement.Pair);

                if (placement.Length > 1)
                    InsertScheduleEntry(con, tx, placement, placement.Pair + 1);
            }

            foreach (IGrouping<int, PlacedOccurrence> planPlacements in result.Placed.GroupBy(p => p.PlanId))
            {
                foreach (int week in planPlacements.Select(p => p.Week).Distinct().OrderBy(w => w))
                {
                    ExecuteNonQuery(
                        con,
                        tx,
                        "INSERT INTO dbo.ПланНедели(IDУчебныйПлан, НомерНедели) VALUES(@id, @week);",
                        new SqlParameter("@id", planPlacements.Key),
                        new SqlParameter("@week", week));
                }
            }
            });
        }

        private static void InsertScheduleEntry(SqlConnection con, SqlTransaction tx, PlacedOccurrence placement, int pair)
        {
            ExecuteNonQuery(con, tx, @"
INSERT INTO dbo.РасписаниеЗанятий
(IDРасписание, НомерНедели, ДеньНедели, НомерПары, НазваниеГруппы, IDПредмет, ТипЗанятия, IDПреподаватель, НомерАудитории)
VALUES
(@sid, @week, @day, @pair, @group, @subj, @type, @teach, @room);",
                new SqlParameter("@sid", placement.ScheduleId),
                new SqlParameter("@week", placement.Week),
                new SqlParameter("@day", placement.Day),
                new SqlParameter("@pair", pair),
                new SqlParameter("@group", placement.Group),
                new SqlParameter("@subj", placement.SubjectId),
                new SqlParameter("@type", placement.LessonType),
                new SqlParameter("@teach", (object)placement.TeacherId ?? DBNull.Value),
                new SqlParameter("@room", string.IsNullOrWhiteSpace(placement.Room) ? (object)DBNull.Value : placement.Room));
        }

        private static void ExecuteNonQuery(SqlConnection con, SqlTransaction tx, string sql, params SqlParameter[] parameters)
        {
            using (var cmd = new SqlCommand(sql, con, tx))
            {
                if (parameters != null && parameters.Length > 0)
                    cmd.Parameters.AddRange(parameters);

                cmd.ExecuteNonQuery();
            }
        }

        private List<PlanSpec> BuildPlans(DataTable planTable, Dictionary<string, List<string>> roomsByType, int weeks)
        {
            var plans = new List<PlanSpec>();
            List<string> allRooms = roomsByType
                .SelectMany(pair => pair.Value ?? new List<string>())
                .Where(room => !string.IsNullOrWhiteSpace(room))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (DataRow row in planTable.Rows)
            {
                int planId = Convert.ToInt32(row["IDУчебныйПлан"]);
                int subjectId = Convert.ToInt32(row["IDПредмет"]);
                string lessonType = Convert.ToString(row["ТипЗанятия"]);
                int? teacherId = row["IDПреподаватель"] == DBNull.Value
                    ? (int?)null
                    : Convert.ToInt32(row["IDПреподаватель"]);
                int hours = Convert.ToInt32(row["ЧасыВСеместре"]);
                bool isPhysical = IsPhysicalLessonType(lessonType) || DataBase.IsPhysicalEducationSubject(subjectId);
                if (isPhysical)
                    teacherId = null;

                string requiredRoomType = GetOptionalColumnString(row, "ТребуемыйТипАудитории");
                string requiredRoom = GetOptionalColumnString(row, "ТребуемаяАудитория");
                if (isPhysical)
                {
                    requiredRoomType = string.Empty;
                    requiredRoom = string.Empty;
                }

                bool hasRequiredRoom = !string.IsNullOrWhiteSpace(requiredRoom);
                bool hasRequiredRoomType = !string.IsNullOrWhiteSpace(requiredRoomType);
                bool hasExplicitRoomRequirement = hasRequiredRoom || hasRequiredRoomType;

                string neededRoomType = hasRequiredRoomType ? requiredRoomType : (isPhysical ? null : GetNeededRoomType(lessonType));
                List<string> rooms;

                if (hasRequiredRoom)
                {
                    string matchedRoom = allRooms
                        .FirstOrDefault(room => string.Equals(room, requiredRoom, StringComparison.OrdinalIgnoreCase));
                    rooms = string.IsNullOrWhiteSpace(matchedRoom)
                        ? new List<string>()
                        : new List<string> { matchedRoom };
                }
                else if (isPhysical && !hasRequiredRoomType)
                {
                    rooms = new List<string> { string.Empty };
                }
                else if (!roomsByType.TryGetValue(neededRoomType, out rooms) || rooms == null)
                {
                    rooms = new List<string>();
                }

                if (rooms.Count == 0 && !hasExplicitRoomRequirement)
                    rooms = allRooms;

                plans.Add(new PlanSpec(
                    planId,
                    subjectId,
                    lessonType,
                    teacherId,
                    GetOccurrenceCount(hours, lessonType),
                    string.Equals(lessonType, "ЛБ", StringComparison.OrdinalIgnoreCase) ? 2 : 1,
                    isPhysical,
                    0,
                    rooms.Distinct(StringComparer.OrdinalIgnoreCase).ToList()));
            }

            ApplyTargetWeekOffsets(plans, weeks);

            return plans;
        }

        private static string GetOptionalColumnString(DataRow row, string columnName)
        {
            if (row == null || row.Table == null || !row.Table.Columns.Contains(columnName))
                return string.Empty;

            object value = row[columnName];
            return value == null || value == DBNull.Value ? string.Empty : Convert.ToString(value).Trim();
        }

        private static void ApplyTargetWeekOffsets(List<PlanSpec> plans, int weeks)
        {
            if (plans == null || plans.Count == 0 || weeks <= 1)
                return;

            foreach (IGrouping<int, PlanSpec> group in plans
                .Where(p => p.OccurrenceCount > 0 && p.OccurrenceCount < weeks)
                .GroupBy(p => p.OccurrenceCount))
            {
                List<PlanSpec> groupPlans = group
                    .OrderBy(p => p.PlanId)
                    .ToList();

                if (groupPlans.Count <= 1)
                    continue;

                for (int i = 0; i < groupPlans.Count; i++)
                {
                    int offset = (int)Math.Round((i * weeks) / (double)groupPlans.Count);
                    groupPlans[i].SetTargetWeekOffset(offset);
                }
            }
        }

        private static Dictionary<int, int> BuildTeacherDemand(IEnumerable<PlanSpec> plans)
        {
            return plans
                .Where(p => p.TeacherId.HasValue)
                .GroupBy(p => p.TeacherId.Value)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(p => p.OccurrenceCount * p.Length));
        }

        private static List<ExistingEntry> LoadExistingEntries(int scheduleId, string group)
        {
            DataTable dt = DataBase.Query(@"
;WITH current_group AS (
    SELECT
        Курс,
        ISNULL(NULLIF(LTRIM(RTRIM(Поток)), N''), Направление) AS Поток
    FROM dbo.Группы
    WHERE НазваниеГруппы = @group
)
SELECT
    rz.НомерНедели,
    rz.ДеньНедели,
    rz.НомерПары,
    rz.НазваниеГруппы,
    rz.IDПредмет,
    rz.ТипЗанятия,
    rz.IDПреподаватель,
    rz.НомерАудитории,
    CAST(CASE
        WHEN EXISTS
        (
            SELECT 1
            FROM current_group cg
            JOIN dbo.Группы g ON g.НазваниеГруппы = rz.НазваниеГруппы
            WHERE g.Курс = cg.Курс
              AND ISNULL(NULLIF(LTRIM(RTRIM(g.Поток)), N''), g.Направление) = cg.Поток
        )
        THEN 1 ELSE 0 END AS bit) AS МожноОбъединитьЛекцию
FROM dbo.РасписаниеЗанятий rz
WHERE rz.IDРасписание = @sid
  AND rz.НазваниеГруппы <> @group;",
                new SqlParameter("@sid", scheduleId),
                new SqlParameter("@group", group));

            var result = new List<ExistingEntry>();
            foreach (DataRow row in dt.Rows)
            {
                result.Add(new ExistingEntry(
                    Convert.ToInt32(row["НомерНедели"]),
                    Convert.ToInt32(row["ДеньНедели"]),
                    Convert.ToInt32(row["НомерПары"]),
                    Convert.ToString(row["НазваниеГруппы"]),
                    Convert.ToInt32(row["IDПредмет"]),
                    Convert.ToString(row["ТипЗанятия"]),
                    row["IDПреподаватель"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["IDПреподаватель"]),
                    Convert.ToString(row["НомерАудитории"]),
                    row["МожноОбъединитьЛекцию"] != DBNull.Value && Convert.ToBoolean(row["МожноОбъединитьЛекцию"])));
            }

            return result;
        }

        private static List<Slot> LoadFlowPhysicalSlots(CombinedScheduleGenerationRequest request)
        {
            if (request == null || request.Semester <= 0 || string.IsNullOrWhiteSpace(request.Group))
                return new List<Slot>();

            DataTable dt = DataBase.Query(@"
;WITH current_group AS (
    SELECT
        Курс,
        ISNULL(NULLIF(LTRIM(RTRIM(Поток)), N''), Направление) AS Поток
    FROM dbo.Группы
    WHERE НазваниеГруппы = @group
),
flow_groups AS (
    SELECT g.НазваниеГруппы
    FROM dbo.Группы g
    CROSS JOIN current_group cg
    WHERE g.НазваниеГруппы <> @group
      AND g.Курс = cg.Курс
      AND ISNULL(NULLIF(LTRIM(RTRIM(g.Поток)), N''), g.Направление) = cg.Поток
),
latest AS (
    SELECT НазваниеГруппы, IDРасписание
    FROM (
        SELECT
            rz.НазваниеГруппы,
            r.IDРасписание,
            ROW_NUMBER() OVER (
                PARTITION BY rz.НазваниеГруппы
                ORDER BY r.ДатаСоздания DESC, r.IDРасписание DESC
            ) AS rn
        FROM dbo.РасписаниеЗанятий rz
        JOIN dbo.Расписания r ON r.IDРасписание = rz.IDРасписание
        JOIN flow_groups fg ON fg.НазваниеГруппы = rz.НазваниеГруппы
        WHERE r.Семестр = @semester
        GROUP BY rz.НазваниеГруппы, r.IDРасписание, r.ДатаСоздания
    ) x
    WHERE rn = 1
)
SELECT
    rz.ДеньНедели,
    rz.НомерПары,
    COUNT(*) AS Количество
FROM dbo.РасписаниеЗанятий rz
JOIN latest ls
    ON ls.IDРасписание = rz.IDРасписание
   AND ls.НазваниеГруппы = rz.НазваниеГруппы
JOIN dbo.Предметы p ON p.IDПредмет = rz.IDПредмет
WHERE rz.ТипЗанятия = N'Физкультура'
   OR p.Название IN (N'Физкультура', N'Элективные курсы по физической культуре и спорту')
GROUP BY rz.ДеньНедели, rz.НомерПары
ORDER BY Количество DESC, rz.ДеньНедели, rz.НомерПары;",
                new SqlParameter("@group", request.Group),
                new SqlParameter("@semester", request.Semester));

            if (dt.Rows.Count == 0)
                return new List<Slot>();

            return dt.AsEnumerable()
                .Select(row => new Slot(Convert.ToInt32(row[0]), Convert.ToInt32(row[1])))
                .ToList();
        }

        private static Dictionary<string, FlowLecturePlacement> LoadFlowLecturePlacements(CombinedScheduleGenerationRequest request)
        {
            var result = new Dictionary<string, FlowLecturePlacement>(StringComparer.Ordinal);
            if (request == null || request.Semester <= 0 || string.IsNullOrWhiteSpace(request.Group))
                return result;

            DataTable dt = DataBase.Query(@"
;WITH current_group AS (
    SELECT
        Курс,
        ISNULL(NULLIF(LTRIM(RTRIM(Поток)), N''), Направление) AS Поток
    FROM dbo.Группы
    WHERE НазваниеГруппы = @group
),
flow_groups AS (
    SELECT g.НазваниеГруппы
    FROM dbo.Группы g
    CROSS JOIN current_group cg
    WHERE g.НазваниеГруппы <> @group
      AND g.Курс = cg.Курс
      AND ISNULL(NULLIF(LTRIM(RTRIM(g.Поток)), N''), g.Направление) = cg.Поток
),
latest AS (
    SELECT НазваниеГруппы, IDРасписание
    FROM (
        SELECT
            rz.НазваниеГруппы,
            r.IDРасписание,
            ROW_NUMBER() OVER (
                PARTITION BY rz.НазваниеГруппы
                ORDER BY r.ДатаСоздания DESC, r.IDРасписание DESC
            ) AS rn
        FROM dbo.РасписаниеЗанятий rz
        JOIN dbo.Расписания r ON r.IDРасписание = rz.IDРасписание
        JOIN flow_groups fg ON fg.НазваниеГруппы = rz.НазваниеГруппы
        WHERE r.Семестр = @semester
        GROUP BY rz.НазваниеГруппы, r.IDРасписание, r.ДатаСоздания
    ) x
    WHERE rn = 1
)
SELECT
    rz.IDПредмет,
    rz.IDПреподаватель,
    rz.НомерНедели,
    rz.ДеньНедели,
    rz.НомерПары,
    rz.НомерАудитории,
    COUNT(*) AS Количество
FROM dbo.РасписаниеЗанятий rz
JOIN latest ls
    ON ls.IDРасписание = rz.IDРасписание
   AND ls.НазваниеГруппы = rz.НазваниеГруппы
WHERE rz.ТипЗанятия = N'ЛК'
  AND rz.IDПреподаватель IS NOT NULL
  AND rz.НомерАудитории IS NOT NULL
  AND LTRIM(RTRIM(rz.НомерАудитории)) <> N''
GROUP BY
    rz.IDПредмет,
    rz.IDПреподаватель,
    rz.НомерНедели,
    rz.ДеньНедели,
    rz.НомерПары,
    rz.НомерАудитории
ORDER BY
    rz.IDПредмет,
    rz.IDПреподаватель,
    rz.НомерНедели,
    Количество DESC,
    rz.ДеньНедели,
    rz.НомерПары,
    rz.НомерАудитории;",
                new SqlParameter("@group", request.Group),
                new SqlParameter("@semester", request.Semester));

            foreach (DataRow row in dt.Rows)
            {
                int subjectId = Convert.ToInt32(row["IDПредмет"]);
                int teacherId = Convert.ToInt32(row["IDПреподаватель"]);
                int week = Convert.ToInt32(row["НомерНедели"]);
                string key = BuildFlowLectureKey(subjectId, teacherId, week);
                if (result.ContainsKey(key))
                    continue;

                result[key] = new FlowLecturePlacement(
                    subjectId,
                    teacherId,
                    week,
                    Convert.ToInt32(row["ДеньНедели"]),
                    Convert.ToInt32(row["НомерПары"]),
                    Convert.ToString(row["НомерАудитории"]));
            }

            return result;
        }

        private static List<Slot> BuildSlots(int maxPairsPerDay)
        {
            var slots = new List<Slot>();
            for (int day = 1; day <= 5; day++)
            {
                for (int pair = 1; pair <= maxPairsPerDay; pair++)
                    slots.Add(new Slot(day, pair));
            }

            return slots;
        }

        private List<Slot> GetOrderedSlots(Slot preferredSlot, Random random)
        {
            var ordered = new List<OrderedSlot>(_slots.Count);

            foreach (Slot slot in _slots)
            {
                int priority;
                if (preferredSlot == null)
                {
                    priority = 0;
                }
                else if (slot.Day == preferredSlot.Day && slot.Pair == preferredSlot.Pair)
                {
                    priority = -1000;
                }
                else
                {
                    priority =
                        (Math.Abs(slot.Day - preferredSlot.Day) * 10) +
                        (Math.Abs(slot.Pair - preferredSlot.Pair) * 4);
                }

                ordered.Add(new OrderedSlot(slot, priority, random.Next()));
            }

            return ordered
                .OrderBy(s => s.Priority)
                .ThenBy(s => s.RandomKey)
                .Select(s => s.Slot)
                .ToList();
        }

        private static List<string> GetOrderedRooms(List<string> rooms, Random random, string preferredRoom)
        {
            if (rooms == null || rooms.Count == 0)
                return new List<string>();

            return rooms
                .Select(room => new
                {
                    Room = room,
                    Priority = !string.IsNullOrWhiteSpace(preferredRoom)
                        && string.Equals(room, preferredRoom, StringComparison.OrdinalIgnoreCase)
                        ? 0
                        : 1,
                    RandomKey = random.Next()
                })
                .OrderBy(x => x.Priority)
                .ThenBy(x => x.RandomKey)
                .Select(x => x.Room)
                .ToList();
        }

        private static IEnumerable<int> EnumerateWeeksByDistance(int targetWeek, int weeks)
        {
            yield return targetWeek;

            for (int delta = 1; delta < weeks; delta++)
            {
                int left = targetWeek - delta;
                if (left >= 1)
                    yield return left;

                int right = targetWeek + delta;
                if (right <= weeks)
                    yield return right;
            }
        }

        private static int GetTargetWeek(PlanSpec plan, int sequenceIndex, int weeks)
        {
            if (plan.IsPhysical || string.Equals(plan.LessonType, "ЛК", StringComparison.OrdinalIgnoreCase))
                return WrapWeek(sequenceIndex, weeks);

            int baseWeek = 1 + (int)Math.Floor(((sequenceIndex - 1) * (double)weeks) / plan.OccurrenceCount);
            return WrapWeek(baseWeek + plan.TargetWeekOffset, weeks);
        }

        private static string BuildTaskSeriesKey(PlanSpec plan, int sequenceIndex, int weeks)
        {
            if (plan == null)
                return string.Empty;

            if (plan.IsPhysical || IsLectureLessonType(plan.LessonType))
            {
                int part = GetTaskPart(sequenceIndex, weeks);
                return plan.PlanId + "|part:" + part;
            }

            return plan.PlanId.ToString();
        }

        private static int GetTaskPart(int sequenceIndex, int weeks)
        {
            return ((sequenceIndex - 1) / Math.Max(1, weeks)) + 1;
        }

        private static int WrapWeek(int week, int weeks)
        {
            return ((week - 1) % weeks) + 1;
        }

        private static int GetOccurrenceCount(int hours, string lessonType)
        {
            return string.Equals(lessonType, "ЛБ", StringComparison.OrdinalIgnoreCase)
                ? Math.Max(1, hours / 4)
                : Math.Max(1, (int)Math.Ceiling(hours / 2.0));
        }

        private static string GetNeededRoomType(string lessonType)
        {
            switch (lessonType)
            {
                case "ЛК":
                    return "Лекции";
                case "У":
                    return "Упражнения";
                case "ЛБ":
                    return "Компьютерный класс";
                default:
                    return "Упражнения";
            }
        }

        private static bool IsPhysicalLessonType(string lessonType)
        {
            return string.Equals(lessonType, PhysicalLessonType, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsLectureLessonType(string lessonType)
        {
            return string.Equals(lessonType, "ЛК", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsLabLessonType(string lessonType)
        {
            return string.Equals(lessonType, "ЛБ", StringComparison.OrdinalIgnoreCase);
        }

        private static string BuildFlowLectureKey(int subjectId, int teacherId, int week)
        {
            return subjectId + "|" + teacherId + "|" + week;
        }

        private static List<T> Shuffle<T>(IEnumerable<T> items, Random random)
        {
            return items
                .Select(item => new { Item = item, RandomKey = random.Next() })
                .OrderBy(x => x.RandomKey)
                .Select(x => x.Item)
                .ToList();
        }

        private PlacedOccurrence CreatePlacement(OccurrenceTask task, CandidatePlacement candidate)
        {
            return new PlacedOccurrence(
                _request.ScheduleId,
                _request.Group,
                task.Plan.PlanId,
                task.Plan.SubjectId,
                task.Plan.LessonType,
                task.Plan.TeacherId,
                task.Plan.Length,
                task.TargetWeek,
                candidate.Week,
                candidate.Day,
                candidate.Pair,
                candidate.Room,
                task);
        }

        private sealed class SearchState
        {
            private readonly CombinedScheduleGenerationRequest _request;
            private readonly HashSet<int> _groupTimeBusy;
            private readonly HashSet<long> _teacherBusy;
            private readonly HashSet<string> _roomBusy;
            private readonly List<ExistingEntry> _existingEntries;
            private readonly List<PlacedOccurrence> _placed;
            private readonly List<OccurrenceTask> _unplaced;
            private readonly Dictionary<string, List<PlacedOccurrence>> _placedBySeries;

            public SearchState(
                CombinedScheduleGenerationRequest request,
                IEnumerable<ExistingEntry> existingEntries,
                IEnumerable<BusyScheduleSlot> globalBusySlots)
            {
                _request = request;
                _groupTimeBusy = new HashSet<int>();
                _teacherBusy = new HashSet<long>();
                _roomBusy = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                _existingEntries = existingEntries == null ? new List<ExistingEntry>() : existingEntries.ToList();
                _placed = new List<PlacedOccurrence>();
                _unplaced = new List<OccurrenceTask>();
                _placedBySeries = new Dictionary<string, List<PlacedOccurrence>>(StringComparer.OrdinalIgnoreCase);

                if (globalBusySlots != null)
                {
                    foreach (BusyScheduleSlot slot in globalBusySlots)
                        AddGlobalBusy(slot);
                }
            }

            private void AddGlobalBusy(BusyScheduleSlot slot)
            {
                if (slot == null)
                    return;

                ScheduleConflictChecker.AddBusySlot(
                    null,
                    _teacherBusy,
                    _roomBusy,
                    slot.Week,
                    slot.Day,
                    slot.Pair,
                    slot.TeacherId,
                    slot.Room);
            }

            public bool IsFeasible(PlanSpec plan, int week, int day, int startPair, string room)
            {
                if (plan.IsPhysical && HasPhysicalOnSameWeekDay(week, day))
                    return false;

                for (int offset = 0; offset < plan.Length; offset++)
                {
                    int pair = startPair + offset;
                    if (pair > _request.MaxPairsPerDay)
                        return false;

                    if (_groupTimeBusy.Contains(ScheduleConflictChecker.BuildTimeKey(week, day, pair)))
                        return false;

                    if (ScheduleConflictChecker.HasInMemoryConflict(null, _teacherBusy, _roomBusy, week, day, pair, plan.TeacherId, room))
                        return false;

                    if (HasBlockingExistingConflict(plan, week, day, pair, room))
                        return false;
                }

                return true;
            }

            private bool HasPhysicalOnSameWeekDay(int week, int day)
            {
                return _placed.Any(placement =>
                    placement.Task != null
                    && placement.Task.Plan != null
                    && placement.Task.Plan.IsPhysical
                    && placement.Week == week
                    && placement.Day == day);
            }

            private bool HasBlockingExistingConflict(PlanSpec plan, int week, int day, int pair, string room)
            {
                foreach (ExistingEntry entry in _existingEntries)
                {
                    if (entry.Week != week || entry.Day != day || entry.Pair != pair)
                        continue;

                    bool teacherConflict = plan.TeacherId.HasValue
                        && entry.TeacherId.HasValue
                        && entry.TeacherId.Value == plan.TeacherId.Value;
                    bool roomConflict = !string.IsNullOrWhiteSpace(room)
                        && !string.IsNullOrWhiteSpace(entry.Room)
                        && string.Equals(entry.Room, room, StringComparison.OrdinalIgnoreCase);

                    if (!teacherConflict && !roomConflict)
                        continue;

                    if (CanShareLecture(plan, entry, room))
                        continue;

                    return true;
                }

                return false;
            }

            private static bool CanShareLecture(PlanSpec plan, ExistingEntry entry, string room)
            {
                return plan != null
                    && entry != null
                    && entry.CanShareLecture
                    && IsLectureLessonType(plan.LessonType)
                    && IsLectureLessonType(entry.LessonType)
                    && plan.TeacherId.HasValue
                    && entry.TeacherId.HasValue
                    && plan.TeacherId.Value == entry.TeacherId.Value
                    && plan.SubjectId == entry.SubjectId
                    && !string.IsNullOrWhiteSpace(room)
                    && !string.IsNullOrWhiteSpace(entry.Room)
                    && string.Equals(entry.Room, room, StringComparison.OrdinalIgnoreCase);
            }

            public void AddPlacement(PlacedOccurrence placement)
            {
                _placed.Add(placement);

                List<PlacedOccurrence> items;
                if (!_placedBySeries.TryGetValue(placement.SeriesKey, out items))
                {
                    items = new List<PlacedOccurrence>();
                    _placedBySeries[placement.SeriesKey] = items;
                }

                items.Add(placement);

                for (int offset = 0; offset < placement.Length; offset++)
                {
                    int pair = placement.Pair + offset;
                    ScheduleConflictChecker.AddBusySlot(_groupTimeBusy, _teacherBusy, _roomBusy, placement.Week, placement.Day, pair, placement.TeacherId, placement.Room);
                }
            }

            public void RemovePlacement(PlacedOccurrence placement)
            {
                _placed.Remove(placement);

                List<PlacedOccurrence> items;
                if (_placedBySeries.TryGetValue(placement.SeriesKey, out items))
                {
                    items.Remove(placement);
                    if (items.Count == 0)
                        _placedBySeries.Remove(placement.SeriesKey);
                }

                for (int offset = 0; offset < placement.Length; offset++)
                {
                    int pair = placement.Pair + offset;
                    ScheduleConflictChecker.RemoveBusySlot(_groupTimeBusy, _teacherBusy, _roomBusy, placement.Week, placement.Day, pair, placement.TeacherId, placement.Room);
                }
            }

            public void MarkUnplaced(OccurrenceTask task)
            {
                if (!_unplaced.Contains(task))
                    _unplaced.Add(task);
            }

            public void RemoveUnplaced(OccurrenceTask task)
            {
                _unplaced.Remove(task);
            }

            public Slot GetPreferredSlot(string seriesKey)
            {
                List<PlacedOccurrence> items;
                if (!_placedBySeries.TryGetValue(seriesKey, out items) || items.Count == 0)
                    return null;

                return items
                    .GroupBy(p => ScheduleConflictChecker.BuildTimeKey(0, p.Day, p.Pair))
                    .OrderByDescending(g => g.Count())
                    .ThenBy(g => g.First().Day)
                    .ThenBy(g => g.First().Pair)
                    .Select(g => new Slot(g.First().Day, g.First().Pair))
                    .FirstOrDefault();
            }

            public string GetPreferredRoom(string seriesKey)
            {
                List<PlacedOccurrence> items;
                if (!_placedBySeries.TryGetValue(seriesKey, out items) || items.Count == 0)
                    return string.Empty;

                return items
                    .Where(p => !string.IsNullOrWhiteSpace(p.Room))
                    .GroupBy(p => p.Room, StringComparer.OrdinalIgnoreCase)
                    .OrderByDescending(g => g.Count())
                    .ThenBy(g => g.Key)
                    .Select(g => g.Key)
                    .FirstOrDefault() ?? string.Empty;
            }

            public List<PlacedOccurrence> GetPlacementsSnapshot()
            {
                return _placed.ToList();
            }

            public List<OccurrenceTask> GetUnplacedSnapshot()
            {
                return _unplaced.ToList();
            }

            public AttemptResult CreateAttemptResult()
            {
                return new AttemptResult(
                    _placed.OrderBy(p => p.Week).ThenBy(p => p.Day).ThenBy(p => p.Pair).ThenBy(p => p.PlanId).ToList(),
                    _unplaced.ToList(),
                    CalculatePenalty());
            }

            public int CalculateCandidateScore(PlacedOccurrence placement, Slot preferredSlot)
            {
                int penalty = Math.Abs(placement.Week - placement.TargetWeek) * WeekShiftPenalty * placement.Length;

                if (IsLabLessonType(placement.LessonType))
                {
                    int labSeriesOnDay = _placed
                        .Where(p => p.Day == placement.Day
                            && IsLabLessonType(p.LessonType)
                            && p.PlanId != placement.PlanId)
                        .Select(p => p.PlanId)
                        .Distinct()
                        .Count();

                    penalty += labSeriesOnDay * labSeriesOnDay * SameGroupLabDayPenalty;
                }

                for (int offset = 0; offset < placement.Length; offset++)
                {
                    int pair = placement.Pair + offset;
                    penalty += Math.Max(0, pair - 2) * LatePairPenalty;
                }

                var occupiedPairs = new HashSet<int>();
                foreach (PlacedOccurrence existing in _placed)
                {
                    if (existing.Week != placement.Week || existing.Day != placement.Day)
                        continue;

                    for (int offset = 0; offset < existing.Length; offset++)
                        occupiedPairs.Add(existing.Pair + offset);
                }

                for (int offset = 0; offset < placement.Length; offset++)
                    occupiedPairs.Add(placement.Pair + offset);

                int heavyDayThreshold = Math.Max(3, Math.Min(4, _request.MaxPairsPerDay));
                if (occupiedPairs.Count > heavyDayThreshold)
                {
                    int overload = occupiedPairs.Count - heavyDayThreshold;
                    penalty += overload * overload * HeavyDayPenalty;
                }

                List<int> orderedPairs = occupiedPairs.OrderBy(p => p).ToList();
                for (int i = 1; i < orderedPairs.Count; i++)
                {
                    int gap = orderedPairs[i] - orderedPairs[i - 1] - 1;
                    if (gap > 0)
                        penalty += gap * GapPenalty;
                }

                int weekLoad = _placed
                    .Where(p => p.Week == placement.Week)
                    .Sum(p => p.Length);
                penalty += weekLoad * WeekLoadPenalty * placement.Length;

                bool usesDayThisWeek = _placed.Any(p => p.Week == placement.Week && p.Day == placement.Day);
                if (!usesDayThisWeek)
                    penalty += NewWeekDayPenalty;

                if (preferredSlot != null && (placement.Day != preferredSlot.Day || placement.Pair != preferredSlot.Pair))
                {
                    penalty += InconsistentSlotPenalty;
                    penalty += Math.Abs(placement.Day - preferredSlot.Day) * 3;
                    penalty += Math.Abs(placement.Pair - preferredSlot.Pair) * 2;
                }

                string preferredRoom = GetPreferredRoom(placement.SeriesKey);
                if (!string.IsNullOrWhiteSpace(preferredRoom)
                    && !string.Equals(placement.Room, preferredRoom, StringComparison.OrdinalIgnoreCase))
                {
                    penalty += InconsistentRoomPenalty;
                }

                if (_request.PerWeekLimit > 0)
                {
                    int countThisWeek = 1;
                    foreach (PlacedOccurrence existing in _placed)
                    {
                        if (existing.PlanId == placement.PlanId && existing.Week == placement.Week)
                            countThisWeek++;
                    }

                    int overflow = countThisWeek - _request.PerWeekLimit;
                    if (overflow > 0)
                        penalty += overflow * overflow * WeeklyOverflowPenalty;
                }

                return penalty;
            }

            public int CalculatePenalty()
            {
                int penalty = _unplaced.Sum(t => t.Plan.Length * UnplacedPenaltyPerPair);

                foreach (PlacedOccurrence placement in _placed)
                {
                    penalty += Math.Abs(placement.Week - placement.TargetWeek) * WeekShiftPenalty * placement.Length;

                    for (int offset = 0; offset < placement.Length; offset++)
                    {
                        int pair = placement.Pair + offset;
                        penalty += Math.Max(0, pair - 2) * LatePairPenalty;
                    }
                }

                foreach (IGrouping<int, PlacedOccurrence> weekGroup in _placed.GroupBy(p => p.Week))
                {
                    int weekLoad = weekGroup.Sum(p => p.Length);
                    penalty += weekLoad * weekLoad * WeekLoadPenalty;

                    foreach (IGrouping<int, PlacedOccurrence> dayGroup in weekGroup.GroupBy(p => p.Day))
                    {
                        List<int> occupiedPairs = dayGroup
                            .SelectMany(p => Enumerable.Range(p.Pair, p.Length))
                            .Distinct()
                            .OrderBy(p => p)
                            .ToList();

                        int heavyDayThreshold = Math.Max(3, Math.Min(4, _request.MaxPairsPerDay));
                        if (occupiedPairs.Count > heavyDayThreshold)
                        {
                            int overload = occupiedPairs.Count - heavyDayThreshold;
                            penalty += overload * overload * HeavyDayPenalty;
                        }

                        for (int i = 1; i < occupiedPairs.Count; i++)
                        {
                            int gap = occupiedPairs[i] - occupiedPairs[i - 1] - 1;
                            if (gap > 0)
                                penalty += gap * GapPenalty;
                        }
                    }
                }

                foreach (IGrouping<string, PlacedOccurrence> seriesGroup in _placed.GroupBy(p => p.SeriesKey, StringComparer.OrdinalIgnoreCase))
                {
                    PlacedOccurrence anchor = seriesGroup
                        .GroupBy(p => ScheduleConflictChecker.BuildTimeKey(0, p.Day, p.Pair))
                        .OrderByDescending(g => g.Count())
                        .ThenBy(g => g.First().Day)
                        .ThenBy(g => g.First().Pair)
                        .Select(g => g.First())
                        .FirstOrDefault();

                    if (anchor != null)
                    {
                        foreach (PlacedOccurrence placement in seriesGroup)
                        {
                            if (placement.Day == anchor.Day && placement.Pair == anchor.Pair)
                                continue;

                            penalty += InconsistentSlotPenalty;
                            penalty += Math.Abs(placement.Day - anchor.Day) * 3;
                            penalty += Math.Abs(placement.Pair - anchor.Pair) * 2;
                        }
                    }

                    string anchorRoom = seriesGroup
                        .Where(p => !string.IsNullOrWhiteSpace(p.Room))
                        .GroupBy(p => p.Room, StringComparer.OrdinalIgnoreCase)
                        .OrderByDescending(g => g.Count())
                        .ThenBy(g => g.Key)
                        .Select(g => g.Key)
                        .FirstOrDefault();

                    if (!string.IsNullOrWhiteSpace(anchorRoom))
                    {
                        foreach (PlacedOccurrence placement in seriesGroup)
                        {
                            if (!string.Equals(placement.Room, anchorRoom, StringComparison.OrdinalIgnoreCase))
                                penalty += InconsistentRoomPenalty;
                        }
                    }
                }

                if (_request.PerWeekLimit > 0)
                {
                    foreach (IGrouping<int, PlacedOccurrence> planGroup in _placed.GroupBy(p => p.PlanId))
                    {
                        foreach (IGrouping<int, PlacedOccurrence> planWeekGroup in planGroup.GroupBy(p => p.Week))
                        {
                            int overflow = planWeekGroup.Count() - _request.PerWeekLimit;
                            if (overflow > 0)
                                penalty += overflow * overflow * WeeklyOverflowPenalty;
                        }
                    }
                }

                return penalty;
            }

        }

        private sealed class PlanSpec
        {
            public PlanSpec(int planId, int subjectId, string lessonType, int? teacherId, int occurrenceCount, int length, bool isPhysical, int targetWeekOffset, List<string> possibleRooms)
            {
                PlanId = planId;
                SubjectId = subjectId;
                LessonType = lessonType;
                TeacherId = teacherId;
                OccurrenceCount = occurrenceCount;
                Length = length;
                IsPhysical = isPhysical;
                TargetWeekOffset = targetWeekOffset;
                PossibleRooms = possibleRooms ?? new List<string>();
            }

            public int PlanId { get; private set; }
            public int SubjectId { get; private set; }
            public string LessonType { get; private set; }
            public int? TeacherId { get; private set; }
            public int OccurrenceCount { get; private set; }
            public int Length { get; private set; }
            public bool IsPhysical { get; private set; }
            public int TargetWeekOffset { get; private set; }
            public List<string> PossibleRooms { get; private set; }

            public void SetTargetWeekOffset(int targetWeekOffset)
            {
                TargetWeekOffset = Math.Max(0, targetWeekOffset);
            }
        }

        private sealed class OccurrenceTask
        {
            public OccurrenceTask(PlanSpec plan, int sequenceIndex, int targetWeek, string seriesKey, int teacherDemand, int randomKey)
            {
                Plan = plan;
                SequenceIndex = sequenceIndex;
                TargetWeek = targetWeek;
                SeriesKey = seriesKey;
                TeacherDemand = teacherDemand;
                RandomKey = randomKey;
            }

            public PlanSpec Plan { get; private set; }
            public int SequenceIndex { get; private set; }
            public int TargetWeek { get; private set; }
            public string SeriesKey { get; private set; }
            public int TeacherDemand { get; private set; }
            public int RandomKey { get; private set; }
        }

        private sealed class PlacedOccurrence
        {
            public PlacedOccurrence(int scheduleId, string group, int planId, int subjectId, string lessonType, int? teacherId, int length, int targetWeek, int week, int day, int pair, string room, OccurrenceTask task)
            {
                ScheduleId = scheduleId;
                Group = group;
                PlanId = planId;
                SubjectId = subjectId;
                LessonType = lessonType;
                TeacherId = teacherId;
                Length = length;
                TargetWeek = targetWeek;
                Week = week;
                Day = day;
                Pair = pair;
                Room = room;
                Task = task;
            }

            public int ScheduleId { get; private set; }
            public string Group { get; private set; }
            public int PlanId { get; private set; }
            public string SeriesKey { get { return Task != null ? Task.SeriesKey : PlanId.ToString(); } }
            public int SubjectId { get; private set; }
            public string LessonType { get; private set; }
            public int? TeacherId { get; private set; }
            public int Length { get; private set; }
            public int TargetWeek { get; private set; }
            public int Week { get; private set; }
            public int Day { get; private set; }
            public int Pair { get; private set; }
            public string Room { get; private set; }
            public OccurrenceTask Task { get; private set; }
        }

        private sealed class CandidatePlacement
        {
            public CandidatePlacement(int week, int day, int pair, string room)
            {
                Week = week;
                Day = day;
                Pair = pair;
                Room = room;
            }

            public int Week { get; private set; }
            public int Day { get; private set; }
            public int Pair { get; private set; }
            public string Room { get; private set; }
        }

        private sealed class FlowLecturePlacement
        {
            public FlowLecturePlacement(int subjectId, int teacherId, int week, int day, int pair, string room)
            {
                SubjectId = subjectId;
                TeacherId = teacherId;
                Week = week;
                Day = day;
                Pair = pair;
                Room = room;
            }

            public int SubjectId { get; private set; }
            public int TeacherId { get; private set; }
            public int Week { get; private set; }
            public int Day { get; private set; }
            public int Pair { get; private set; }
            public string Room { get; private set; }
        }

        private sealed class Slot
        {
            public Slot(int day, int pair)
            {
                Day = day;
                Pair = pair;
            }

            public int Day { get; private set; }
            public int Pair { get; private set; }
        }

        private sealed class OrderedSlot
        {
            public OrderedSlot(Slot slot, int priority, int randomKey)
            {
                Slot = slot;
                Priority = priority;
                RandomKey = randomKey;
            }

            public Slot Slot { get; private set; }
            public int Priority { get; private set; }
            public int RandomKey { get; private set; }
        }

        private sealed class ExistingEntry
        {
            public ExistingEntry(int week, int day, int pair, string groupName, int subjectId, string lessonType, int? teacherId, string room, bool canShareLecture)
            {
                Week = week;
                Day = day;
                Pair = pair;
                GroupName = groupName;
                SubjectId = subjectId;
                LessonType = lessonType;
                TeacherId = teacherId;
                Room = room;
                CanShareLecture = canShareLecture;
            }

            public int Week { get; private set; }
            public int Day { get; private set; }
            public int Pair { get; private set; }
            public string GroupName { get; private set; }
            public int SubjectId { get; private set; }
            public string LessonType { get; private set; }
            public int? TeacherId { get; private set; }
            public string Room { get; private set; }
            public bool CanShareLecture { get; private set; }
        }

        private sealed class ScoredCandidate
        {
            public ScoredCandidate(CandidatePlacement candidate, int score, int randomKey)
            {
                Candidate = candidate;
                Score = score;
                RandomKey = randomKey;
            }

            public CandidatePlacement Candidate { get; private set; }
            public int Score { get; private set; }
            public int RandomKey { get; private set; }
        }

        private sealed class AttemptResult
        {
            public AttemptResult(List<PlacedOccurrence> placed, List<OccurrenceTask> unplaced, int totalScore)
            {
                Placed = placed ?? new List<PlacedOccurrence>();
                Unplaced = unplaced ?? new List<OccurrenceTask>();
                TotalScore = totalScore;
            }

            public List<PlacedOccurrence> Placed { get; private set; }
            public List<OccurrenceTask> Unplaced { get; private set; }
            public int TotalScore { get; private set; }
        }
    }
}
