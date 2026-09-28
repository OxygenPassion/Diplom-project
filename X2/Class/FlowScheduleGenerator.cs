using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace X2.Class
{
    internal sealed class FlowScheduleGenerationRequest
    {
        public DataTable PlanTable { get; set; }
        public int ScheduleId { get; set; }
        public List<string> Groups { get; set; }
        public int Semester { get; set; }
        public int Weeks { get; set; }
        public int MaxPairsPerDay { get; set; }
        public int PerWeekLimit { get; set; }
        public Dictionary<string, List<string>> RoomsByType { get; set; }
        public List<int> IgnoredScheduleIds { get; set; }
    }

    internal sealed class FlowScheduleGenerator
    {
        private const string PhysicalLessonType = "Физкультура";
        private const int SameSeriesSlotPenalty = 6000;
        private const int SameSeriesRoomPenalty = 700;
        private const int FutureSeriesSlotConflictPenalty = 3500;
        private const int FutureSeriesDayConflictPenalty = 450;
        private const int SameGroupLabDayPenalty = 30000;
        private const int LatePairPenalty = 4;
        private const int GapPenalty = 22;
        private const int HeavyDayPenalty = 9;
        private const int WeekLoadPenalty = 10;
        private const int NewDayPenalty = 8;
        private const int MaxRandomizedCandidates = 3;
        private const int MaxAttempts = 8;

        private readonly FlowScheduleGenerationRequest _request;
        private readonly List<FlowPlanSpec> _plans;
        private readonly List<Slot> _slots;
        private readonly List<string> _allRooms;

        public FlowScheduleGenerator(FlowScheduleGenerationRequest request)
        {
            if (request == null) throw new ArgumentNullException("request");
            if (request.PlanTable == null) throw new ArgumentNullException("request.PlanTable");
            if (request.RoomsByType == null) throw new ArgumentNullException("request.RoomsByType");

            DataBase.EnsureExcelLikeScheduleSchema();

            _request = request;
            _request.Groups = (_request.Groups ?? new List<string>())
                .Where(g => !string.IsNullOrWhiteSpace(g))
                .Select(g => g.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            _allRooms = request.RoomsByType
                .SelectMany(pair => pair.Value ?? new List<string>())
                .Where(room => !string.IsNullOrWhiteSpace(room))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            _slots = BuildSlots(request.MaxPairsPerDay);
            _plans = BuildPlans();
        }

        public CombinedScheduleGenerationResult Generate()
        {
            AttemptResult best = null;
            int attempts = _plans.Sum(p => p.Weeks.Count) <= 12 ? 2 : MaxAttempts;

            for (int attempt = 0; attempt < attempts; attempt++)
            {
                var random = new Random(unchecked(Environment.TickCount + attempt * 997));
                AttemptResult current = RunAttempt(random);
                if (best == null || current.Score < best.Score)
                    best = current;
            }

            if (best == null)
                best = new AttemptResult(new List<PlacedFlowOccurrence>(), new List<FlowTask>(), 0);

            Persist(best);

            return new CombinedScheduleGenerationResult
            {
                InsertedPairs = best.Placed.Sum(p => p.Length * p.Plan.Groups.Count),
                FailedPairs = best.Unplaced.Sum(t => t.Plan.Length * t.Plan.Groups.Count)
            };
        }

        private AttemptResult RunAttempt(Random random)
        {
            var state = new FlowSearchState(_request);
            foreach (BusyScheduleSlot busy in ScheduleConflictChecker.LoadGlobalBusySlotsForSeason(_request.ScheduleId, _request.Semester, _request.IgnoredScheduleIds))
                state.AddGlobalBusy(busy);

            foreach (ExistingScheduleEntry existing in LoadExistingEntries())
                state.AddExisting(existing);

            List<FlowTask> tasks = BuildTasks(random);
            foreach (FlowTask task in tasks)
            {
                CandidateFlowPlacement candidate = SelectCandidate(task, state, random);
                if (candidate == null)
                    state.MarkUnplaced(task);
                else
                    state.AddPlacement(CreatePlacement(task, candidate));
            }

            return state.CreateAttemptResult();
        }

        private List<FlowTask> BuildTasks(Random random)
        {
            var tasks = new List<FlowTask>();
            foreach (FlowPlanSpec plan in _plans)
            {
                for (int i = 0; i < plan.Weeks.Count; i++)
                {
                    int sequenceIndex = i + 1;
                    tasks.Add(new FlowTask(
                        plan,
                        sequenceIndex,
                        plan.Weeks[i],
                        BuildTaskSeriesKey(plan, sequenceIndex, _request.Weeks),
                        random.Next()));
                }
            }

            return tasks
                .OrderBy(t => GetPlanPriority(t.Plan))
                .ThenByDescending(t => t.Plan.Groups.Count)
                .ThenByDescending(t => t.Plan.Length)
                .ThenByDescending(t => t.Plan.Weeks.Count)
                .ThenBy(t => t.Plan.Rooms.Count == 0 ? int.MaxValue : t.Plan.Rooms.Count)
                .ThenBy(t => t.TargetWeek)
                .ThenBy(t => t.RandomKey)
                .ToList();
        }

        private static int GetPlanPriority(FlowPlanSpec plan)
        {
            if (plan.IsPhysical)
                return 0;
            if (plan.IsFlowLesson && IsLectureLessonType(plan.LessonType))
                return 1;
            if (IsLectureLessonType(plan.LessonType))
                return 2;
            if (plan.Length > 1)
                return 3;
            if (string.Equals(plan.LessonType, "КР", StringComparison.OrdinalIgnoreCase))
                return 4;
            return 5;
        }

        private CandidateFlowPlacement SelectCandidate(FlowTask task, FlowSearchState state, Random random)
        {
            List<CandidateFlowPlacement> candidates = BuildCandidates(task, state, random);
            if (candidates.Count == 0)
                return null;

            Slot preferredSlot = state.GetPreferredSlot(task.SeriesKey);
            string preferredRoom = state.GetPreferredRoom(task.SeriesKey);

            var scored = candidates
                .Select(c => new
                {
                    Candidate = c,
                    Score = state.CalculateCandidateScore(CreatePlacement(task, c), preferredSlot, preferredRoom)
                        + CalculateFutureSeriesPenalty(task, state, c),
                    RandomKey = random.Next()
                })
                .OrderBy(x => x.Score)
                .ThenBy(x => x.RandomKey)
                .ToList();

            int take = ShouldKeepSeriesStable(task.Plan) ? 1 : Math.Min(MaxRandomizedCandidates, scored.Count);
            return scored[random.Next(take)].Candidate;
        }

        private List<CandidateFlowPlacement> BuildCandidates(FlowTask task, FlowSearchState state, Random random)
        {
            Slot preferredSlot = state.GetPreferredSlot(task.SeriesKey);
            string preferredRoom = state.GetPreferredRoom(task.SeriesKey);
            IEnumerable<Slot> orderedSlots = GetOrderedSlots(preferredSlot, random).ToList();

            if (preferredSlot != null)
            {
                List<CandidateFlowPlacement> exactSlotCandidates = BuildCandidatesForSlots(
                    task,
                    state,
                    orderedSlots.Where(slot => slot.Day == preferredSlot.Day && slot.Pair == preferredSlot.Pair),
                    preferredRoom,
                    random);

                if (exactSlotCandidates.Count > 0)
                    return exactSlotCandidates;
            }

            if (ShouldKeepSeriesStable(task.Plan) && preferredSlot != null)
            {
                List<CandidateFlowPlacement> sameDayCandidates = BuildCandidatesForSlots(
                    task,
                    state,
                    orderedSlots.Where(slot => slot.Day == preferredSlot.Day),
                    preferredRoom,
                    random);

                if (sameDayCandidates.Count > 0)
                    return sameDayCandidates;
            }

            return BuildCandidatesForSlots(task, state, orderedSlots, preferredRoom, random);
        }

        private static bool ShouldKeepSeriesStable(FlowPlanSpec plan)
        {
            return plan != null
                && (plan.IsPhysical || !IsLectureLessonType(plan.LessonType));
        }

        private static string BuildTaskSeriesKey(FlowPlanSpec plan, int sequenceIndex, int weeks)
        {
            if (plan == null)
                return string.Empty;

            if (plan.IsPhysical || IsLectureLessonType(plan.LessonType))
            {
                int part = ((sequenceIndex - 1) / Math.Max(1, weeks)) + 1;
                return plan.SeriesKey + "|part:" + part;
            }

            return plan.SeriesKey;
        }

        private static int CalculateFutureSeriesPenalty(FlowTask task, FlowSearchState state, CandidateFlowPlacement candidate)
        {
            if (task == null || task.Plan == null || state == null || candidate == null || !ShouldKeepSeriesStable(task.Plan))
                return 0;

            int penalty = 0;
            foreach (int week in task.Plan.Weeks.Distinct())
            {
                if (week == task.TargetWeek)
                    continue;

                if (state.HasSeriesPlacement(task.SeriesKey, week))
                    continue;

                if (!state.IsFeasible(task.Plan, week, candidate.Day, candidate.Pair, candidate.Room))
                {
                    penalty += FutureSeriesSlotConflictPenalty;

                    bool hasSameDaySlot = false;
                    for (int pair = 1; pair <= state.MaxPairsPerDay && !hasSameDaySlot; pair++)
                    {
                        if (pair + task.Plan.Length - 1 <= state.MaxPairsPerDay
                            && state.IsFeasible(task.Plan, week, candidate.Day, pair, candidate.Room))
                        {
                            hasSameDaySlot = true;
                        }
                    }

                    if (!hasSameDaySlot)
                        penalty += FutureSeriesDayConflictPenalty;
                }
            }

            return penalty;
        }

        private List<CandidateFlowPlacement> BuildCandidatesForSlots(
            FlowTask task,
            FlowSearchState state,
            IEnumerable<Slot> slots,
            string preferredRoom,
            Random random)
        {
            var candidates = new List<CandidateFlowPlacement>();

            foreach (Slot slot in slots)
            {
                if (slot.Pair + task.Plan.Length - 1 > _request.MaxPairsPerDay)
                    continue;

                IEnumerable<string> rooms = !string.IsNullOrWhiteSpace(preferredRoom)
                    ? new[] { preferredRoom }
                    : GetOrderedRooms(task.Plan.Rooms, preferredRoom, random);

                foreach (string room in rooms)
                {
                    if (state.IsFeasible(task.Plan, task.TargetWeek, slot.Day, slot.Pair, room))
                        candidates.Add(new CandidateFlowPlacement(task.TargetWeek, slot.Day, slot.Pair, room));
                }
            }

            return candidates;
        }

        private PlacedFlowOccurrence CreatePlacement(FlowTask task, CandidateFlowPlacement candidate)
        {
            return new PlacedFlowOccurrence(
                _request.ScheduleId,
                task.Plan,
                task.SeriesKey,
                task.TargetWeek,
                candidate.Day,
                candidate.Pair,
                candidate.Room);
        }

        private List<FlowPlanSpec> BuildPlans()
        {
            List<RawPlanRow> rawRows = _request.PlanTable.AsEnumerable()
                .Select(ReadRawPlanRow)
                .Where(row => row != null && _request.Groups.Contains(row.GroupName, StringComparer.OrdinalIgnoreCase))
                .ToList();

            var result = new List<FlowPlanSpec>();
            var mergedRowKeys = new HashSet<int>();

            foreach (IGrouping<string, RawPlanRow> group in rawRows
                .Where(CanBeMergedAcrossGroups)
                .GroupBy(BuildMergeKey))
            {
                List<RawPlanRow> rows = group
                    .OrderBy(row => row.GroupName)
                    .ThenBy(row => row.PlanId)
                    .ToList();
                bool shouldMerge = rows.Count > 1 || rows.Any(row => row.IsFlowLesson) || rows.Any(row => row.IsPhysical);
                if (!shouldMerge)
                    continue;

                result.Add(CreatePlanSpec(rows, true, result.Count));
                foreach (RawPlanRow row in rows)
                    mergedRowKeys.Add(row.PlanId);
            }

            foreach (RawPlanRow row in rawRows.Where(row => !mergedRowKeys.Contains(row.PlanId)))
                result.Add(CreatePlanSpec(new List<RawPlanRow> { row }, false, result.Count));

            return result;
        }

        private RawPlanRow ReadRawPlanRow(DataRow row)
        {
            int subjectId = Convert.ToInt32(row["IDПредмет"]);
            string lessonType = Convert.ToString(row["ТипЗанятия"]).Trim();
            bool isPhysical = IsPhysicalLessonType(lessonType) || DataBase.IsPhysicalEducationSubject(subjectId);

            return new RawPlanRow
            {
                PlanId = Convert.ToInt32(row["IDУчебныйПлан"]),
                GroupName = Convert.ToString(row["НазваниеГруппы"]).Trim(),
                SubjectId = subjectId,
                LessonType = lessonType,
                TeacherId = isPhysical || row["IDПреподаватель"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["IDПреподаватель"]),
                Hours = Convert.ToInt32(row["ЧасыВСеместре"]),
                RequiredRoomType = GetOptionalString(row, "ТребуемыйТипАудитории"),
                RequiredRoom = GetOptionalString(row, "ТребуемаяАудитория"),
                IsFlowLesson = GetOptionalBool(row, "ПотоковоеЗанятие"),
                WeekPattern = GetOptionalString(row, "ШаблонНедель"),
                IsPhysical = isPhysical
            };
        }

        private static bool CanBeMergedAcrossGroups(RawPlanRow row)
        {
            return row.IsPhysical
                || row.IsFlowLesson
                || IsLectureLessonType(row.LessonType);
        }

        private static string BuildMergeKey(RawPlanRow row)
        {
            return string.Join("\u001F",
                row.SubjectId,
                row.TeacherId.HasValue ? row.TeacherId.Value.ToString() : string.Empty,
                row.LessonType,
                row.Hours,
                row.RequiredRoomType ?? string.Empty,
                row.RequiredRoom ?? string.Empty,
                row.WeekPattern ?? string.Empty,
                row.IsPhysical ? "1" : "0");
        }

        private FlowPlanSpec CreatePlanSpec(List<RawPlanRow> rows, bool merged, int planIndex)
        {
            RawPlanRow first = rows[0];
            List<string> groups = rows
                .Select(row => row.GroupName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(group => group)
                .ToList();
            Dictionary<string, int> planIdsByGroup = rows
                .GroupBy(row => row.GroupName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().PlanId, StringComparer.OrdinalIgnoreCase);
            List<int> planIds = rows
                .Select(row => row.PlanId)
                .Distinct()
                .OrderBy(id => id)
                .ToList();

            int length = string.Equals(first.LessonType, "ЛБ", StringComparison.OrdinalIgnoreCase) ? 2 : 1;
            List<int> weeks = BuildWeeks(first, planIndex);
            List<string> rooms = ResolveRooms(first);
            string seriesKey = BuildSeriesKey(first, groups, planIds);

            return new FlowPlanSpec(
                seriesKey,
                planIds,
                planIdsByGroup,
                groups,
                first.SubjectId,
                first.LessonType,
                first.TeacherId,
                length,
                first.IsPhysical,
                merged || first.IsFlowLesson || groups.Count > 1,
                weeks,
                rooms);
        }

        private static string BuildSeriesKey(RawPlanRow row, List<string> groups, List<int> planIds)
        {
            if (row.IsFlowLesson || row.IsPhysical || groups.Count > 1)
            {
                return string.Join("\u001F",
                    "flow",
                    row.SubjectId,
                    row.TeacherId.HasValue ? row.TeacherId.Value.ToString() : string.Empty,
                    row.LessonType,
                    string.Join(",", groups));
            }

            return "plan|" + string.Join(",", planIds);
        }

        private List<string> ResolveRooms(RawPlanRow row)
        {
            if (row.IsPhysical)
                return new List<string> { string.Empty };

            if (!string.IsNullOrWhiteSpace(row.RequiredRoom))
            {
                string matched = _allRooms.FirstOrDefault(room => string.Equals(room, row.RequiredRoom, StringComparison.OrdinalIgnoreCase));
                return string.IsNullOrWhiteSpace(matched) ? new List<string>() : new List<string> { matched };
            }

            string neededRoomType = !string.IsNullOrWhiteSpace(row.RequiredRoomType)
                ? row.RequiredRoomType
                : GetNeededRoomType(row.LessonType);

            List<string> rooms;
            if (!_request.RoomsByType.TryGetValue(neededRoomType, out rooms) || rooms == null || rooms.Count == 0)
                rooms = _allRooms;

            return rooms
                .Where(room => !string.IsNullOrWhiteSpace(room))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private List<int> BuildWeeks(RawPlanRow row, int planIndex)
        {
            List<int> explicitWeeks = ParseWeekPattern(row.WeekPattern, _request.Weeks);
            if (explicitWeeks.Count > 0)
                return explicitWeeks;

            int occurrenceCount = GetOccurrenceCount(row.Hours, row.LessonType);
            if (occurrenceCount <= 0)
                return new List<int>();

            if (row.IsPhysical)
                return BuildSequentialWeeks(occurrenceCount, _request.Weeks);

            if (IsLectureLessonType(row.LessonType))
                return BuildSequentialWeeks(occurrenceCount, _request.Weeks);

            if (occurrenceCount == 4)
                return BuildFourWeekCycle(1 + (planIndex % 4), occurrenceCount, _request.Weeks);

            return BuildEvenWeeks(occurrenceCount, _request.Weeks);
        }

        private static List<int> ParseWeekPattern(string pattern, int weeksInSemester)
        {
            var result = new List<int>();
            string text = (pattern ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(text))
                return result;

            if (text.IndexOf("кажд", StringComparison.OrdinalIgnoreCase) >= 0)
                return Enumerable.Range(1, weeksInSemester).ToList();

            string normalized = text.Replace(";", ",").Replace(" ", ",");
            if (normalized.Contains("-"))
            {
                string[] rangeParts = normalized.Split(new[] { '-' }, 2);
                int start;
                int end;
                if (rangeParts.Length == 2
                    && int.TryParse(rangeParts[0].Trim(','), out start)
                    && int.TryParse(rangeParts[1].Trim(','), out end))
                {
                    if (end < start)
                    {
                        int temp = start;
                        start = end;
                        end = temp;
                    }

                    for (int week = start; week <= end; week++)
                    {
                        if (week >= 1 && week <= weeksInSemester)
                            result.Add(week);
                    }
                }
            }
            else
            {
                foreach (string part in normalized.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    int week;
                    if (int.TryParse(part.Trim(), out week) && week >= 1 && week <= weeksInSemester)
                        result.Add(week);
                }
            }

            return result.Distinct().OrderBy(week => week).ToList();
        }

        private static List<int> BuildFourWeekCycle(int startWeek, int occurrenceCount, int weeks)
        {
            var result = new List<int>();
            for (int week = startWeek; week <= weeks && result.Count < occurrenceCount; week += 4)
                result.Add(week);

            if (result.Count < occurrenceCount)
            {
                foreach (int week in BuildEvenWeeks(occurrenceCount, weeks))
                {
                    if (!result.Contains(week))
                        result.Add(week);
                    if (result.Count >= occurrenceCount)
                        break;
                }
            }

            return result.OrderBy(week => week).ToList();
        }

        private static List<int> BuildSequentialWeeks(int occurrenceCount, int weeks)
        {
            var result = new List<int>();
            if (occurrenceCount <= 0 || weeks <= 0)
                return result;

            for (int i = 0; i < occurrenceCount; i++)
                result.Add(1 + (i % weeks));

            return result;
        }

        private static List<int> BuildEvenWeeks(int occurrenceCount, int weeks)
        {
            var result = new List<int>();
            if (occurrenceCount >= weeks)
                return Enumerable.Range(1, weeks).ToList();

            for (int i = 0; i < occurrenceCount; i++)
            {
                int week = 1 + (int)Math.Floor(i * (double)weeks / occurrenceCount);
                while (result.Contains(week) && week < weeks)
                    week++;
                result.Add(Math.Max(1, Math.Min(weeks, week)));
            }

            return result.Distinct().OrderBy(week => week).ToList();
        }

        private static int GetOccurrenceCount(int hours, string lessonType)
        {
            return string.Equals(lessonType, "ЛБ", StringComparison.OrdinalIgnoreCase)
                ? Math.Max(1, hours / 4)
                : Math.Max(1, (int)Math.Ceiling(hours / 2.0));
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

        private IEnumerable<Slot> GetOrderedSlots(Slot preferredSlot, Random random)
        {
            return _slots
                .Select(slot => new
                {
                    Slot = slot,
                    Priority = preferredSlot == null
                        ? 0
                        : (Math.Abs(slot.Day - preferredSlot.Day) * 12) + (Math.Abs(slot.Pair - preferredSlot.Pair) * 5),
                    Exact = preferredSlot != null && slot.Day == preferredSlot.Day && slot.Pair == preferredSlot.Pair ? 0 : 1,
                    RandomKey = random.Next()
                })
                .OrderBy(x => x.Exact)
                .ThenBy(x => x.Priority)
                .ThenBy(x => x.RandomKey)
                .Select(x => x.Slot);
        }

        private static IEnumerable<string> GetOrderedRooms(List<string> rooms, string preferredRoom, Random random)
        {
            return (rooms ?? new List<string>())
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
                .Select(x => x.Room);
        }

        private List<ExistingScheduleEntry> LoadExistingEntries()
        {
            string groupsCsv = string.Join("|", _request.Groups);
            DataTable dt = DataBase.Query(@"
SELECT
    НомерНедели,
    ДеньНедели,
    НомерПары,
    НазваниеГруппы,
    IDПреподаватель,
    НомерАудитории
FROM dbo.РасписаниеЗанятий
WHERE IDРасписание = @sid
  AND CHARINDEX(N'|' + НазваниеГруппы + N'|', N'|' + @groupsCsv + N'|') = 0;",
                new SqlParameter("@sid", _request.ScheduleId),
                new SqlParameter("@groupsCsv", groupsCsv));

            var result = new List<ExistingScheduleEntry>();
            foreach (DataRow row in dt.Rows)
            {
                result.Add(new ExistingScheduleEntry(
                    Convert.ToInt32(row["НомерНедели"]),
                    Convert.ToInt32(row["ДеньНедели"]),
                    Convert.ToInt32(row["НомерПары"]),
                    Convert.ToString(row["НазваниеГруппы"]),
                    row["IDПреподаватель"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["IDПреподаватель"]),
                    Convert.ToString(row["НомерАудитории"])));
            }

            return result;
        }

        private void Persist(AttemptResult result)
        {
            string groupsCsv = string.Join("|", _request.Groups);

            DataBase.ExecuteInTransaction((con, tx) =>
            {
                ExecuteNonQuery(con, tx, @"
DELETE FROM dbo.РасписаниеЗанятий
WHERE IDРасписание = @sid
  AND CHARINDEX(N'|' + НазваниеГруппы + N'|', N'|' + @groupsCsv + N'|') > 0;",
                    new SqlParameter("@sid", _request.ScheduleId),
                    new SqlParameter("@groupsCsv", groupsCsv));

                foreach (int planId in _plans.SelectMany(p => p.PlanIds).Distinct())
                {
                    ExecuteNonQuery(con, tx,
                        "DELETE FROM dbo.ПланНедели WHERE IDУчебныйПлан = @id;",
                        new SqlParameter("@id", planId));
                }

                foreach (PlacedFlowOccurrence placement in result.Placed
                    .OrderBy(p => p.Week)
                    .ThenBy(p => p.Day)
                    .ThenBy(p => p.Pair)
                    .ThenBy(p => p.Plan.SeriesKey))
                {
                    Guid seriesId = Guid.NewGuid();
                    foreach (string group in placement.Plan.Groups)
                    {
                        int planId = placement.Plan.GetPlanIdForGroup(group);
                        for (int offset = 0; offset < placement.Length; offset++)
                        {
                            InsertScheduleEntry(con, tx, placement, group, planId, placement.Pair + offset, seriesId);
                        }
                    }
                }

                foreach (IGrouping<int, PlacedFlowOccurrence> planGroup in result.Placed
                    .SelectMany(p => p.Plan.PlanIds.Select(planId => new { PlanId = planId, Placement = p }))
                    .GroupBy(x => x.PlanId, x => x.Placement))
                {
                    foreach (int week in planGroup.Select(p => p.Week).Distinct().OrderBy(w => w))
                    {
                        ExecuteNonQuery(con, tx,
                            "INSERT INTO dbo.ПланНедели(IDУчебныйПлан, НомерНедели) VALUES(@id, @week);",
                            new SqlParameter("@id", planGroup.Key),
                            new SqlParameter("@week", week));
                    }
                }
            });
        }

        private static void InsertScheduleEntry(SqlConnection con, SqlTransaction tx, PlacedFlowOccurrence placement, string group, int planId, int pair, Guid seriesId)
        {
            ExecuteNonQuery(con, tx, @"
INSERT INTO dbo.РасписаниеЗанятий
    (IDРасписание, НомерНедели, ДеньНедели, НомерПары, НазваниеГруппы, IDПредмет, ТипЗанятия, IDПреподаватель, НомерАудитории, СвязкаЗанятия, IDУчебныйПлан)
VALUES
    (@sid, @week, @day, @pair, @group, @subjectId, @lessonType, @teacherId, @room, @seriesId, @planId);",
                new SqlParameter("@sid", placement.ScheduleId),
                new SqlParameter("@week", placement.Week),
                new SqlParameter("@day", placement.Day),
                new SqlParameter("@pair", pair),
                new SqlParameter("@group", group),
                new SqlParameter("@subjectId", placement.SubjectId),
                new SqlParameter("@lessonType", placement.LessonType),
                new SqlParameter("@teacherId", (object)placement.TeacherId ?? DBNull.Value),
                new SqlParameter("@room", string.IsNullOrWhiteSpace(placement.Room) ? (object)DBNull.Value : placement.Room),
                new SqlParameter("@seriesId", seriesId),
                new SqlParameter("@planId", planId));
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

        private static string GetOptionalString(DataRow row, string columnName)
        {
            if (row.Table == null || !row.Table.Columns.Contains(columnName) || row[columnName] == DBNull.Value)
                return string.Empty;

            return Convert.ToString(row[columnName]).Trim();
        }

        private static bool GetOptionalBool(DataRow row, string columnName)
        {
            if (row.Table == null || !row.Table.Columns.Contains(columnName) || row[columnName] == DBNull.Value)
                return false;

            return Convert.ToBoolean(row[columnName]);
        }

        private static string GetNeededRoomType(string lessonType)
        {
            switch (lessonType)
            {
                case "ЛК":
                    return "Лекции";
                case "ЛБ":
                    return "Компьютерный класс";
                case "КР":
                    return "Упражнения";
                case "У":
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

        private sealed class FlowSearchState
        {
            private readonly FlowScheduleGenerationRequest _request;
            private readonly Dictionary<string, bool> _groupBusy;
            private readonly HashSet<long> _teacherBusy;
            private readonly HashSet<string> _roomBusy;
            private readonly List<PlacedFlowOccurrence> _placed;
            private readonly List<FlowTask> _unplaced;
            private readonly Dictionary<string, List<PlacedFlowOccurrence>> _placedBySeries;

            public FlowSearchState(FlowScheduleGenerationRequest request)
            {
                _request = request;
                _groupBusy = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
                _teacherBusy = new HashSet<long>();
                _roomBusy = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                _placed = new List<PlacedFlowOccurrence>();
                _unplaced = new List<FlowTask>();
                _placedBySeries = new Dictionary<string, List<PlacedFlowOccurrence>>(StringComparer.OrdinalIgnoreCase);
            }

            public int MaxPairsPerDay
            {
                get { return _request.MaxPairsPerDay; }
            }

            public void AddExisting(ExistingScheduleEntry entry)
            {
                AddBusy(entry.Week, entry.Day, entry.Pair, entry.GroupName, entry.TeacherId, entry.Room);
            }

            public void AddGlobalBusy(BusyScheduleSlot slot)
            {
                if (slot == null)
                    return;

                AddBusy(slot.Week, slot.Day, slot.Pair, null, slot.TeacherId, slot.Room);
            }

            public bool IsFeasible(FlowPlanSpec plan, int week, int day, int startPair, string room)
            {
                if (plan.Rooms.Count == 0)
                    return false;

                if (plan.IsPhysical && HasPhysicalOnSameWeekDay(plan, week, day))
                    return false;

                for (int offset = 0; offset < plan.Length; offset++)
                {
                    int pair = startPair + offset;
                    if (pair > _request.MaxPairsPerDay)
                        return false;

                    foreach (string group in plan.Groups)
                    {
                        if (HasGroupConflict(week, day, pair, group))
                            return false;
                    }

                    if (plan.TeacherId.HasValue && _teacherBusy.Contains(ScheduleConflictChecker.BuildTeacherKey(week, day, pair, plan.TeacherId.Value)))
                        return false;

                    if (!string.IsNullOrWhiteSpace(room) && _roomBusy.Contains(ScheduleConflictChecker.BuildRoomKey(week, day, pair, room)))
                        return false;
                }

                return true;
            }

            private bool HasPhysicalOnSameWeekDay(FlowPlanSpec plan, int week, int day)
            {
                return _placed.Any(placement =>
                    placement.Plan.IsPhysical
                    && placement.Week == week
                    && placement.Day == day
                    && placement.Plan.Groups.Any(group => plan.Groups.Contains(group, StringComparer.OrdinalIgnoreCase)));
            }

            public void AddPlacement(PlacedFlowOccurrence placement)
            {
                _placed.Add(placement);

                List<PlacedFlowOccurrence> seriesItems;
                if (!_placedBySeries.TryGetValue(placement.SeriesKey, out seriesItems))
                {
                    seriesItems = new List<PlacedFlowOccurrence>();
                    _placedBySeries[placement.SeriesKey] = seriesItems;
                }

                seriesItems.Add(placement);

                for (int offset = 0; offset < placement.Length; offset++)
                {
                    int pair = placement.Pair + offset;
                    foreach (string group in placement.Plan.Groups)
                    {
                        AddBusy(placement.Week, placement.Day, pair, group, null, null);
                    }

                    ScheduleConflictChecker.AddBusySlot(
                        null,
                        _teacherBusy,
                        _roomBusy,
                        placement.Week,
                        placement.Day,
                        pair,
                        placement.TeacherId,
                        placement.Room);
                }
            }

            public void MarkUnplaced(FlowTask task)
            {
                if (!_unplaced.Contains(task))
                    _unplaced.Add(task);
            }

            public Slot GetPreferredSlot(string seriesKey)
            {
                List<PlacedFlowOccurrence> items;
                if (!_placedBySeries.TryGetValue(seriesKey, out items) || items.Count == 0)
                    return null;

                return items
                    .GroupBy(item => ScheduleConflictChecker.BuildTimeKey(0, item.Day, item.Pair))
                    .OrderByDescending(group => group.Count())
                    .ThenBy(group => group.First().Day)
                    .ThenBy(group => group.First().Pair)
                    .Select(group => new Slot(group.First().Day, group.First().Pair))
                    .FirstOrDefault();
            }

            public string GetPreferredRoom(string seriesKey)
            {
                List<PlacedFlowOccurrence> items;
                if (!_placedBySeries.TryGetValue(seriesKey, out items) || items.Count == 0)
                    return string.Empty;

                return items
                    .Where(item => !string.IsNullOrWhiteSpace(item.Room))
                    .GroupBy(item => item.Room, StringComparer.OrdinalIgnoreCase)
                    .OrderByDescending(group => group.Count())
                    .ThenBy(group => group.Key)
                    .Select(group => group.Key)
                    .FirstOrDefault() ?? string.Empty;
            }

            public bool HasSeriesPlacement(string seriesKey, int week)
            {
                List<PlacedFlowOccurrence> items;
                return _placedBySeries.TryGetValue(seriesKey, out items)
                    && items.Any(item => item.Week == week);
            }

            public int CalculateCandidateScore(PlacedFlowOccurrence placement, Slot preferredSlot, string preferredRoom)
            {
                int penalty = 0;

                for (int offset = 0; offset < placement.Length; offset++)
                {
                    int pair = placement.Pair + offset;
                    penalty += Math.Max(0, pair - 2) * LatePairPenalty;
                }

                foreach (string group in placement.Plan.Groups)
                {
                    if (IsLabLessonType(placement.LessonType))
                    {
                        int labSeriesOnDay = _placed
                            .Where(p => p.Day == placement.Day
                                && IsLabLessonType(p.LessonType)
                                && !string.Equals(p.Plan.SeriesKey, placement.Plan.SeriesKey, StringComparison.OrdinalIgnoreCase)
                                && p.Plan.Groups.Contains(group, StringComparer.OrdinalIgnoreCase))
                            .Select(p => p.Plan.SeriesKey)
                            .Distinct(StringComparer.OrdinalIgnoreCase)
                            .Count();

                        penalty += labSeriesOnDay * labSeriesOnDay * SameGroupLabDayPenalty;
                    }

                    List<int> occupiedPairs = _placed
                        .Where(p => p.Week == placement.Week && p.Day == placement.Day && p.Plan.Groups.Contains(group, StringComparer.OrdinalIgnoreCase))
                        .SelectMany(p => Enumerable.Range(p.Pair, p.Length))
                        .Concat(Enumerable.Range(placement.Pair, placement.Length))
                        .Distinct()
                        .OrderBy(pair => pair)
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

                    int weekLoad = _placed
                        .Where(p => p.Week == placement.Week && p.Plan.Groups.Contains(group, StringComparer.OrdinalIgnoreCase))
                        .Sum(p => p.Length);
                    penalty += weekLoad * WeekLoadPenalty;

                    bool usesDayThisWeek = _placed.Any(p => p.Week == placement.Week && p.Day == placement.Day && p.Plan.Groups.Contains(group, StringComparer.OrdinalIgnoreCase));
                    if (!usesDayThisWeek)
                        penalty += NewDayPenalty;
                }

                if (preferredSlot != null && (placement.Day != preferredSlot.Day || placement.Pair != preferredSlot.Pair))
                {
                    penalty += SameSeriesSlotPenalty;
                    penalty += Math.Abs(placement.Day - preferredSlot.Day) * 6;
                    penalty += Math.Abs(placement.Pair - preferredSlot.Pair) * 4;
                }

                if (!string.IsNullOrWhiteSpace(preferredRoom)
                    && !string.Equals(placement.Room, preferredRoom, StringComparison.OrdinalIgnoreCase))
                {
                    penalty += SameSeriesRoomPenalty;
                }

                return penalty;
            }

            public AttemptResult CreateAttemptResult()
            {
                int score = _unplaced.Sum(t => t.Plan.Length * Math.Max(1, t.Plan.Groups.Count) * 100000);
                foreach (PlacedFlowOccurrence placement in _placed)
                    score += CalculateCandidateScore(placement, GetPreferredSlot(placement.SeriesKey), GetPreferredRoom(placement.SeriesKey));

                return new AttemptResult(
                    _placed.ToList(),
                    _unplaced.ToList(),
                    score);
            }

            private void AddBusy(int week, int day, int pair, string groupName, int? teacherId, string room)
            {
                if (!string.IsNullOrWhiteSpace(groupName))
                    _groupBusy[BuildGroupKey(week, day, pair, groupName)] = true;

                ScheduleConflictChecker.AddBusySlot(null, _teacherBusy, _roomBusy, week, day, pair, teacherId, room);
            }

            private bool HasGroupConflict(int week, int day, int pair, string groupName)
            {
                return _groupBusy.ContainsKey(BuildGroupKey(week, day, pair, groupName));
            }

            private static string BuildGroupKey(int week, int day, int pair, string groupName)
            {
                return ScheduleConflictChecker.BuildTimeKey(week, day, pair) + "|" + (groupName ?? string.Empty);
            }
        }

        private sealed class RawPlanRow
        {
            public int PlanId { get; set; }
            public string GroupName { get; set; }
            public int SubjectId { get; set; }
            public string LessonType { get; set; }
            public int? TeacherId { get; set; }
            public int Hours { get; set; }
            public string RequiredRoomType { get; set; }
            public string RequiredRoom { get; set; }
            public bool IsFlowLesson { get; set; }
            public string WeekPattern { get; set; }
            public bool IsPhysical { get; set; }
        }

        private sealed class FlowPlanSpec
        {
            private readonly Dictionary<string, int> _planIdsByGroup;

            public FlowPlanSpec(
                string seriesKey,
                List<int> planIds,
                Dictionary<string, int> planIdsByGroup,
                List<string> groups,
                int subjectId,
                string lessonType,
                int? teacherId,
                int length,
                bool isPhysical,
                bool isFlowLesson,
                List<int> weeks,
                List<string> rooms)
            {
                SeriesKey = seriesKey;
                PlanIds = planIds ?? new List<int>();
                _planIdsByGroup = planIdsByGroup ?? new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                Groups = groups ?? new List<string>();
                SubjectId = subjectId;
                LessonType = lessonType;
                TeacherId = teacherId;
                Length = length;
                IsPhysical = isPhysical;
                IsFlowLesson = isFlowLesson;
                Weeks = weeks ?? new List<int>();
                Rooms = rooms ?? new List<string>();
            }

            public string SeriesKey { get; private set; }
            public List<int> PlanIds { get; private set; }
            public List<string> Groups { get; private set; }
            public int SubjectId { get; private set; }
            public string LessonType { get; private set; }
            public int? TeacherId { get; private set; }
            public int Length { get; private set; }
            public bool IsPhysical { get; private set; }
            public bool IsFlowLesson { get; private set; }
            public List<int> Weeks { get; private set; }
            public List<string> Rooms { get; private set; }

            public int GetPlanIdForGroup(string group)
            {
                int planId;
                if (_planIdsByGroup.TryGetValue(group, out planId))
                    return planId;

                return PlanIds.Count > 0 ? PlanIds[0] : 0;
            }
        }

        private sealed class FlowTask
        {
            public FlowTask(FlowPlanSpec plan, int sequenceIndex, int targetWeek, string seriesKey, int randomKey)
            {
                Plan = plan;
                SequenceIndex = sequenceIndex;
                TargetWeek = targetWeek;
                SeriesKey = seriesKey;
                RandomKey = randomKey;
            }

            public FlowPlanSpec Plan { get; private set; }
            public int SequenceIndex { get; private set; }
            public int TargetWeek { get; private set; }
            public string SeriesKey { get; private set; }
            public int RandomKey { get; private set; }
        }

        private sealed class PlacedFlowOccurrence
        {
            public PlacedFlowOccurrence(int scheduleId, FlowPlanSpec plan, string seriesKey, int week, int day, int pair, string room)
            {
                ScheduleId = scheduleId;
                Plan = plan;
                SeriesKey = seriesKey;
                Week = week;
                Day = day;
                Pair = pair;
                Room = room;
            }

            public FlowPlanSpec Plan { get; private set; }
            public string SeriesKey { get; private set; }
            public int ScheduleId { get; private set; }
            public int SubjectId { get { return Plan.SubjectId; } }
            public string LessonType { get { return Plan.LessonType; } }
            public int? TeacherId { get { return Plan.TeacherId; } }
            public int Length { get { return Plan.Length; } }
            public int Week { get; private set; }
            public int Day { get; private set; }
            public int Pair { get; private set; }
            public string Room { get; private set; }
        }

        private sealed class CandidateFlowPlacement
        {
            public CandidateFlowPlacement(int week, int day, int pair, string room)
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

        private sealed class ExistingScheduleEntry
        {
            public ExistingScheduleEntry(int week, int day, int pair, string groupName, int? teacherId, string room)
            {
                Week = week;
                Day = day;
                Pair = pair;
                GroupName = groupName;
                TeacherId = teacherId;
                Room = room;
            }

            public int Week { get; private set; }
            public int Day { get; private set; }
            public int Pair { get; private set; }
            public string GroupName { get; private set; }
            public int? TeacherId { get; private set; }
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

        private sealed class AttemptResult
        {
            public AttemptResult(List<PlacedFlowOccurrence> placed, List<FlowTask> unplaced, int score)
            {
                Placed = placed ?? new List<PlacedFlowOccurrence>();
                Unplaced = unplaced ?? new List<FlowTask>();
                Score = score;
            }

            public List<PlacedFlowOccurrence> Placed { get; private set; }
            public List<FlowTask> Unplaced { get; private set; }
            public int Score { get; private set; }
        }
    }
}
