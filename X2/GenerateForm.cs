using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using X2.Class;

namespace X2
{
    public partial class GenerateForm : Form
    {
        private DataTable _allSubjects;
        private DataTable _subjects;
        private DataTable _types;
        private DataTable _roomTypes;
        private DataTable _rooms;

        private DataTable _planTable;
        private bool _scheduleNameIsAuto = true;
        private bool _suppressScheduleNameTextChanged;
        private bool _suppressPlanRefresh;
        private bool _loadingGroups;
        private const string PhysicalLessonType = "Физкультура";
        private const string SingleGroupGenerationMode = "Одна группа";
        private const string FlowGenerationMode = "Поток курса";
        private const string DirectionGenerationMode = "Направление 1-4 курс";

        public GenerateForm()
        {
            InitializeComponent();
        }

        private void GenerateForm_Load(object sender, EventArgs e)
        {
            DataBase.EnsureTeacherSubjectSchema();
            DataBase.EnsureSubjectProfileSemesterSchema();
            DataBase.EnsureGroupFlowSchema();
            DataBase.EnsureLessonTypeSchema();
            DataBase.EnsureScheduleRoomNullableSchema();
            DataBase.EnsureRoomTypeSchema();
            DataBase.EnsureCurriculumRoomRequirementSchema();
            DataBase.EnsureExcelLikeScheduleSchema();

            txtScheduleName.TextChanged -= txtScheduleName_TextChanged;
            txtScheduleName.TextChanged += txtScheduleName_TextChanged;

            LoadPlanLookups();
            LoadGroups();

            cbGroup.SelectedIndexChanged -= cbGroup_SelectedIndexChanged;
            cbGroup.SelectedIndexChanged += cbGroup_SelectedIndexChanged;

            cbGenerationMode.SelectedIndexChanged -= cbGenerationMode_SelectedIndexChanged;
            cbGenerationMode.SelectedIndexChanged += cbGenerationMode_SelectedIndexChanged;

            cbFlowCourse.SelectedIndexChanged -= cbFlowCourse_SelectedIndexChanged;
            cbFlowCourse.SelectedIndexChanged += cbFlowCourse_SelectedIndexChanged;

            cbFlowName.SelectedIndexChanged -= cbFlowName_SelectedIndexChanged;
            cbFlowName.SelectedIndexChanged += cbFlowName_SelectedIndexChanged;

            cbDirectionName.SelectedIndexChanged -= cbDirectionName_SelectedIndexChanged;
            cbDirectionName.SelectedIndexChanged += cbDirectionName_SelectedIndexChanged;

            nudSemester.ValueChanged -= nudSemester_ValueChanged;
            nudSemester.ValueChanged += nudSemester_ValueChanged;

            dgvPlan.CurrentCellDirtyStateChanged -= dgvPlan_CurrentCellDirtyStateChanged;
            dgvPlan.CurrentCellDirtyStateChanged += dgvPlan_CurrentCellDirtyStateChanged;

            dgvPlan.CellValueChanged -= dgvPlan_CellValueChanged;
            dgvPlan.CellValueChanged += dgvPlan_CellValueChanged;

            LoadFlowGenerationLookups();
            LoadDirectionGenerationLookups();
            RefreshGroupPlanAndWeeks();
            UpdateDefaultScheduleName(true);
        }

        private void nudSemester_ValueChanged(object sender, EventArgs e)
        {
            if (_suppressPlanRefresh)
                return;

            if (IsDirectionGenerationMode())
            {
                UpdateDefaultScheduleName(false);
                return;
            }

            RefreshGroupPlanAndWeeks();
            UpdateDefaultScheduleName(false);
        }

        private void cbGroup_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_loadingGroups || _suppressPlanRefresh)
                return;

            ApplyDefaultSemesterForSelectedGroup();
            RefreshGroupPlanAndWeeks();
            UpdateDefaultScheduleName(false);
        }

        private void txtScheduleName_TextChanged(object sender, EventArgs e)
        {
            if (!_suppressScheduleNameTextChanged)
                _scheduleNameIsAuto = false;
        }

        private void btnClose_Click(object sender, EventArgs e) => Close();

        // ---------------------------------------
        // LOOKUPS (предметы/преподы/типы)
        // ---------------------------------------
        private void LoadPlanLookups()
        {
            _allSubjects = DataBase.Query(@"
SELECT
    IDПредмет,
    Название
FROM dbo.Предметы
ORDER BY Название;");
            _subjects = CreateEmptySubjectTable();

            _types = new DataTable();
            _types.Columns.Add("Код", typeof(string));
            _types.Columns.Add("Название", typeof(string));
            _types.Rows.Add("ЛК", "Лекция");
            _types.Rows.Add("У", "Упражнение");
            _types.Rows.Add("ЛБ", "Лабораторная");
            _types.Rows.Add("КР", "Контрольная");
            _types.Rows.Add(PhysicalLessonType, PhysicalLessonType);

            LoadRoomRequirementLookups();
        }

        private void LoadRoomRequirementLookups()
        {
            _roomTypes = new DataTable();
            _roomTypes.Columns.Add("ТипАудитории", typeof(string));
            _roomTypes.Columns.Add("Название", typeof(string));
            _roomTypes.Rows.Add(string.Empty, "Авто");

            DataTable roomTypeRows = DataBase.Query(@"
SELECT DISTINCT ТипАудитории
FROM dbo.Аудитории
WHERE LTRIM(RTRIM(ТипАудитории)) <> N''
ORDER BY ТипАудитории;");
            foreach (DataRow row in roomTypeRows.Rows)
            {
                string roomType = Convert.ToString(row["ТипАудитории"]);
                _roomTypes.Rows.Add(roomType, roomType);
            }

            _rooms = new DataTable();
            _rooms.Columns.Add("НомерАудитории", typeof(string));
            _rooms.Columns.Add("Название", typeof(string));
            _rooms.Columns.Add("ТипАудитории", typeof(string));
            _rooms.Rows.Add(string.Empty, "Авто", string.Empty);

            DataTable roomRows = DataBase.Query(@"
SELECT НомерАудитории, ТипАудитории
FROM dbo.Аудитории
ORDER BY НомерАудитории;");
            foreach (DataRow row in roomRows.Rows)
            {
                string room = Convert.ToString(row["НомерАудитории"]);
                string roomType = Convert.ToString(row["ТипАудитории"]);
                _rooms.Rows.Add(room, room + " | " + roomType, roomType);
            }
        }

        // ---------------------------------------
        // Группы / недели / учебный план
        // ---------------------------------------
        private void LoadGroups()
        {
            LoadGroupsForScope();
        }

        private void LoadGroupsForScope()
        {
            int? course = null;
            string flow = null;

            if (IsFlowGenerationMode() && cbFlowCourse.SelectedValue != null && cbFlowName.SelectedValue != null)
            {
                course = Convert.ToInt32(cbFlowCourse.SelectedValue);
                flow = Convert.ToString(cbFlowName.SelectedValue);
            }

            LoadGroups(course, flow);
        }

        private void LoadGroups(int? course, string flow)
        {
            string oldGroup = cbGroup.SelectedValue == null ? string.Empty : Convert.ToString(cbGroup.SelectedValue);

            DataTable dt = DataBase.Query(@"
SELECT
    НазваниеГруппы,
    Курс,
    ISNULL(NULLIF(LTRIM(RTRIM(Поток)), N''), Направление) AS Поток
FROM dbo.Группы
WHERE (@course IS NULL OR Курс = @course)
  AND (@flow IS NULL OR ISNULL(NULLIF(LTRIM(RTRIM(Поток)), N''), Направление) = @flow)
ORDER BY Курс, ISNULL(NULLIF(LTRIM(RTRIM(Поток)), N''), Направление), НазваниеГруппы;",
                new SqlParameter("@course", SqlDbType.Int) { Value = (object)course ?? DBNull.Value },
                new SqlParameter("@flow", string.IsNullOrWhiteSpace(flow) ? (object)DBNull.Value : flow));

            _loadingGroups = true;
            try
            {
                cbGroup.DisplayMember = "НазваниеГруппы";
                cbGroup.ValueMember = "НазваниеГруппы";
                cbGroup.DataSource = dt;

                if (!string.IsNullOrWhiteSpace(oldGroup))
                    cbGroup.SelectedValue = oldGroup;

                if ((cbGroup.SelectedIndex < 0 || cbGroup.SelectedValue == null) && cbGroup.Items.Count > 0)
                    cbGroup.SelectedIndex = 0;
            }
            finally
            {
                _loadingGroups = false;
            }

            ApplyDefaultSemesterForSelectedGroup();
            RefreshGroupPlanAndWeeks();
        }

        private void LoadFlowGenerationLookups()
        {
            DataTable courses = DataBase.Query(@"
SELECT DISTINCT Курс
FROM dbo.Группы
ORDER BY Курс;");

            cbFlowCourse.DisplayMember = "Курс";
            cbFlowCourse.ValueMember = "Курс";
            cbFlowCourse.DataSource = courses;

            if (cbFlowCourse.Items.Count > 0)
                cbFlowCourse.SelectedIndex = 0;

            LoadFlowNames();
            UpdateGenerationScopeControls();
        }

        private void LoadDirectionGenerationLookups()
        {
            DataTable directions = DataBase.Query(@"
SELECT DISTINCT ISNULL(NULLIF(LTRIM(RTRIM(Поток)), N''), Направление) AS Направление
FROM dbo.Группы
WHERE Курс BETWEEN 1 AND 4
ORDER BY Направление;");

            cbDirectionName.DisplayMember = "Направление";
            cbDirectionName.ValueMember = "Направление";
            cbDirectionName.DataSource = directions;

            if (cbDirectionName.Items.Count > 0)
                cbDirectionName.SelectedIndex = 0;
        }

        private void LoadFlowNames()
        {
            if (cbFlowCourse.SelectedValue == null)
            {
                cbFlowName.DataSource = null;
                clbFlowGroups.Items.Clear();
                return;
            }

            int course = Convert.ToInt32(cbFlowCourse.SelectedValue);
            DataTable flows = DataBase.Query(@"
SELECT DISTINCT ISNULL(NULLIF(LTRIM(RTRIM(Поток)), N''), Направление) AS Поток
FROM dbo.Группы
WHERE Курс = @course
ORDER BY Поток;",
                new SqlParameter("@course", course));

            cbFlowName.DisplayMember = "Поток";
            cbFlowName.ValueMember = "Поток";
            cbFlowName.DataSource = flows;

            if (cbFlowName.Items.Count > 0)
                cbFlowName.SelectedIndex = 0;

            LoadFlowGroups();
        }

        private void LoadFlowGroups()
        {
            clbFlowGroups.Items.Clear();

            if (cbFlowCourse.SelectedValue == null || cbFlowName.SelectedValue == null)
                return;

            int course = Convert.ToInt32(cbFlowCourse.SelectedValue);
            string flow = Convert.ToString(cbFlowName.SelectedValue);
            DataTable groups = DataBase.Query(@"
SELECT НазваниеГруппы
FROM dbo.Группы
WHERE Курс = @course
  AND ISNULL(NULLIF(LTRIM(RTRIM(Поток)), N''), Направление) = @flow
ORDER BY НазваниеГруппы;",
                new SqlParameter("@course", course),
                new SqlParameter("@flow", flow));

            foreach (DataRow row in groups.Rows)
                clbFlowGroups.Items.Add(Convert.ToString(row["НазваниеГруппы"]), true);
        }

        private void cbGenerationMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateGenerationScopeControls();
            if (!IsDirectionGenerationMode())
                LoadGroupsForScope();
            else
                ClearPlanGrid();
            UpdateDefaultScheduleName(false);
        }

        private void cbFlowCourse_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadFlowNames();
            LoadGroupsForScope();
            UpdateDefaultScheduleName(false);
        }

        private void cbFlowName_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadFlowGroups();
            LoadGroupsForScope();
            UpdateDefaultScheduleName(false);
        }

        private void cbDirectionName_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateDefaultScheduleName(false);
        }

        private bool IsFlowGenerationMode()
        {
            return string.Equals(Convert.ToString(cbGenerationMode.SelectedItem), FlowGenerationMode, StringComparison.Ordinal);
        }

        private bool IsDirectionGenerationMode()
        {
            return string.Equals(Convert.ToString(cbGenerationMode.SelectedItem), DirectionGenerationMode, StringComparison.Ordinal);
        }

        private void UpdateGenerationScopeControls()
        {
            bool flowMode = IsFlowGenerationMode();
            bool directionMode = IsDirectionGenerationMode();

            lblGroup.Enabled = !directionMode;
            cbGroup.Enabled = !directionMode;
            lblFlowCourse.Enabled = flowMode;
            cbFlowCourse.Enabled = flowMode;
            lblFlowName.Enabled = flowMode;
            cbFlowName.Enabled = flowMode;
            lblFlowGroups.Enabled = flowMode;
            clbFlowGroups.Enabled = flowMode;
            lblDirectionName.Enabled = directionMode;
            cbDirectionName.Enabled = directionMode;
        }

        private List<string> GetSelectedGenerationGroups()
        {
            if (!IsFlowGenerationMode())
                return cbGroup.SelectedValue == null
                    ? new List<string>()
                    : new List<string> { Convert.ToString(cbGroup.SelectedValue) };

            var groups = new List<string>();
            foreach (object item in clbFlowGroups.CheckedItems)
            {
                string group = Convert.ToString(item).Trim();
                if (!string.IsNullOrWhiteSpace(group))
                    groups.Add(group);
            }

            return groups.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        private List<string> FindGroupsWithoutPlan(IEnumerable<string> groups, int semester)
        {
            List<string> groupList = (groups ?? Enumerable.Empty<string>())
                .Where(group => !string.IsNullOrWhiteSpace(group))
                .Select(group => group.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (groupList.Count == 0)
                return new List<string>();

            string groupsCsv = string.Join("|", groupList);
            DataTable planGroups = DataBase.Query(@"
SELECT up.НазваниеГруппы
FROM dbo.УчебныйПлан up
WHERE up.Семестр = @sem
  AND CHARINDEX(N'|' + up.НазваниеГруппы + N'|', N'|' + @groupsCsv + N'|') > 0
GROUP BY up.НазваниеГруппы;",
                new SqlParameter("@sem", semester),
                new SqlParameter("@groupsCsv", groupsCsv));

            var groupsWithPlan = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (DataRow row in planGroups.Rows)
                groupsWithPlan.Add(Convert.ToString(row["НазваниеГруппы"]));

            return groupList
                .Where(group => !groupsWithPlan.Contains(group))
                .ToList();
        }

        private void RefreshGroupPlanAndWeeks()
        {
            if (cbGroup.SelectedValue == null)
            {
                ClearPlanGrid();
                return;
            }

            string group = cbGroup.SelectedValue.ToString();
            int semester = (int)nudSemester.Value;

            LoadWeeksForGroup(group, semester);
            LoadPlanForGroup(group, semester);
        }

        private void ClearPlanGrid()
        {
            _planTable = null;
            dgvPlan.DataSource = null;
            dgvPlan.Columns.Clear();
        }

        private void ApplyDefaultSemesterForSelectedGroup()
        {
            int? course = GetSelectedGroupCourse();
            if (course == null)
                return;

            int semester = Math.Max((int)nudSemester.Minimum, Math.Min((course.Value * 2) - 1, (int)nudSemester.Maximum));
            if ((int)nudSemester.Value == semester)
                return;

            _suppressPlanRefresh = true;
            try
            {
                nudSemester.Value = semester;
            }
            finally
            {
                _suppressPlanRefresh = false;
            }
        }

        private int? GetSelectedGroupCourse()
        {
            if (cbGroup.SelectedItem is DataRowView rowView && rowView.Row.Table.Columns.Contains("Курс"))
                return Convert.ToInt32(rowView["Курс"]);

            string group = cbGroup.SelectedValue == null ? string.Empty : Convert.ToString(cbGroup.SelectedValue);
            if (string.IsNullOrWhiteSpace(group))
                return null;

            object value = DataBase.ExecuteScalar(@"
SELECT TOP (1) Курс
FROM dbo.Группы
WHERE НазваниеГруппы = @group;",
                new SqlParameter("@group", group));

            if (value == null || value == DBNull.Value)
                return null;

            return Convert.ToInt32(value);
        }

        private void LoadWeeksForGroup(string group, int semester)
        {
            object w = DataBase.ExecuteScalar(@"
SELECT КоличествоНедель
FROM dbo.ГруппаСеместр
WHERE НазваниеГруппы = @g AND Семестр = @s;",
                new SqlParameter("@g", group),
                new SqlParameter("@s", semester));

            int weeks = (w == null || w == DBNull.Value) ? 16 : Convert.ToInt32(w);

            if (weeks < (int)nudWeeks.Minimum) weeks = (int)nudWeeks.Minimum;
            if (weeks > (int)nudWeeks.Maximum) weeks = (int)nudWeeks.Maximum;

            nudWeeks.Value = weeks;
        }

        private void SaveWeeksForGroup(string group, int semester, int weeks)
        {
            DataBase.ExecuteInTransaction((con, tx) =>
            {
                SaveWeeksForGroup(con, tx, group, semester, weeks);
            });
        }

        private static void SaveWeeksForGroup(SqlConnection con, SqlTransaction tx, string group, int semester, int weeks)
        {
            DataBase.ExecuteNonQuery(con, tx, @"
IF EXISTS (SELECT 1 FROM dbo.ГруппаСеместр WHERE НазваниеГруппы=@g AND Семестр=@s)
    UPDATE dbo.ГруппаСеместр SET КоличествоНедель=@w WHERE НазваниеГруппы=@g AND Семестр=@s
ELSE
    INSERT INTO dbo.ГруппаСеместр(НазваниеГруппы, Семестр, КоличествоНедель)
    VALUES(@g, @s, @w);",
                new SqlParameter("@g", group),
                new SqlParameter("@s", semester),
                new SqlParameter("@w", weeks));
        }

        private void LoadPlanForGroup(string group, int semester)
        {
            _planTable = DataBase.Query(@"
SELECT
    up.IDУчебныйПлан,
    up.IDПредмет,
    up.ТипЗанятия,
    up.IDПреподаватель,
    up.ЧасыВСеместре,
    ISNULL(up.ТребуемыйТипАудитории, N'') AS ТребуемыйТипАудитории,
    ISNULL(up.ТребуемаяАудитория, N'') AS ТребуемаяАудитория,
    up.ПотоковоеЗанятие,
    ISNULL(up.ШаблонНедель, N'') AS ШаблонНедель
FROM dbo.УчебныйПлан up
WHERE up.Семестр = @sem AND up.НазваниеГруппы = @group
ORDER BY up.ТипЗанятия;",
                new SqlParameter("@sem", semester),
                new SqlParameter("@group", group));

            LoadSubjectLookupForGroupAndSemester(group, semester, GetPlanSubjectIds());

            dgvPlan.AutoGenerateColumns = false;
            dgvPlan.Columns.Clear();

            var colSubject = new DataGridViewComboBoxColumn
            {
                Name = "IDПредмет",
                HeaderText = "Предмет",
                DataPropertyName = "IDПредмет",
                DataSource = _subjects,
                ValueMember = "IDПредмет",
                DisplayMember = "Название",
                FlatStyle = FlatStyle.Flat
            };
            dgvPlan.Columns.Add(colSubject);

            var colType = new DataGridViewComboBoxColumn
            {
                Name = "ТипЗанятия",
                HeaderText = "Тип занятия",
                DataPropertyName = "ТипЗанятия",
                DataSource = _types,
                ValueMember = "Код",
                DisplayMember = "Название",
                FlatStyle = FlatStyle.Flat
            };
            dgvPlan.Columns.Add(colType);

            var colTeacher = new DataGridViewComboBoxColumn
            {
                Name = "IDПреподаватель",
                HeaderText = "Преподаватель",
                DataPropertyName = "IDПреподаватель",
                ValueMember = "IDПреподаватель",
                DisplayMember = "ФИО",
                FlatStyle = FlatStyle.Flat
            };
            dgvPlan.Columns.Add(colTeacher);

            var colHours = new DataGridViewTextBoxColumn
            {
                Name = "ЧасыВСеместре",
                HeaderText = "Часы",
                DataPropertyName = "ЧасыВСеместре"
            };
            dgvPlan.Columns.Add(colHours);

            var colFlow = new DataGridViewCheckBoxColumn
            {
                Name = "ПотоковоеЗанятие",
                HeaderText = "Общее для потока",
                DataPropertyName = "ПотоковоеЗанятие"
            };
            dgvPlan.Columns.Add(colFlow);

            var colWeekPattern = new DataGridViewComboBoxColumn
            {
                Name = "ШаблонНедель",
                HeaderText = "Недели",
                DataPropertyName = "ШаблонНедель",
                DataSource = CreateWeekPatternLookupTable(),
                ValueMember = "ШаблонНедель",
                DisplayMember = "Название",
                FlatStyle = FlatStyle.Flat
            };
            dgvPlan.Columns.Add(colWeekPattern);

            var colRequiredRoomType = new DataGridViewComboBoxColumn
            {
                Name = "ТребуемыйТипАудитории",
                HeaderText = "Тип аудитории",
                DataPropertyName = "ТребуемыйТипАудитории",
                DataSource = _roomTypes,
                ValueMember = "ТипАудитории",
                DisplayMember = "Название",
                FlatStyle = FlatStyle.Flat
            };
            dgvPlan.Columns.Add(colRequiredRoomType);

            var colRequiredRoom = new DataGridViewComboBoxColumn
            {
                Name = "ТребуемаяАудитория",
                HeaderText = "Аудитория",
                DataPropertyName = "ТребуемаяАудитория",
                DataSource = _rooms,
                ValueMember = "НомерАудитории",
                DisplayMember = "Название",
                FlatStyle = FlatStyle.Flat
            };
            dgvPlan.Columns.Add(colRequiredRoom);

            dgvPlan.DataSource = _planTable;

            dgvPlan.AllowUserToAddRows = false;
            dgvPlan.RowHeadersVisible = false;
            dgvPlan.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvPlan.MultiSelect = false;
            dgvPlan.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            RefreshLessonTypeCells();
            RefreshTeacherCells(true);
        }

        private void LoadSubjectLookupForGroupAndSemester(string group, int semester, IEnumerable<int> includeSubjectIds)
        {
            _subjects = DataBase.Query(@"
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
      AND ex.НазваниеГруппы = @group
)
ORDER BY Название;",
                new SqlParameter("@semester", semester),
                new SqlParameter("@group", group),
                new SqlParameter("@profile", DataBase.GetGroupDirection(group) ?? string.Empty),
                new SqlParameter("@allProfilesMarker", "*"));

            if (includeSubjectIds == null)
                return;

            var existingIds = new HashSet<int>();
            foreach (DataRow row in _subjects.Rows)
                existingIds.Add(Convert.ToInt32(row["IDПредмет"]));

            foreach (int subjectId in includeSubjectIds)
            {
                if (existingIds.Contains(subjectId))
                    continue;

                DataRow sourceRow = FindSubjectRow(subjectId);
                if (sourceRow == null)
                    continue;

                DataRow newRow = _subjects.NewRow();
                newRow["IDПредмет"] = subjectId;
                newRow["Название"] = Convert.ToString(sourceRow["Название"]) + " (не назначен для профиля/семестра)";
                _subjects.Rows.Add(newRow);
                existingIds.Add(subjectId);
            }
        }

        private IEnumerable<int> GetPlanSubjectIds()
        {
            var result = new List<int>();

            if (_planTable == null)
                return result;

            foreach (DataRow row in _planTable.Rows)
            {
                if (row.RowState == DataRowState.Deleted || row["IDПредмет"] == DBNull.Value)
                    continue;

                int subjectId = Convert.ToInt32(row["IDПредмет"]);
                if (!result.Contains(subjectId))
                    result.Add(subjectId);
            }

            return result;
        }

        private DataRow FindSubjectRow(int subjectId)
        {
            if (_allSubjects == null)
                return null;

            foreach (DataRow row in _allSubjects.Rows)
            {
                if (Convert.ToInt32(row["IDПредмет"]) == subjectId)
                    return row;
            }

            return null;
        }

        private static DataTable CreateEmptySubjectTable()
        {
            var dt = new DataTable();
            dt.Columns.Add("IDПредмет", typeof(int));
            dt.Columns.Add("Название", typeof(string));
            return dt;
        }

        private static DataTable CreateWeekPatternLookupTable()
        {
            var dt = new DataTable();
            dt.Columns.Add("ШаблонНедель", typeof(string));
            dt.Columns.Add("Название", typeof(string));
            dt.Rows.Add(string.Empty, "Авто");
            dt.Rows.Add("Каждую неделю", "Каждую неделю");
            dt.Rows.Add("1,5,9,13", "1,5,9,13");
            dt.Rows.Add("2,6,10,14", "2,6,10,14");
            dt.Rows.Add("3,7,11,15", "3,7,11,15");
            dt.Rows.Add("4,8,12,16", "4,8,12,16");
            dt.Rows.Add("1-11", "1-11");
            return dt;
        }

        private void dgvPlan_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            // чтобы не падало на пустых значениях/несоответствиях
            e.ThrowException = false;
        }

        private void dgvPlan_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (dgvPlan.IsCurrentCellDirty)
                dgvPlan.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        private void dgvPlan_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
                return;

            if (dgvPlan.Columns[e.ColumnIndex].Name == "IDПредмет")
            {
                ConfigureTeacherCell(e.RowIndex, false);
                ConfigureLessonTypeCell(e.RowIndex);
            }
            else if (dgvPlan.Columns[e.ColumnIndex].Name == "IDПреподаватель")
            {
                ConfigureLessonTypeCell(e.RowIndex);
            }
        }

        private void RefreshLessonTypeCells()
        {
            foreach (DataGridViewRow row in dgvPlan.Rows)
            {
                if (row.IsNewRow)
                    continue;

                ConfigureLessonTypeCell(row.Index);
            }
        }

        private void ConfigureLessonTypeCell(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= dgvPlan.Rows.Count)
                return;

            DataGridViewRow row = dgvPlan.Rows[rowIndex];
            if (row.IsNewRow)
                return;

            var typeCell = row.Cells["ТипЗанятия"] as DataGridViewComboBoxCell;
            if (typeCell == null)
                return;

            int? teacherId = GetNullableInt(row.Cells["IDПреподаватель"].Value);
            int? subjectId = GetNullableInt(row.Cells["IDПредмет"].Value);
            string currentType = Convert.ToString(row.Cells["ТипЗанятия"].Value);
            bool allowPhysicalType = string.Equals(currentType, PhysicalLessonType, StringComparison.OrdinalIgnoreCase)
                || (subjectId.HasValue && DataBase.IsPhysicalEducationSubject(subjectId.Value));
            DataTable lessonTypes = CreateLessonTypeTable(allowPhysicalType);

            typeCell.DataSource = lessonTypes;
            typeCell.ValueMember = "Код";
            typeCell.DisplayMember = "Название";
            typeCell.FlatStyle = FlatStyle.Flat;

            if (!string.IsNullOrWhiteSpace(currentType) && ContainsLessonType(lessonTypes, currentType))
                typeCell.Value = currentType;
            else if (lessonTypes.Rows.Count > 0)
                typeCell.Value = lessonTypes.Rows[0]["Код"];
            else
                typeCell.Value = DBNull.Value;
        }

        private static DataTable CreateLessonTypeTable(bool includePhysicalType)
        {
            var dt = new DataTable();
            dt.Columns.Add("Код", typeof(string));
            dt.Columns.Add("Название", typeof(string));

            if (includePhysicalType)
            {
                dt.Rows.Add(PhysicalLessonType, string.Empty);
            }
            else
            {
                dt.Rows.Add("ЛК", "Лекция");
                dt.Rows.Add("У", "Упражнение");
                dt.Rows.Add("ЛБ", "Лабораторная");
                dt.Rows.Add("КР", "Контрольная");
            }

            return dt;
        }

        private static bool ContainsLessonType(DataTable lessonTypes, string type)
        {
            foreach (DataRow row in lessonTypes.Rows)
            {
                if (string.Equals(Convert.ToString(row["Код"]), type, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private void RefreshTeacherCells(bool preserveCurrentTeacher)
        {
            foreach (DataGridViewRow row in dgvPlan.Rows)
            {
                if (row.IsNewRow)
                    continue;

                ConfigureTeacherCell(row.Index, preserveCurrentTeacher);
            }
        }

        private void ConfigureTeacherCell(int rowIndex, bool preserveCurrentTeacher)
        {
            if (rowIndex < 0 || rowIndex >= dgvPlan.Rows.Count)
                return;

            DataGridViewRow row = dgvPlan.Rows[rowIndex];
            if (row.IsNewRow)
                return;

            var teacherCell = row.Cells["IDПреподаватель"] as DataGridViewComboBoxCell;
            if (teacherCell == null)
                return;

            int? subjectId = GetNullableInt(row.Cells["IDПредмет"].Value);
            int? currentTeacherId = GetNullableInt(row.Cells["IDПреподаватель"].Value);
            bool isPhysicalSubject = subjectId.HasValue && DataBase.IsPhysicalEducationSubject(subjectId.Value);

            DataTable teachers = CreateEmptyTeacherTable();
            if (subjectId.HasValue && !isPhysicalSubject)
            {
                teachers = DataBase.GetTeachersForSubject(
                    subjectId.Value,
                    preserveCurrentTeacher ? currentTeacherId : null);
            }

            teacherCell.DataSource = teachers;
            teacherCell.DisplayMember = "ФИО";
            teacherCell.ValueMember = "IDПреподаватель";
            teacherCell.FlatStyle = FlatStyle.Flat;

            if (currentTeacherId.HasValue && ContainsTeacherId(teachers, currentTeacherId.Value))
                teacherCell.Value = currentTeacherId.Value;
            else if (!isPhysicalSubject && teachers.Rows.Count > 0)
                teacherCell.Value = teachers.Rows[0]["IDПреподаватель"];
            else
                teacherCell.Value = DBNull.Value;
        }

        private static DataTable CreateEmptyTeacherTable()
        {
            var dt = new DataTable();
            dt.Columns.Add("IDПреподаватель", typeof(int));
            dt.Columns.Add("ФИО", typeof(string));
            return dt;
        }

        private static int? GetNullableInt(object value)
        {
            if (value == null || value == DBNull.Value)
                return null;

            return Convert.ToInt32(value);
        }

        private static bool ContainsTeacherId(DataTable teachers, int teacherId)
        {
            foreach (DataRow row in teachers.Rows)
            {
                if (Convert.ToInt32(row["IDПреподаватель"]) == teacherId)
                    return true;
            }

            return false;
        }

        // ---------------------------------------
        // CRUD: добавить/удалить/сохранить
        // ---------------------------------------
        private void btnAddPlanRow_Click(object sender, EventArgs e)
        {
            if (_planTable == null) return;

            object defaultSubjectId = GetDefaultAllowedSubjectId();
            if (defaultSubjectId == DBNull.Value)
            {
                MessageBox.Show("Для выбранной группы и семестра нет доступных предметов. Настройте предметы на форме \"Предметы\".");
                return;
            }

            DataRow r = _planTable.NewRow();
            r["IDУчебныйПлан"] = DBNull.Value;

            object subjectId = defaultSubjectId;
            r["IDПредмет"] = subjectId;
            r["ТипЗанятия"] = "ЛК";
            r["IDПреподаватель"] = GetDefaultTeacherIdForSubject(subjectId);
            r["ЧасыВСеместре"] = 16;
            r["ПотоковоеЗанятие"] = true;
            r["ШаблонНедель"] = string.Empty;
            r["ТребуемыйТипАудитории"] = string.Empty;
            r["ТребуемаяАудитория"] = string.Empty;

            _planTable.Rows.Add(r);
            ConfigureTeacherCell(_planTable.Rows.Count - 1, false);
            ConfigureLessonTypeCell(_planTable.Rows.Count - 1);
        }

        private void btnDeletePlanRow_Click(object sender, EventArgs e)
        {
            if (dgvPlan.CurrentRow == null) return;

            var rowView = dgvPlan.CurrentRow.DataBoundItem as DataRowView;
            if (rowView == null) return;

            object idObj = rowView["IDУчебныйПлан"];
            if (idObj != null && idObj != DBNull.Value)
            {
                int planId = Convert.ToInt32(idObj);

                DataBase.ExecuteInTransaction((con, tx) =>
                {
                    DataBase.ExecuteNonQuery(con, tx, "DELETE FROM dbo.ПланНедели WHERE IDУчебныйПлан=@id;",
                        new SqlParameter("@id", planId));

                    DataBase.ExecuteNonQuery(con, tx, "DELETE FROM dbo.УчебныйПлан WHERE IDУчебныйПлан=@id;",
                        new SqlParameter("@id", planId));
                });
            }

            rowView.Row.Delete();
            _planTable.AcceptChanges();

            RefreshGroupPlanAndWeeks();
        }

        private void btnSavePlan_Click(object sender, EventArgs e)
        {
            try
            {
                SaveAll();
                MessageBox.Show("Сохранено.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка сохранения:\n" + ex.Message);
            }
        }

        private void SaveAll()
        {
            if (cbGroup.SelectedValue == null) throw new Exception("Не выбрана группа.");

            string group = cbGroup.SelectedValue.ToString();
            int semester = (int)nudSemester.Value;
            int weeks = (int)nudWeeks.Value;

            DataBase.ExecuteInTransaction((con, tx) =>
            {
                SaveWeeksForGroup(con, tx, group, semester, weeks);
                SavePlanAll(con, tx, group, semester);
            });
        }

        private void SavePlanAll(SqlConnection con, SqlTransaction tx, string group, int semester)
        {
            if (_planTable == null) return;

            dgvPlan.EndEdit();
            dgvPlan.CommitEdit(DataGridViewDataErrorContexts.Commit);

            for (int i = 0; i < _planTable.Rows.Count; i++)
            {
                DataRow r = _planTable.Rows[i];
                if (r.RowState == DataRowState.Deleted) continue;

                if (r["IDПредмет"] == DBNull.Value)
                    throw new Exception("В строке " + (i + 1) + " учебного плана не выбран предмет.");

                int subjectId = Convert.ToInt32(r["IDПредмет"]);
                string type = Convert.ToString(r["ТипЗанятия"]);

                if (!DataBase.SubjectAllowedForGroupAndSemester(subjectId, group, semester))
                    throw new Exception("В строке " + (i + 1) + " предмет \"" + GetSubjectName(subjectId) + "\" не назначен для профиля группы и этого семестра.");

                int? teacherId = GetNullableInt(r["IDПреподаватель"]);

                if (type == PhysicalLessonType)
                {
                    if (!DataBase.IsPhysicalEducationSubject(subjectId))
                        throw new Exception("В строке " + (i + 1) + " тип занятия \"Физкультура\" можно выбрать только для физкультуры.");
                }
                else
                {
                    if (!teacherId.HasValue)
                        throw new Exception("Для предмета \"" + GetSubjectName(subjectId) + "\" не выбран преподаватель.");

                    if (!DataBase.TeacherCanTeachSubject(teacherId.Value, subjectId))
                        throw new Exception("В строке " + (i + 1) + " выбран преподаватель, который не закреплен за предметом \"" + GetSubjectName(subjectId) + "\".");
                }

                int hours;
                if (!int.TryParse(Convert.ToString(r["ЧасыВСеместре"]), out hours) || hours <= 0)
                    throw new Exception("В строке " + (i + 1) + " указано некорректное количество часов.");

                if (type == "ЛБ")
                {
                    if (hours % 4 != 0)
                        throw new Exception("В строке " + (i + 1) + " для лабораторной работы часы должны быть кратны 4.");
                }
                else
                {
                    if (hours % 2 != 0)
                        throw new Exception("В строке " + (i + 1) + " часы должны быть кратны 2.");
                }

                string requiredRoomType = Convert.ToString(r["ТребуемыйТипАудитории"]).Trim();
                string requiredRoom = Convert.ToString(r["ТребуемаяАудитория"]).Trim();
                bool isFlowLesson = r.Table.Columns.Contains("ПотоковоеЗанятие")
                    && r["ПотоковоеЗанятие"] != DBNull.Value
                    && Convert.ToBoolean(r["ПотоковоеЗанятие"]);
                string weekPattern = r.Table.Columns.Contains("ШаблонНедель")
                    ? Convert.ToString(r["ШаблонНедель"]).Trim()
                    : string.Empty;

                ValidateRoomRequirement(requiredRoomType, requiredRoom, i + 1);

                object idObj = r["IDУчебныйПлан"];

                if (idObj == DBNull.Value || idObj == null)
                {
                    object newId = DataBase.ExecuteScalar(con, tx, @"
INSERT INTO dbo.УчебныйПлан
    (НазваниеГруппы, IDПредмет, IDПреподаватель, Семестр, ТипЗанятия, ЧасыВСеместре, ТребуемыйТипАудитории, ТребуемаяАудитория, ПотоковоеЗанятие, ШаблонНедель)
VALUES
    (@g, @subj, @t, @sem, @type, @h, @requiredRoomType, @requiredRoom, @isFlowLesson, @weekPattern);
SELECT SCOPE_IDENTITY();",
                        new SqlParameter("@g", group),
                        new SqlParameter("@subj", subjectId),
                        new SqlParameter("@t", (object)teacherId ?? DBNull.Value),
                        new SqlParameter("@sem", semester),
                        new SqlParameter("@type", type),
                        new SqlParameter("@h", hours),
                        new SqlParameter("@requiredRoomType", RoomRequirementDbValue(requiredRoomType)),
                        new SqlParameter("@requiredRoom", RoomRequirementDbValue(requiredRoom)),
                        new SqlParameter("@isFlowLesson", isFlowLesson),
                        new SqlParameter("@weekPattern", RoomRequirementDbValue(weekPattern)));

                    r["IDУчебныйПлан"] = Convert.ToInt32(newId);
                }
                else
                {
                    DataBase.ExecuteNonQuery(con, tx, @"
UPDATE dbo.УчебныйПлан
SET IDПредмет=@subj,
    ТипЗанятия=@type,
    IDПреподаватель=@t,
    ЧасыВСеместре=@h,
    ТребуемыйТипАудитории=@requiredRoomType,
    ТребуемаяАудитория=@requiredRoom,
    ПотоковоеЗанятие=@isFlowLesson,
    ШаблонНедель=@weekPattern
WHERE IDУчебныйПлан=@id;",
                        new SqlParameter("@subj", subjectId),
                        new SqlParameter("@type", type),
                        new SqlParameter("@t", (object)teacherId ?? DBNull.Value),
                        new SqlParameter("@h", hours),
                        new SqlParameter("@requiredRoomType", RoomRequirementDbValue(requiredRoomType)),
                        new SqlParameter("@requiredRoom", RoomRequirementDbValue(requiredRoom)),
                        new SqlParameter("@isFlowLesson", isFlowLesson),
                        new SqlParameter("@weekPattern", RoomRequirementDbValue(weekPattern)),
                        new SqlParameter("@id", Convert.ToInt32(idObj)));
                }
            }

            _planTable.AcceptChanges();
        }

        private void ValidateRoomRequirement(string requiredRoomType, string requiredRoom, int rowNumber)
        {
            if (string.IsNullOrWhiteSpace(requiredRoomType) || string.IsNullOrWhiteSpace(requiredRoom))
                return;

            string actualType = null;
            foreach (DataRow row in _rooms.Rows)
            {
                if (string.Equals(Convert.ToString(row["НомерАудитории"]), requiredRoom, StringComparison.OrdinalIgnoreCase))
                {
                    actualType = Convert.ToString(row["ТипАудитории"]);
                    break;
                }
            }

            if (actualType == null)
                throw new Exception("В строке " + rowNumber + " выбранная аудитория не найдена.");

            if (!string.Equals(actualType, requiredRoomType, StringComparison.OrdinalIgnoreCase))
                throw new Exception("В строке " + rowNumber + " выбранная аудитория не относится к выбранному типу аудитории.");
        }

        private static object RoomRequirementDbValue(string value)
        {
            string trimmed = (value ?? string.Empty).Trim();
            return string.IsNullOrWhiteSpace(trimmed) ? (object)DBNull.Value : trimmed;
        }

        private object GetDefaultTeacherIdForSubject(object subjectIdValue)
        {
            int? subjectId = GetNullableInt(subjectIdValue);
            if (!subjectId.HasValue)
                return DBNull.Value;

            DataTable teachers = DataBase.GetTeachersForSubject(subjectId.Value);
            if (teachers.Rows.Count == 0)
                return DBNull.Value;

            return teachers.Rows[0]["IDПреподаватель"];
        }

        private object GetDefaultAllowedSubjectId()
        {
            if (_subjects == null || cbGroup.SelectedValue == null)
                return DBNull.Value;

            string group = Convert.ToString(cbGroup.SelectedValue);
            int semester = (int)nudSemester.Value;

            foreach (DataRow row in _subjects.Rows)
            {
                int subjectId = Convert.ToInt32(row["IDПредмет"]);
                if (DataBase.SubjectAllowedForGroupAndSemester(subjectId, group, semester))
                    return subjectId;
            }

            return DBNull.Value;
        }

        private string GetSubjectName(int subjectId)
        {
            foreach (DataRow row in _subjects.Rows)
            {
                if (Convert.ToInt32(row["IDПредмет"]) == subjectId)
                    return Convert.ToString(row["Название"]);
            }

            return "ID " + subjectId;
        }

        // ---------------------------------------
        // Генерация расписания
        // ---------------------------------------
        private async void btnGenerate_Click(object sender, EventArgs e)
        {
            try
            {
                if (IsDirectionGenerationMode())
                {
                    await GenerateDirectionSchedulesAsync();
                    return;
                }

                SaveAll(); // сохраняем план и недели перед генерацией

                string scheduleName = txtScheduleName.Text.Trim();
                if (string.IsNullOrWhiteSpace(scheduleName))
                {
                    MessageBox.Show("Введите название расписания.");
                    return;
                }

                List<string> groupsToGenerate = GetSelectedGenerationGroups();
                if (groupsToGenerate.Count == 0)
                {
                    MessageBox.Show("Выберите хотя бы одну группу для генерации.");
                    return;
                }

                int semester = (int)nudSemester.Value;
                List<string> groupsWithoutPlan = FindGroupsWithoutPlan(groupsToGenerate, semester);
                if (groupsWithoutPlan.Count > 0)
                {
                    MessageBox.Show(
                        "Для следующих групп нет учебного плана за " + semester + " семестр:\n" +
                        string.Join("\n", groupsWithoutPlan) +
                        "\n\nЗаполните учебный план для этих групп или уберите их из выбора потока.",
                        "Нельзя сгенерировать расписание");
                    return;
                }

                int weeksForGroup = (int)nudWeeks.Value;
                int maxPairsPerDay = (int)nudMaxPairsPerDay.Value;

                int perWeekLimit = (int)nudPairsPerPlanRow.Value; // лимит по записи плана

                SetGenerationUiState(true);

                GenerationWorkResult workResult = await Task.Run(() =>
                {
                    string groupsCsv = string.Join("|", groupsToGenerate);
                    DataTable plan = DataBase.Query(@"
SELECT
    up.IDУчебныйПлан,
    up.НазваниеГруппы,
    up.IDПредмет,
    up.ТипЗанятия,
    up.IDПреподаватель,
    up.ЧасыВСеместре,
    up.ТребуемыйТипАудитории,
    up.ТребуемаяАудитория,
    up.ПотоковоеЗанятие,
    up.ШаблонНедель
FROM dbo.УчебныйПлан up
WHERE up.Семестр = @sem
  AND CHARINDEX(N'|' + up.НазваниеГруппы + N'|', N'|' + @groupsCsv + N'|') > 0
ORDER BY up.ТипЗанятия, up.IDПредмет, up.НазваниеГруппы;",
                        new SqlParameter("@sem", semester),
                        new SqlParameter("@groupsCsv", groupsCsv));

                    if (plan.Rows.Count == 0)
                        return GenerationWorkResult.Empty();

                    int scheduleId = GetOrCreateSchedule(scheduleName, semester);
                    var generator = new FlowScheduleGenerator(new FlowScheduleGenerationRequest
                    {
                        PlanTable = plan,
                        ScheduleId = scheduleId,
                        Groups = groupsToGenerate,
                        Semester = semester,
                        Weeks = weeksForGroup,
                        MaxPairsPerDay = maxPairsPerDay,
                        PerWeekLimit = perWeekLimit,
                        RoomsByType = LoadRoomsByType()
                    });

                    CombinedScheduleGenerationResult generationResult = generator.Generate();
                    return GenerationWorkResult.Success(scheduleId, generationResult);
                });

                if (workResult.IsPlanEmpty)
                {
                    MessageBox.Show("Учебный план пуст.");
                    return;
                }

                LoadScheduleToGrid(workResult.ScheduleId, week: 1);

                MessageBox.Show(
                    $"Готово!\nСоздано: {workResult.GenerationResult.InsertedPairs}\nНе удалось: {workResult.GenerationResult.FailedPairs}",
                    "Генерация");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка генерации:\n" + ex.Message);
            }
            finally
            {
                SetGenerationUiState(false);
            }
        }

        private async Task GenerateDirectionSchedulesAsync()
        {
            string scheduleName = txtScheduleName.Text.Trim();
            if (string.IsNullOrWhiteSpace(scheduleName))
            {
                MessageBox.Show("Введите название расписания.");
                return;
            }

            string direction = cbDirectionName.SelectedValue == null ? string.Empty : Convert.ToString(cbDirectionName.SelectedValue).Trim();
            if (string.IsNullOrWhiteSpace(direction))
            {
                MessageBox.Show("Выберите направление.");
                return;
            }

            int selectedSemester = (int)nudSemester.Value;
            bool oddSeason = selectedSemester % 2 != 0;
            int defaultWeeks = (int)nudWeeks.Value;
            int maxPairsPerDay = (int)nudMaxPairsPerDay.Value;
            int perWeekLimit = (int)nudPairsPerPlanRow.Value;

            List<DirectionGenerationScope> scopes = LoadDirectionGenerationScopes(direction, oddSeason, defaultWeeks);
            if (scopes.Count == 0)
            {
                MessageBox.Show("Для выбранного направления не найдены группы 1-4 курса.", "Генерация направления");
                return;
            }

            var missingPlanLines = new List<string>();
            foreach (DirectionGenerationScope scope in scopes)
            {
                List<string> missingGroups = FindGroupsWithoutPlan(scope.Groups, scope.Semester);
                if (missingGroups.Count > 0)
                    missingPlanLines.Add(scope.Course + " курс, " + scope.Semester + " семестр: " + string.Join(", ", missingGroups));
            }

            if (missingPlanLines.Count > 0)
            {
                MessageBox.Show(
                    "Нельзя сгенерировать направление: нет учебного плана для групп:\n" +
                    string.Join("\n", missingPlanLines),
                    "Генерация направления");
                return;
            }

            SetGenerationUiState(true);

            DirectionGenerationResult result = await Task.Run(() =>
                GenerateDirectionSchedules(scheduleName, scopes, maxPairsPerDay, perWeekLimit));

            if (result.LastScheduleId > 0)
                LoadScheduleToGrid(result.LastScheduleId, week: 1);

            MessageBox.Show(
                $"Готово!\nСоздано расписаний: {result.ScheduleCount}\nСоздано пар: {result.InsertedPairs}\nНе удалось: {result.FailedPairs}",
                "Генерация направления");
        }

        private List<DirectionGenerationScope> LoadDirectionGenerationScopes(string direction, bool oddSeason, int defaultWeeks)
        {
            DataTable dt = DataBase.Query(@"
SELECT
    Курс,
    НазваниеГруппы
FROM dbo.Группы
WHERE Курс BETWEEN 1 AND 4
  AND ISNULL(NULLIF(LTRIM(RTRIM(Поток)), N''), Направление) = @direction
ORDER BY Курс, НазваниеГруппы;",
                new SqlParameter("@direction", direction));

            var groupsByCourse = new Dictionary<int, List<string>>();
            foreach (DataRow row in dt.Rows)
            {
                int course = Convert.ToInt32(row["Курс"]);
                string group = Convert.ToString(row["НазваниеГруппы"]).Trim();
                if (string.IsNullOrWhiteSpace(group))
                    continue;

                List<string> groups;
                if (!groupsByCourse.TryGetValue(course, out groups))
                {
                    groups = new List<string>();
                    groupsByCourse[course] = groups;
                }

                groups.Add(group);
            }

            var result = new List<DirectionGenerationScope>();
            for (int course = 1; course <= 4; course++)
            {
                List<string> groups;
                if (!groupsByCourse.TryGetValue(course, out groups) || groups.Count == 0)
                    continue;

                int semester = (course * 2) - (oddSeason ? 1 : 0);
                int weeks = LoadMostCommonWeeks(groups, semester, defaultWeeks);
                result.Add(new DirectionGenerationScope(course, semester, groups, weeks));
            }

            return result;
        }

        private static int LoadMostCommonWeeks(IEnumerable<string> groups, int semester, int defaultWeeks)
        {
            List<string> groupList = (groups ?? Enumerable.Empty<string>())
                .Where(group => !string.IsNullOrWhiteSpace(group))
                .Select(group => group.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (groupList.Count == 0)
                return defaultWeeks;

            DataTable dt = DataBase.Query(@"
SELECT TOP 1 КоличествоНедель
FROM dbo.ГруппаСеместр
WHERE Семестр = @semester
  AND CHARINDEX(N'|' + НазваниеГруппы + N'|', N'|' + @groupsCsv + N'|') > 0
GROUP BY КоличествоНедель
ORDER BY COUNT(*) DESC, КоличествоНедель DESC;",
                new SqlParameter("@semester", semester),
                new SqlParameter("@groupsCsv", string.Join("|", groupList)));

            if (dt.Rows.Count == 0)
                return defaultWeeks;

            return Convert.ToInt32(dt.Rows[0]["КоличествоНедель"]);
        }

        private DirectionGenerationResult GenerateDirectionSchedules(
            string baseScheduleName,
            IEnumerable<DirectionGenerationScope> scopes,
            int maxPairsPerDay,
            int perWeekLimit)
        {
            var result = new DirectionGenerationResult();
            List<DirectionGenerationScope> scopeList = (scopes ?? Enumerable.Empty<DirectionGenerationScope>())
                .OrderBy(scope => scope.Course)
                .ToList();
            Dictionary<string, List<string>> roomsByType = LoadRoomsByType();
            var scheduleIdsByScope = new Dictionary<DirectionGenerationScope, int>();

            foreach (DirectionGenerationScope scope in scopeList)
            {
                string scheduleName = BuildDirectionCourseScheduleName(baseScheduleName, scope.Course, scope.Semester);
                scheduleIdsByScope[scope] = GetOrCreateSchedule(scheduleName, scope.Semester);
            }

            List<int> ignoredScheduleIds = scheduleIdsByScope.Values
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            foreach (DirectionGenerationScope scope in scopeList)
            {
                SaveWeeksForGroups(scope.Groups, scope.Semester, scope.Weeks);

                DataTable plan = LoadPlanForGroups(scope.Groups, scope.Semester);
                if (plan.Rows.Count == 0)
                    continue;

                int scheduleId = scheduleIdsByScope[scope];

                var generator = new FlowScheduleGenerator(new FlowScheduleGenerationRequest
                {
                    PlanTable = plan,
                    ScheduleId = scheduleId,
                    Groups = scope.Groups,
                    Semester = scope.Semester,
                    Weeks = scope.Weeks,
                    MaxPairsPerDay = maxPairsPerDay,
                    PerWeekLimit = perWeekLimit,
                    RoomsByType = roomsByType,
                    IgnoredScheduleIds = ignoredScheduleIds
                });

                CombinedScheduleGenerationResult generationResult = generator.Generate();
                result.ScheduleCount++;
                result.LastScheduleId = scheduleId;
                result.InsertedPairs += generationResult.InsertedPairs;
                result.FailedPairs += generationResult.FailedPairs;
            }

            return result;
        }

        private static DataTable LoadPlanForGroups(IEnumerable<string> groups, int semester)
        {
            string groupsCsv = string.Join("|", groups ?? Enumerable.Empty<string>());
            return DataBase.Query(@"
SELECT
    up.IDУчебныйПлан,
    up.НазваниеГруппы,
    up.IDПредмет,
    up.ТипЗанятия,
    up.IDПреподаватель,
    up.ЧасыВСеместре,
    up.ТребуемыйТипАудитории,
    up.ТребуемаяАудитория,
    up.ПотоковоеЗанятие,
    up.ШаблонНедель
FROM dbo.УчебныйПлан up
WHERE up.Семестр = @sem
  AND CHARINDEX(N'|' + up.НазваниеГруппы + N'|', N'|' + @groupsCsv + N'|') > 0
ORDER BY up.ТипЗанятия, up.IDПредмет, up.НазваниеГруппы;",
                new SqlParameter("@sem", semester),
                new SqlParameter("@groupsCsv", groupsCsv));
        }

        private static void SaveWeeksForGroups(IEnumerable<string> groups, int semester, int weeks)
        {
            DataBase.ExecuteInTransaction((con, tx) =>
            {
                foreach (string group in groups ?? Enumerable.Empty<string>())
                    SaveWeeksForGroup(con, tx, group, semester, weeks);
            });
        }

        private static string BuildDirectionCourseScheduleName(string baseScheduleName, int course, int semester)
        {
            return $"{baseScheduleName} - {course} курс ({semester} семестр)";
        }

        private void UpdateDefaultScheduleName(bool force)
        {
            if (!IsDirectionGenerationMode() && cbGroup.SelectedValue == null)
                return;

            if (!force && !_scheduleNameIsAuto && !string.IsNullOrWhiteSpace(txtScheduleName.Text))
                return;

            int semester = (int)nudSemester.Value;

            _suppressScheduleNameTextChanged = true;
            if (IsDirectionGenerationMode())
                txtScheduleName.Text = BuildDefaultDirectionScheduleName(semester);
            else if (IsFlowGenerationMode())
                txtScheduleName.Text = BuildDefaultFlowScheduleName(semester);
            else
                txtScheduleName.Text = BuildDefaultScheduleName(Convert.ToString(cbGroup.SelectedValue).Trim(), semester);
            _suppressScheduleNameTextChanged = false;
            _scheduleNameIsAuto = true;
        }

        private static string BuildDefaultScheduleName(string group, int semester)
        {
            return $"{group} - {semester} учебный семестр";
        }

        private string BuildDefaultFlowScheduleName(int semester)
        {
            string course = cbFlowCourse.SelectedValue == null ? string.Empty : Convert.ToString(cbFlowCourse.SelectedValue);
            string flow = cbFlowName.SelectedValue == null ? string.Empty : Convert.ToString(cbFlowName.SelectedValue);

            if (string.IsNullOrWhiteSpace(course) || string.IsNullOrWhiteSpace(flow))
                return $"Поток - {semester} учебный семестр";

            return $"{course} курс {flow} - {semester} учебный семестр";
        }

        private string BuildDefaultDirectionScheduleName(int semester)
        {
            string direction = cbDirectionName.SelectedValue == null ? string.Empty : Convert.ToString(cbDirectionName.SelectedValue);
            string season = semester % 2 == 0 ? "весна" : "осень";

            if (string.IsNullOrWhiteSpace(direction))
                return $"Направление 1-4 курс - {season}";

            return $"{direction} 1-4 курс - {season}";
        }

        private void SetGenerationUiState(bool isGenerating)
        {
            btnGenerate.Enabled = !isGenerating;
            btnSavePlan.Enabled = !isGenerating;
            btnAddPlanRow.Enabled = !isGenerating;
            btnDeletePlanRow.Enabled = !isGenerating;
            btnClose.Enabled = !isGenerating;
            cbGroup.Enabled = !isGenerating && !IsDirectionGenerationMode();
            cbGenerationMode.Enabled = !isGenerating;
            cbFlowCourse.Enabled = !isGenerating && IsFlowGenerationMode();
            cbFlowName.Enabled = !isGenerating && IsFlowGenerationMode();
            clbFlowGroups.Enabled = !isGenerating && IsFlowGenerationMode();
            cbDirectionName.Enabled = !isGenerating && IsDirectionGenerationMode();
            nudSemester.Enabled = !isGenerating;
            nudWeeks.Enabled = !isGenerating;
            nudMaxPairsPerDay.Enabled = !isGenerating;
            nudPairsPerPlanRow.Enabled = !isGenerating;
            txtScheduleName.Enabled = !isGenerating;
            dgvPlan.Enabled = !isGenerating;

            UseWaitCursor = isGenerating;
            Cursor = isGenerating ? Cursors.WaitCursor : Cursors.Default;
            btnGenerate.Text = isGenerating ? "Генерация..." : "Сгенерировать";
        }

        private sealed class DirectionGenerationScope
        {
            public DirectionGenerationScope(int course, int semester, List<string> groups, int weeks)
            {
                Course = course;
                Semester = semester;
                Groups = groups ?? new List<string>();
                Weeks = weeks;
            }

            public int Course { get; private set; }
            public int Semester { get; private set; }
            public List<string> Groups { get; private set; }
            public int Weeks { get; private set; }
        }

        private sealed class DirectionGenerationResult
        {
            public int ScheduleCount { get; set; }
            public int LastScheduleId { get; set; }
            public int InsertedPairs { get; set; }
            public int FailedPairs { get; set; }
        }

        private sealed class GenerationWorkResult
        {
            public bool IsPlanEmpty { get; private set; }
            public int ScheduleId { get; private set; }
            public CombinedScheduleGenerationResult GenerationResult { get; private set; }

            public static GenerationWorkResult Empty()
            {
                return new GenerationWorkResult
                {
                    IsPlanEmpty = true
                };
            }

            public static GenerationWorkResult Success(int scheduleId, CombinedScheduleGenerationResult generationResult)
            {
                return new GenerationWorkResult
                {
                    ScheduleId = scheduleId,
                    GenerationResult = generationResult
                };
            }
        }

        private int GetOrCreateSchedule(string name, int semester)
        {
            int scheduleId = 0;

            DataBase.ExecuteInTransaction((con, tx) =>
            {
                object existingId = DataBase.ExecuteScalar(con, tx, @"
SELECT TOP 1 IDРасписание
FROM dbo.Расписания
WHERE Название = @name AND Семестр = @sem
ORDER BY ДатаСоздания DESC, IDРасписание DESC;",
                    new SqlParameter("@name", name),
                    new SqlParameter("@sem", semester));

                if (existingId != null && existingId != DBNull.Value)
                {
                    scheduleId = Convert.ToInt32(existingId);
                    return;
                }

                object idObj = DataBase.ExecuteScalar(con, tx, @"
INSERT INTO dbo.Расписания(Название, Семестр, ДатаСоздания)
VALUES (@name, @sem, GETDATE());
SELECT SCOPE_IDENTITY();",
                    new SqlParameter("@name", name),
                    new SqlParameter("@sem", semester));

                scheduleId = Convert.ToInt32(idObj);
            });

            return scheduleId;
        }

        private Dictionary<string, List<string>> LoadRoomsByType()
        {
            DataTable dt = DataBase.Query(@"SELECT НомерАудитории, ТипАудитории FROM dbo.Аудитории;");

            var dict = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["Лекции"] = new List<string>(),
                ["Упражнения"] = new List<string>(),
                ["Компьютерный класс"] = new List<string>(),
                ["Мультимедийные аудитории"] = new List<string>()
            };

            foreach (DataRow r in dt.Rows)
            {
                string num = Convert.ToString(r["НомерАудитории"]);
                string type = Convert.ToString(r["ТипАудитории"]);
                if (!dict.ContainsKey(type)) dict[type] = new List<string>();
                dict[type].Add(num);
            }
            return dict;
        }

        private void LoadScheduleToGrid(int scheduleId, int week)
        {
            DataTable dt = DataBase.Query(@"
SELECT
    rz.IDЗапись,
    rz.НомерНедели,
    rz.ДеньНедели,
    rz.НомерПары,
    rz.НазваниеГруппы,
    CASE
        WHEN rz.ТипЗанятия = N'Физкультура'
            THEN N'Элективные курсы по физической культуре и спорту'
        ELSE p.Название
    END AS Предмет,
    CASE
        WHEN rz.ТипЗанятия = N'Физкультура'
            THEN N''
        ELSE rz.ТипЗанятия
    END AS ТипЗанятия,
    t.ФИО AS Преподаватель,
    CASE
        WHEN rz.ТипЗанятия = N'Физкультура'
            THEN N''
        ELSE rz.НомерАудитории
    END AS НомерАудитории
FROM dbo.РасписаниеЗанятий rz
JOIN dbo.Предметы p ON p.IDПредмет = rz.IDПредмет
LEFT JOIN dbo.Преподаватели t ON t.IDПреподаватель = rz.IDПреподаватель
WHERE rz.IDРасписание = @sid AND rz.НомерНедели = @week
ORDER BY rz.ДеньНедели, rz.НомерПары;",
                new SqlParameter("@sid", scheduleId),
                new SqlParameter("@week", week));

            dgvResult.DataSource = dt;
            GridHelper.HideIdColumns(dgvResult);
            dgvResult.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvResult.AllowUserToAddRows = false;
            dgvResult.RowHeadersVisible = false;
        }

    }
}
