using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using ClosedXML.Excel;
using X2.Class;

namespace X2
{
    public partial class ViewScheduleForm : Form
    {
        private const string AllGroupsLabel = "Все группы";
        private const string MatrixMode = "Общая сетка по группам";
        private const string FlowMode = "Поток";
        private const string TeacherSemesterMode = "Преподаватель на весь семестр";
        private const string EditMode = "Прямое редактирование записей";
        private const string AllDirectionsLabel = "Все потоки";
        private const string PhysicalLessonType = "Физкультура";
        private const string PhysicalSubjectDisplayName = "Элективные курсы по физической культуре и спорту";

        private const int DefaultGridTop = 293;
        private const int EditorTop = 285;
        private const int GridMargin = 14;
        private const int GridBottomMargin = 22;
        private const string EditDayKeyColumn = "__EditDay";
        private const string EditPairKeyColumn = "__EditPair";
        private int? _selectedEntryId;
        private ScheduleSeriesData _selectedSeries;
        private bool _suspendEditorHandlers;
        private DataTable _allSubjects;
        private DataTable _editRooms;
        private readonly bool _openInEditMode;

        private GroupBox gbEditor;
        private Label lblEditWeek;
        private NumericUpDown nudWeek;
        private Label lblEditDay;
        private ComboBox cbEditDay;
        private Label lblEditPair;
        private ComboBox cbEditPair;
        private Label lblEditType;
        private ComboBox cbEditType;
        private Label lblEditRoom;
        private ComboBox cbEditRoom;
        private Label lblEditWeeks;
        private TextBox txtEditWeeks;
        private Label lblEditGroup;
        private ComboBox cbEditGroup;
        private Label lblEditSubject;
        private ComboBox cbEditSubject;
        private Label lblEditTeacher;
        private ComboBox cbEditTeacher;
        private Label lblEditSecondTeacher;
        private ComboBox cbEditSecondTeacher;
        private Button btnAddEntry;
        private Button btnUpdateEntry;
        private Button btnDeleteEntry;
        private Button btnResetEdit;
        private Label lblFlowCourse;
        private ComboBox cbFlowCourse;
        private Label lblFlowSemester;
        private ComboBox cbFlowSemester;
        private Label lblFlowDirection;
        private ComboBox cbFlowDirection;

        private sealed class LessonVariantOption
        {
            public DataRow RepresentativeRow { get; set; }
            public string DisplayText { get; set; }

            public override string ToString()
            {
                return DisplayText ?? string.Empty;
            }
        }

        public ViewScheduleForm()
            : this(false)
        {
        }

        public ViewScheduleForm(bool openInEditMode)
        {
            _openInEditMode = openInEditMode;
            InitializeComponent();
            dgv.AllowUserToAddRows = false;
            InitializeFlowControls();
            InitializeEditorControls();

            dgv.CellClick -= dgv_CellClick;
            dgv.CellClick += dgv_CellClick;

            cbSchedule.SelectedIndexChanged -= cbSchedule_SelectedIndexChanged;
            cbSchedule.SelectedIndexChanged += cbSchedule_SelectedIndexChanged;

            cbGroup.SelectedIndexChanged -= cbGroup_SelectedIndexChanged;
            cbGroup.SelectedIndexChanged += cbGroup_SelectedIndexChanged;

            cbFlowSemester.SelectedIndexChanged -= cbFlowSemester_SelectedIndexChanged;
            cbFlowSemester.SelectedIndexChanged += cbFlowSemester_SelectedIndexChanged;

            cbFlowCourse.SelectedIndexChanged -= cbFlowCourse_SelectedIndexChanged;
            cbFlowCourse.SelectedIndexChanged += cbFlowCourse_SelectedIndexChanged;

            cbFlowDirection.SelectedIndexChanged -= cbFlowDirection_SelectedIndexChanged;
            cbFlowDirection.SelectedIndexChanged += cbFlowDirection_SelectedIndexChanged;

            Resize -= ViewScheduleForm_Resize;
            Resize += ViewScheduleForm_Resize;
        }

        private void InitializeFlowControls()
        {
            lblFlowCourse = new Label
            {
                AutoSize = true,
                Font = new System.Drawing.Font("Segoe UI", 9F),
                Location = new System.Drawing.Point(560, 84),
                Name = "lblFlowCourse",
                Text = "Курс"
            };

            cbFlowCourse = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new System.Drawing.Font("Segoe UI", 10F),
                FormattingEnabled = true,
                Location = new System.Drawing.Point(563, 104),
                Name = "cbFlowCourse",
                Size = new System.Drawing.Size(95, 25)
            };

            lblFlowSemester = new Label
            {
                AutoSize = true,
                Font = new System.Drawing.Font("Segoe UI", 9F),
                Location = new System.Drawing.Point(675, 84),
                Name = "lblFlowSemester",
                Text = "Семестр"
            };

            cbFlowSemester = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new System.Drawing.Font("Segoe UI", 10F),
                FormattingEnabled = true,
                Location = new System.Drawing.Point(678, 104),
                Name = "cbFlowSemester",
                Size = new System.Drawing.Size(110, 25)
            };

            lblFlowDirection = new Label
            {
                AutoSize = true,
                Font = new System.Drawing.Font("Segoe UI", 9F),
                Location = new System.Drawing.Point(805, 84),
                Name = "lblFlowDirection",
                Text = "Поток"
            };

            cbFlowDirection = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new System.Drawing.Font("Segoe UI", 10F),
                FormattingEnabled = true,
                Location = new System.Drawing.Point(808, 104),
                Name = "cbFlowDirection",
                Size = new System.Drawing.Size(268, 25)
            };

            gbFilters.Controls.Add(lblFlowCourse);
            gbFilters.Controls.Add(cbFlowCourse);
            gbFilters.Controls.Add(lblFlowSemester);
            gbFilters.Controls.Add(cbFlowSemester);
            gbFilters.Controls.Add(lblFlowDirection);
            gbFilters.Controls.Add(cbFlowDirection);
        }

        private void InitializeEditorControls()
        {
            gbEditor = new GroupBox
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold),
                Location = new System.Drawing.Point(14, EditorTop),
                Name = "gbEditor",
                Size = new System.Drawing.Size(1092, 178),
                TabStop = false,
                Text = "Прямое редактирование записи",
                Visible = false
            };

            lblEditWeek = CreateEditorLabel("Неделя", 16, 27);
            nudWeek = new NumericUpDown
            {
                Font = new System.Drawing.Font("Segoe UI", 10F),
                Location = new System.Drawing.Point(19, 47),
                Maximum = 52,
                Minimum = 1,
                Name = "nudWeek",
                Size = new System.Drawing.Size(90, 25),
                Value = 1
            };
            nudWeek.ValueChanged += nudWeek_ValueChanged;

            lblEditDay = CreateEditorLabel("День", 125, 27);
            cbEditDay = CreateEditorComboBox(128, 47, 140, "cbEditDay");
            cbEditDay.Enabled = false;

            lblEditPair = CreateEditorLabel("Пара", 284, 27);
            cbEditPair = CreateEditorComboBox(287, 47, 160, "cbEditPair");
            cbEditPair.Enabled = false;

            lblEditType = CreateEditorLabel("Тип занятия", 463, 27);
            cbEditType = CreateEditorComboBox(466, 47, 120, "cbEditType");
            cbEditType.Enabled = false;
            cbEditType.Items.AddRange(new object[] { "ЛК", "У", "ЛБ", "КР" });
            cbEditType.SelectedIndexChanged += cbEditType_SelectedIndexChanged;

            lblEditRoom = CreateEditorLabel("Аудитория", 602, 27);
            cbEditRoom = CreateEditorComboBox(605, 47, 180, "cbEditRoom");
            cbEditRoom.Enabled = false;

            btnResetEdit = CreateEditorButton("Очистить", 798, 45, 120, "btnResetEdit");
            btnResetEdit.Click += btnResetEdit_Click;
            btnResetEdit.Location = new System.Drawing.Point(935, 45);
            btnResetEdit.Size = new System.Drawing.Size(133, 30);
            btnResetEdit.Text = "Снять выбор";

            lblEditGroup = CreateEditorLabel("Группа", 16, 77);
            cbEditGroup = CreateEditorComboBox(19, 97, 220, "cbEditGroup");
            cbEditGroup.SelectedIndexChanged += cbEditGroup_SelectedIndexChanged;
            cbEditGroup.Enabled = false;

            lblEditSubject = CreateEditorLabel("Предмет", 255, 77);
            cbEditSubject = CreateEditorComboBox(258, 97, 250, "cbEditSubject");
            cbEditSubject.SelectedIndexChanged += cbEditSubject_SelectedIndexChanged;
            cbEditSubject.Enabled = false;

            lblEditTeacher = CreateEditorLabel("Преподаватель", 524, 77);
            cbEditTeacher = CreateEditorComboBox(527, 97, 250, "cbEditTeacher");
            cbEditTeacher.SelectedIndexChanged += cbEditTeacher_SelectedIndexChanged;
            cbEditTeacher.Enabled = false;

            lblEditSecondTeacher = CreateEditorLabel("Преподаватель 2 (ЛБ)", 773, 77);
            cbEditSecondTeacher = CreateEditorComboBox(776, 97, 200, "cbEditSecondTeacher");
            cbEditSecondTeacher.Enabled = false;

            lblEditWeeks = CreateEditorLabel("Недели", 16, 127);
            txtEditWeeks = new TextBox
            {
                Font = new System.Drawing.Font("Segoe UI", 10F),
                Location = new System.Drawing.Point(19, 147),
                Name = "txtEditWeeks",
                Size = new System.Drawing.Size(250, 25),
                Enabled = false
            };

            btnAddEntry = CreateEditorButton("Добавить", 798, 95, 85, "btnAddEntry");
            btnAddEntry.Click += btnAddEntry_Click;
            btnAddEntry.Visible = false;

            btnUpdateEntry = CreateEditorButton("Изменить", 888, 95, 90, "btnUpdateEntry");
            btnUpdateEntry.Enabled = false;
            btnUpdateEntry.Click += btnUpdateEntry_Click;
            btnUpdateEntry.Location = new System.Drawing.Point(975, 145);
            btnUpdateEntry.Size = new System.Drawing.Size(93, 30);
            btnUpdateEntry.Text = "Сохранить";

            btnDeleteEntry = CreateEditorButton("Удалить", 983, 95, 85, "btnDeleteEntry");
            btnDeleteEntry.Enabled = false;
            btnDeleteEntry.Click += btnDeleteEntry_Click;
            btnDeleteEntry.Visible = false;
            gbEditor.Text = "Редактирование расписания";
            lblEditWeek.Text = "Неделя варианта";
            lblEditDay.Text = "День недели";
            lblEditPair.Text = "Номер пары";
            lblEditType.Text = "Тип занятия";
            lblEditRoom.Text = "Аудитория";
            lblEditGroup.Text = "Группа";
            lblEditSubject.Text = "Предмет";
            lblEditTeacher.Text = "Преподаватель";
            lblEditSecondTeacher.Text = "Преподаватель 2 (ЛБ)";
            lblEditWeeks.Text = "Недели занятия";
            cbEditType.Items.Clear();
            cbEditType.Items.AddRange(new object[] { "ЛК", "У", "ЛБ", "КР" });
            btnResetEdit.Text = "Снять выбор";
            btnUpdateEntry.Text = "Сохранить";

            lblEditWeek.Visible = false;
            nudWeek.Visible = false;
            nudWeek.TabStop = false;

            lblEditDay.Location = new System.Drawing.Point(16, 27);
            cbEditDay.Location = new System.Drawing.Point(19, 47);
            lblEditPair.Location = new System.Drawing.Point(175, 27);
            cbEditPair.Location = new System.Drawing.Point(178, 47);
            lblEditType.Location = new System.Drawing.Point(354, 27);
            cbEditType.Location = new System.Drawing.Point(357, 47);
            lblEditRoom.Location = new System.Drawing.Point(493, 27);
            cbEditRoom.Location = new System.Drawing.Point(496, 47);
            lblEditGroup.Location = new System.Drawing.Point(16, 77);
            cbEditGroup.Location = new System.Drawing.Point(19, 97);
            cbEditGroup.Size = new System.Drawing.Size(200, 25);
            lblEditSubject.Location = new System.Drawing.Point(235, 77);
            cbEditSubject.Location = new System.Drawing.Point(238, 97);
            cbEditSubject.Size = new System.Drawing.Size(250, 25);
            lblEditTeacher.Location = new System.Drawing.Point(504, 77);
            cbEditTeacher.Location = new System.Drawing.Point(507, 97);
            cbEditTeacher.Size = new System.Drawing.Size(250, 25);
            lblEditSecondTeacher.Location = new System.Drawing.Point(773, 77);
            cbEditSecondTeacher.Location = new System.Drawing.Point(776, 97);
            cbEditSecondTeacher.Size = new System.Drawing.Size(200, 25);

            gbEditor.Controls.Add(lblEditWeek);
            gbEditor.Controls.Add(nudWeek);
            gbEditor.Controls.Add(lblEditDay);
            gbEditor.Controls.Add(cbEditDay);
            gbEditor.Controls.Add(lblEditPair);
            gbEditor.Controls.Add(cbEditPair);
            gbEditor.Controls.Add(lblEditType);
            gbEditor.Controls.Add(cbEditType);
            gbEditor.Controls.Add(lblEditRoom);
            gbEditor.Controls.Add(cbEditRoom);
            gbEditor.Controls.Add(btnResetEdit);
            gbEditor.Controls.Add(lblEditGroup);
            gbEditor.Controls.Add(cbEditGroup);
            gbEditor.Controls.Add(lblEditSubject);
            gbEditor.Controls.Add(cbEditSubject);
            gbEditor.Controls.Add(lblEditTeacher);
            gbEditor.Controls.Add(cbEditTeacher);
            gbEditor.Controls.Add(lblEditSecondTeacher);
            gbEditor.Controls.Add(cbEditSecondTeacher);
            gbEditor.Controls.Add(lblEditWeeks);
            gbEditor.Controls.Add(txtEditWeeks);
            gbEditor.Controls.Add(btnAddEntry);
            gbEditor.Controls.Add(btnUpdateEntry);
            gbEditor.Controls.Add(btnDeleteEntry);

            Controls.Add(gbEditor);
            gbEditor.BringToFront();
        }

        private static Label CreateEditorLabel(string text, int x, int y)
        {
            return new Label
            {
                AutoSize = true,
                Font = new System.Drawing.Font("Segoe UI", 9F),
                Location = new System.Drawing.Point(x, y),
                Text = text
            };
        }

        private static ComboBox CreateEditorComboBox(int x, int y, int width, string name)
        {
            return new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new System.Drawing.Font("Segoe UI", 10F),
                FormattingEnabled = true,
                Location = new System.Drawing.Point(x, y),
                Name = name,
                Size = new System.Drawing.Size(width, 25)
            };
        }

        private static Button CreateEditorButton(string text, int x, int y, int width, string name)
        {
            return new Button
            {
                Font = new System.Drawing.Font("Segoe UI", 10F),
                Location = new System.Drawing.Point(x, y),
                Name = name,
                Size = new System.Drawing.Size(width, 30),
                Text = text,
                UseVisualStyleBackColor = true
            };
        }

        private void ViewScheduleForm_Load(object sender, EventArgs e)
        {
            try
            {
                DataBase.EnsureTeacherSubjectSchema();
                DataBase.EnsureSubjectProfileSemesterSchema();
                DataBase.EnsureGroupFlowSchema();
                DataBase.EnsureLessonTypeSchema();
                DataBase.EnsureScheduleRoomNullableSchema();
                DataBase.EnsureExcelLikeScheduleSchema();

                _suspendEditorHandlers = true;

                LoadLookups();
                LoadEditorStaticLookups();
                LoadViewModes();
                ClearFilters();

                if (_openInEditMode)
                    SelectComboBoxValue(cbViewMode, EditMode);

                if (cbSchedule.Items.Count > 0)
                    cbSchedule.SelectedIndex = 0;

                LoadFilterGroupsForSelectedSchedule(false);
                LoadTeachersForSelectedSchedule(false);

                _suspendEditorHandlers = false;

                ResetEditor();
                ConfigureMatrixGrid();
                UpdateViewModeState();
                LayoutContent();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки формы:\n" + ex.Message);
            }
        }

        private void LayoutContent()
        {
            int gridTop = DefaultGridTop;

            gbEditor.Visible = IsEditMode();
            if (gbEditor.Visible)
            {
                gbEditor.Location = new System.Drawing.Point(GridMargin, EditorTop);
                gridTop = gbEditor.Bottom + 10;
            }

            dgv.Location = new System.Drawing.Point(GridMargin, gridTop);
            dgv.Size = new System.Drawing.Size(
                ClientSize.Width - GridMargin * 2,
                ClientSize.Height - gridTop - GridBottomMargin);
        }

        private void LoadLookups()
        {
            DataTable schedules = DataBase.Query(@"
SELECT
    IDРасписание,
    Семестр,
    ДатаСоздания,
    (Название
        + N' (семестр ' + CAST(Семестр AS nvarchar(10))
        + N', ' + CONVERT(nvarchar(10), ДатаСоздания, 104)
        + N' ' + LEFT(CONVERT(nvarchar(8), ДатаСоздания, 108), 5)
        + N')') AS DisplayName
FROM dbo.Расписания
ORDER BY ДатаСоздания DESC;");

            cbSchedule.DisplayMember = "DisplayName";
            cbSchedule.ValueMember = "IDРасписание";
            cbSchedule.DataSource = schedules;

            DataTable groups = DataBase.Query(@"
SELECT НазваниеГруппы
FROM dbo.Группы
ORDER BY НазваниеГруппы;");

            LoadFilterGroupsForSelectedSchedule(false);

            cbEditGroup.DisplayMember = "НазваниеГруппы";
            cbEditGroup.ValueMember = "НазваниеГруппы";
            cbEditGroup.DataSource = groups.Copy();

            LoadFlowLookups(schedules);

            LoadTeachersForSelectedSchedule(false);

            _editRooms = DataBase.Query(@"
SELECT НомерАудитории, ТипАудитории
FROM dbo.Аудитории
ORDER BY НомерАудитории;");

            DataTable filterRooms = new DataTable();
            filterRooms.Columns.Add("НомерАудитории", typeof(string));
            filterRooms.Rows.Add(string.Empty);
            foreach (DataRow row in _editRooms.Rows)
                filterRooms.Rows.Add(Convert.ToString(row["НомерАудитории"]));

            cbRoom.DisplayMember = "НомерАудитории";
            cbRoom.ValueMember = "НомерАудитории";
            cbRoom.DataSource = filterRooms;

            _allSubjects = DataBase.Query(@"
SELECT IDПредмет, Название
FROM dbo.Предметы
ORDER BY Название;");
        }

        private void LoadFlowLookups(DataTable schedules)
        {
            DataTable courses = new DataTable();
            courses.Columns.Add("Курс", typeof(int));
            courses.Columns.Add("Название", typeof(string));
            courses.Rows.Add(DBNull.Value, string.Empty);

            DataTable courseRows = DataBase.Query(@"
SELECT DISTINCT Курс
FROM dbo.Группы
ORDER BY Курс;");
            foreach (DataRow row in courseRows.Rows)
            {
                int course = Convert.ToInt32(row["Курс"]);
                courses.Rows.Add(course, course + " курс");
            }

            cbFlowCourse.DisplayMember = "Название";
            cbFlowCourse.ValueMember = "Курс";
            cbFlowCourse.DataSource = courses;

            LoadFlowDirections();
            LoadGeneratedFlowSemesters();
        }

        private void LoadFlowDirections()
        {
            string oldDirection = GetSelectedFlowDirection();
            int? course = GetSelectedFlowCourse();
            DataTable directions = new DataTable();
            directions.Columns.Add("Направление", typeof(string));
            directions.Columns.Add("Название", typeof(string));
            directions.Rows.Add(string.Empty, AllDirectionsLabel);

            DataTable directionRows = DataBase.Query(@"
SELECT DISTINCT ISNULL(NULLIF(LTRIM(RTRIM(Поток)), N''), Направление) AS Поток
FROM dbo.Группы
WHERE (@course IS NULL OR Курс = @course)
  AND LTRIM(RTRIM(ISNULL(NULLIF(Поток, N''), Направление))) <> N''
ORDER BY Поток;",
                new SqlParameter("@course", (object)course ?? DBNull.Value));
            foreach (DataRow row in directionRows.Rows)
            {
                string direction = Convert.ToString(row["Поток"]);
                directions.Rows.Add(direction, direction);
            }

            bool wasSuspended = _suspendEditorHandlers;
            _suspendEditorHandlers = true;
            try
            {
                cbFlowDirection.DisplayMember = "Название";
                cbFlowDirection.ValueMember = "Направление";
                cbFlowDirection.DataSource = directions;

                if (!string.IsNullOrWhiteSpace(oldDirection))
                    SelectComboBoxValue(cbFlowDirection, oldDirection);

                if (cbFlowDirection.SelectedIndex < 0 && cbFlowDirection.Items.Count > 0)
                    cbFlowDirection.SelectedIndex = 0;
            }
            finally
            {
                _suspendEditorHandlers = wasSuspended;
            }
        }

        private void LoadGeneratedFlowSemesters()
        {
            int? oldSemester = GetSelectedFlowSemester();
            int? course = GetSelectedFlowCourse();
            string direction = GetSelectedFlowDirection();

            DataTable semesters = new DataTable();
            semesters.Columns.Add("Семестр", typeof(int));
            semesters.Columns.Add("Название", typeof(string));

            if (!course.HasValue)
            {
                bool wasSuspendedWithoutCourse = _suspendEditorHandlers;
                _suspendEditorHandlers = true;
                try
                {
                    cbFlowSemester.DisplayMember = "Название";
                    cbFlowSemester.ValueMember = "Семестр";
                    cbFlowSemester.DataSource = semesters;
                }
                finally
                {
                    _suspendEditorHandlers = wasSuspendedWithoutCourse;
                }

                return;
            }

            DataTable semesterRows = DataBase.Query(@"
SELECT DISTINCT r.Семестр
FROM dbo.Расписания r
JOIN dbo.РасписаниеЗанятий rz ON rz.IDРасписание = r.IDРасписание
JOIN dbo.Группы g ON g.НазваниеГруппы = rz.НазваниеГруппы
WHERE (@course IS NULL OR g.Курс = @course)
  AND (@direction = N'' OR ISNULL(NULLIF(LTRIM(RTRIM(g.Поток)), N''), g.Направление) = @direction)
ORDER BY r.Семестр;",
                new SqlParameter("@course", (object)course ?? DBNull.Value),
                new SqlParameter("@direction", direction ?? string.Empty));

            foreach (DataRow row in semesterRows.Rows)
            {
                int semester = Convert.ToInt32(row["Семестр"]);
                semesters.Rows.Add(semester, semester + " семестр");
            }

            bool wasSuspended = _suspendEditorHandlers;
            _suspendEditorHandlers = true;
            try
            {
                cbFlowSemester.DisplayMember = "Название";
                cbFlowSemester.ValueMember = "Семестр";
                cbFlowSemester.DataSource = semesters;

                if (oldSemester.HasValue)
                    SelectComboBoxValue(cbFlowSemester, oldSemester.Value);

                if (cbFlowSemester.SelectedIndex < 0 && cbFlowSemester.Items.Count > 0)
                    cbFlowSemester.SelectedIndex = 0;
            }
            finally
            {
                _suspendEditorHandlers = wasSuspended;
            }
        }

        private void LoadEditorStaticLookups()
        {
            LoadEditorDayLookup();
            LoadEditorPairLookup();
            LoadEditorRoomLookup();
        }

        private void LoadFilterGroupsForSelectedSchedule(bool preserveSelection)
        {
            object oldGroup = preserveSelection ? cbGroup.SelectedValue : null;
            int scheduleId;

            DataTable groups = TryGetSelectedScheduleId(out scheduleId)
                ? DataBase.Query(@"
SELECT DISTINCT НазваниеГруппы
FROM dbo.РасписаниеЗанятий
WHERE IDРасписание = @scheduleId
ORDER BY НазваниеГруппы;",
                    new SqlParameter("@scheduleId", scheduleId))
                : CreateGroupFilterLookupTable(false);

            DataTable filterGroups = CreateGroupFilterLookupTable(true);
            foreach (DataRow row in groups.Rows)
            {
                string groupName = Convert.ToString(row["НазваниеГруппы"]);
                if (!string.IsNullOrWhiteSpace(groupName))
                    filterGroups.Rows.Add(groupName);
            }

            bool wasSuspended = _suspendEditorHandlers;
            _suspendEditorHandlers = true;
            try
            {
                cbGroup.DisplayMember = "НазваниеГруппы";
                cbGroup.ValueMember = "НазваниеГруппы";
                cbGroup.DataSource = filterGroups;

                if (oldGroup != null && oldGroup != DBNull.Value)
                    SelectComboBoxValue(cbGroup, oldGroup);

                if (cbGroup.SelectedIndex < 0 && cbGroup.Items.Count > 0)
                    cbGroup.SelectedIndex = 0;
            }
            finally
            {
                _suspendEditorHandlers = wasSuspended;
            }
        }

        private static DataTable CreateGroupFilterLookupTable(bool includeAllGroups)
        {
            var dt = new DataTable();
            dt.Columns.Add("НазваниеГруппы", typeof(string));

            if (includeAllGroups)
                dt.Rows.Add(AllGroupsLabel);

            return dt;
        }

        private void LoadTeachersForSelectedSchedule(bool preserveSelection)
        {
            object oldTeacherId = preserveSelection ? cbTeacher.SelectedValue : null;
            int scheduleId;

            DataTable teachers;
            if (!TryGetSelectedScheduleId(out scheduleId))
            {
                teachers = CreateTeacherFilterLookupTable();
            }
            else if (IsTeacherSemesterMode())
            {
                int seasonParity = Math.Abs(GetSelectedScheduleSemester() % 2);
                teachers = DataBase.Query(@"
SELECT CAST(NULL AS int) AS IDПреподаватель, N'' AS ФИО
UNION ALL
SELECT DISTINCT
    t.IDПреподаватель,
    t.ФИО
FROM
(
    SELECT rz.IDПреподаватель
    FROM dbo.РасписаниеЗанятий rz
    JOIN dbo.Расписания r ON r.IDРасписание = rz.IDРасписание
    WHERE r.Семестр BETWEEN 1 AND 10
      AND (r.Семестр % 2) = @seasonParity
      AND rz.IDПреподаватель IS NOT NULL

    UNION

    SELECT rz.IDПреподаватель2
    FROM dbo.РасписаниеЗанятий rz
    JOIN dbo.Расписания r ON r.IDРасписание = rz.IDРасписание
    WHERE r.Семестр BETWEEN 1 AND 10
      AND (r.Семестр % 2) = @seasonParity
      AND rz.IDПреподаватель2 IS NOT NULL
) teacher_ids
JOIN dbo.Преподаватели t ON t.IDПреподаватель = teacher_ids.IDПреподаватель
ORDER BY ФИО;",
                    new SqlParameter("@seasonParity", seasonParity));
            }
            else
            {
                teachers = DataBase.Query(@"
SELECT CAST(NULL AS int) AS IDПреподаватель, N'' AS ФИО
UNION ALL
SELECT DISTINCT
    t.IDПреподаватель,
    t.ФИО
FROM
(
    SELECT rz.IDПреподаватель
    FROM dbo.РасписаниеЗанятий rz
    WHERE rz.IDРасписание = @scheduleId
      AND rz.IDПреподаватель IS NOT NULL

    UNION

    SELECT rz.IDПреподаватель2
    FROM dbo.РасписаниеЗанятий rz
    WHERE rz.IDРасписание = @scheduleId
      AND rz.IDПреподаватель2 IS NOT NULL
) teacher_ids
JOIN dbo.Преподаватели t ON t.IDПреподаватель = teacher_ids.IDПреподаватель
ORDER BY ФИО;",
                    new SqlParameter("@scheduleId", scheduleId));
            }

            bool wasSuspended = _suspendEditorHandlers;
            _suspendEditorHandlers = true;
            try
            {
                cbTeacher.DisplayMember = "ФИО";
                cbTeacher.ValueMember = "IDПреподаватель";
                cbTeacher.DataSource = teachers;

                if (oldTeacherId != null && oldTeacherId != DBNull.Value)
                    SelectComboBoxValue(cbTeacher, oldTeacherId);

                if (cbTeacher.SelectedIndex < 0 && cbTeacher.Items.Count > 0)
                    cbTeacher.SelectedIndex = 0;
            }
            finally
            {
                _suspendEditorHandlers = wasSuspended;
            }
        }

        private static DataTable CreateTeacherFilterLookupTable()
        {
            var dt = new DataTable();
            dt.Columns.Add("IDПреподаватель", typeof(int));
            dt.Columns.Add("ФИО", typeof(string));
            dt.Rows.Add(DBNull.Value, string.Empty);
            return dt;
        }

        private void LoadEditorDayLookup()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("DayValue", typeof(int));
            dt.Columns.Add("DayName", typeof(string));

            for (int day = 1; day <= 5; day++)
                dt.Rows.Add(day, DayFull(day));

            cbEditDay.DisplayMember = "DayName";
            cbEditDay.ValueMember = "DayValue";
            cbEditDay.DataSource = dt;
        }

        private void LoadEditorPairLookup()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("PairValue", typeof(int));
            dt.Columns.Add("PairDisplay", typeof(string));

            Dictionary<int, string> bellTimes = null;
            try
            {
                bellTimes = LoadBellTimes();
            }
            catch
            {
                bellTimes = null;
            }

            if (bellTimes != null && bellTimes.Count > 0)
            {
                foreach (var pair in bellTimes.OrderBy(x => x.Key))
                    dt.Rows.Add(pair.Key, pair.Key + " (" + pair.Value + ")");
            }
            else
            {
                for (int pair = 1; pair <= 5; pair++)
                    dt.Rows.Add(pair, pair.ToString());
            }

            cbEditPair.DisplayMember = "PairDisplay";
            cbEditPair.ValueMember = "PairValue";
            cbEditPair.DataSource = dt;
        }

        private void LoadEditorRoomLookup()
        {
            cbEditRoom.DisplayMember = "НомерАудитории";
            cbEditRoom.ValueMember = "НомерАудитории";
            cbEditRoom.DataSource = _editRooms == null ? null : _editRooms.Copy();
        }

        private void LoadViewModes()
        {
            cbViewMode.Items.Clear();
            cbViewMode.Items.Add(MatrixMode);
            cbViewMode.Items.Add(FlowMode);
            cbViewMode.Items.Add(TeacherSemesterMode);
            cbViewMode.Items.Add(EditMode);
        }

        private void ClearFilters()
        {
            if (cbViewMode.Items.Count > 0)
                cbViewMode.SelectedIndex = 0;

            if (cbGroup.Items.Count > 0)
                cbGroup.SelectedIndex = 0;

            if (cbTeacher.Items.Count > 0)
                cbTeacher.SelectedIndex = 0;

            if (cbRoom.Items.Count > 0)
                cbRoom.SelectedIndex = 0;

            if (cbFlowCourse != null && cbFlowCourse.Items.Count > 0)
                cbFlowCourse.SelectedIndex = 0;

            if (cbFlowDirection != null && cbFlowDirection.Items.Count > 0)
                cbFlowDirection.SelectedIndex = 0;

            LoadGeneratedFlowSemesters();

            if (!_suspendEditorHandlers)
                ResetEditor();
        }

        private void ResetEditor(bool preserveViewWeek = false)
        {
            int currentWeek = (int)nudWeek.Value;

            _selectedEntryId = null;
            _selectedSeries = null;
            _suspendEditorHandlers = true;

            nudWeek.Value = preserveViewWeek ? currentWeek : 1;
            cbEditDay.SelectedIndex = -1;
            cbEditPair.SelectedIndex = -1;
            cbEditType.SelectedIndex = -1;
            cbEditGroup.SelectedIndex = -1;
            cbEditSubject.DataSource = CreateEmptySubjectLookupTable();
            cbEditTeacher.DataSource = CreateEmptyTeacherLookupTable();
            cbEditSecondTeacher.DataSource = CreateEmptyTeacherLookupTable();
            RefreshEditLessonTypeItems(false);
            cbEditRoom.SelectedIndex = -1;
            txtEditWeeks.Text = string.Empty;

            _suspendEditorHandlers = false;

            SetEditorState(false);
        }

        private void SetEditorState(bool hasSelection)
        {
            cbEditDay.Enabled = hasSelection;
            cbEditPair.Enabled = hasSelection;
            txtEditWeeks.Enabled = hasSelection;
            btnUpdateEntry.Enabled = hasSelection;
            btnDeleteEntry.Enabled = false;
            UpdateSecondTeacherControlState(hasSelection);
        }

        private void SetDefaultEditGroup()
        {
            if (cbEditGroup.Items.Count == 0)
            {
                cbEditGroup.SelectedIndex = -1;
                return;
            }

            string preferredGroup = cbGroup.SelectedValue == null
                ? string.Empty
                : Convert.ToString(cbGroup.SelectedValue);

            if (!string.IsNullOrWhiteSpace(preferredGroup) &&
                !string.Equals(preferredGroup, AllGroupsLabel, StringComparison.OrdinalIgnoreCase))
            {
                SelectComboBoxValue(cbEditGroup, preferredGroup);
            }
            else
            {
                cbEditGroup.SelectedIndex = 0;
            }
        }

        private void ClearEditorSelection()
        {
            _selectedEntryId = null;
            _selectedSeries = null;
            SetEditorState(false);
        }

        private bool IsTeacherSemesterMode()
        {
            return string.Equals(Convert.ToString(cbViewMode.SelectedItem), TeacherSemesterMode, StringComparison.Ordinal);
        }

        private bool IsFlowMode()
        {
            return string.Equals(Convert.ToString(cbViewMode.SelectedItem), FlowMode, StringComparison.Ordinal);
        }

        private bool IsEditMode()
        {
            return string.Equals(Convert.ToString(cbViewMode.SelectedItem), EditMode, StringComparison.Ordinal);
        }

        private int? GetSelectedTeacherId()
        {
            object teacherObj = cbTeacher.SelectedValue;
            if (teacherObj == null || teacherObj == DBNull.Value)
                return null;

            int teacherId;
            return int.TryParse(teacherObj.ToString(), out teacherId) ? teacherId : (int?)null;
        }

        private bool TryGetSelectedScheduleId(out int scheduleId)
        {
            scheduleId = 0;

            if (cbSchedule.SelectedValue == null)
                return false;

            return int.TryParse(cbSchedule.SelectedValue.ToString(), out scheduleId);
        }

        private int GetSelectedScheduleSemester()
        {
            var rowView = cbSchedule.SelectedItem as DataRowView;
            if (rowView == null || rowView["Семестр"] == DBNull.Value)
                return 1;

            return Convert.ToInt32(rowView["Семестр"]);
        }

        private int? GetSelectedFlowCourse()
        {
            if (cbFlowCourse == null || cbFlowCourse.SelectedValue == null || cbFlowCourse.SelectedValue == DBNull.Value)
                return null;

            int course;
            return int.TryParse(cbFlowCourse.SelectedValue.ToString(), out course) ? course : (int?)null;
        }

        private int? GetSelectedFlowSemester()
        {
            if (cbFlowSemester == null || cbFlowSemester.SelectedValue == null || cbFlowSemester.SelectedValue == DBNull.Value)
                return null;

            int semester;
            return int.TryParse(cbFlowSemester.SelectedValue.ToString(), out semester) ? semester : (int?)null;
        }

        private string GetSelectedFlowDirection()
        {
            if (cbFlowDirection == null || cbFlowDirection.SelectedValue == null || cbFlowDirection.SelectedValue == DBNull.Value)
                return string.Empty;

            return Convert.ToString(cbFlowDirection.SelectedValue).Trim();
        }

        private void SyncFlowSemesterWithSelectedSchedule()
        {
            if (cbFlowSemester == null || cbFlowSemester.Items.Count == 0 || cbSchedule.SelectedItem == null)
                return;

            int semester = GetSelectedScheduleSemester();
            SelectComboBoxValue(cbFlowSemester, semester);
        }

        private bool SelectLatestScheduleForFlowSemester()
        {
            int? semester = GetSelectedFlowSemester();
            if (!semester.HasValue || cbSchedule.DataSource == null)
                return false;

            var scheduleTable = cbSchedule.DataSource as DataTable;
            if (scheduleTable == null)
                return false;

            DataRow scheduleRow = scheduleTable.AsEnumerable()
                .Where(row => row["Семестр"] != DBNull.Value && Convert.ToInt32(row["Семестр"]) == semester.Value)
                .OrderByDescending(row => row.Table.Columns.Contains("ДатаСоздания") && row["ДатаСоздания"] != DBNull.Value
                    ? Convert.ToDateTime(row["ДатаСоздания"])
                    : DateTime.MinValue)
                .ThenByDescending(row => Convert.ToInt32(row["IDРасписание"]))
                .FirstOrDefault();

            if (scheduleRow == null)
                return false;

            SelectComboBoxValue(cbSchedule, scheduleRow["IDРасписание"]);
            return true;
        }

        private Dictionary<string, int> GetSemesterWeeksByGroup(IEnumerable<string> groupNames, int semester)
        {
            var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            if (groupNames == null)
                return result;

            foreach (string groupName in groupNames
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                result[groupName] = DataBase.GetWeeksForGroup(groupName, semester);
            }

            return result;
        }

        private static List<int> NormalizeWeeks(IEnumerable<int> weeks)
        {
            if (weeks == null)
                return new List<int>();

            return weeks
                .Where(week => week > 0)
                .Distinct()
                .OrderBy(week => week)
                .ToList();
        }

        private static List<int> ParseWeeksCsv(string weeksCsv)
        {
            if (string.IsNullOrWhiteSpace(weeksCsv))
                return new List<int>();

            return NormalizeWeeks(
                weeksCsv
                    .Split(new[] { ',', ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(token =>
                    {
                        int week;
                        return int.TryParse(token.Trim(), out week) ? week : 0;
                    }));
        }

        private static bool CoversWholeSemester(IList<int> weeks, int semesterWeeks)
        {
            if (weeks == null || semesterWeeks <= 0 || weeks.Count != semesterWeeks)
                return false;

            for (int i = 0; i < semesterWeeks; i++)
            {
                if (weeks[i] != i + 1)
                    return false;
            }

            return true;
        }

        private static string BuildWeeksDisplayText(
            IEnumerable<int> weeks,
            string groupName,
            IDictionary<string, int> semesterWeeksByGroup)
        {
            List<int> normalizedWeeks = NormalizeWeeks(weeks);
            if (normalizedWeeks.Count == 0)
                return string.Empty;

            if (!string.IsNullOrWhiteSpace(groupName)
                && semesterWeeksByGroup != null
                && semesterWeeksByGroup.TryGetValue(groupName, out int semesterWeeks)
                && CoversWholeSemester(normalizedWeeks, semesterWeeks))
            {
                return string.Empty;
            }

            return FormatWeekRanges(normalizedWeeks);
        }

        private static string BuildWeeksDisplayText(
            string weeksCsv,
            string groupName,
            IDictionary<string, int> semesterWeeksByGroup)
        {
            return BuildWeeksDisplayText(ParseWeeksCsv(weeksCsv), groupName, semesterWeeksByGroup);
        }

        private static string FormatWeekRanges(IList<int> weeks)
        {
            if (weeks == null || weeks.Count == 0)
                return string.Empty;

            var parts = new List<string>();
            int start = weeks[0];
            int previous = weeks[0];

            for (int i = 1; i < weeks.Count; i++)
            {
                if (weeks[i] == previous + 1)
                {
                    previous = weeks[i];
                    continue;
                }

                parts.Add(start == previous ? start.ToString() : start + "-" + previous);
                start = weeks[i];
                previous = weeks[i];
            }

            parts.Add(start == previous ? start.ToString() : start + "-" + previous);
            return string.Join(", ", parts);
        }

        private void UpdateViewModeState()
        {
            bool teacherMode = IsTeacherSemesterMode();
            bool flowMode = IsFlowMode();
            bool editMode = IsEditMode();

            lblSchedule.Enabled = !flowMode && !teacherMode;
            cbSchedule.Enabled = !flowMode && !teacherMode;
            UpdateDeleteScheduleButtonState();
            lblGroup.Enabled = !teacherMode && !flowMode;
            cbGroup.Enabled = !teacherMode && !flowMode;
            lblTeacher.Enabled = !editMode && !flowMode;
            cbTeacher.Enabled = !editMode && !flowMode;
            lblRoom.Enabled = !teacherMode && !editMode && !flowMode;
            cbRoom.Enabled = !teacherMode && !editMode && !flowMode;

            lblFlowCourse.Visible = flowMode;
            cbFlowCourse.Visible = flowMode;
            lblFlowSemester.Visible = flowMode;
            cbFlowSemester.Visible = flowMode;
            lblFlowDirection.Visible = flowMode;
            cbFlowDirection.Visible = flowMode;
            cbFlowCourse.Enabled = flowMode;
            cbFlowSemester.Enabled = flowMode;
            cbFlowDirection.Enabled = flowMode;

            if (teacherMode)
            {
                LoadTeachersForSelectedSchedule(true);

                if (cbGroup.Items.Count > 0)
                    cbGroup.SelectedIndex = 0;

                if (cbRoom.Items.Count > 0)
                    cbRoom.SelectedIndex = 0;
            }

            if (editMode && cbTeacher.Items.Count > 0)
                cbTeacher.SelectedIndex = 0;

            if (flowMode)
            {
                if (cbGroup.Items.Count > 0)
                    cbGroup.SelectedIndex = 0;

                if (cbTeacher.Items.Count > 0)
                    cbTeacher.SelectedIndex = 0;

                if (cbRoom.Items.Count > 0)
                    cbRoom.SelectedIndex = 0;

                LoadGeneratedFlowSemesters();
            }

            LayoutContent();
        }

        private void cbViewMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_suspendEditorHandlers)
                return;

            dgv.DataSource = null;
            ResetEditor();
            UpdateViewModeState();
        }

        private void cbSchedule_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_suspendEditorHandlers)
                return;

            if (!IsFlowMode())
                SyncFlowSemesterWithSelectedSchedule();

            LoadFilterGroupsForSelectedSchedule(true);
            LoadTeachersForSelectedSchedule(true);
            UpdateDeleteScheduleButtonState();

            if (IsEditMode())
            {
                dgv.DataSource = null;
                ResetEditor();
            }
        }

        private void cbFlowCourse_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_suspendEditorHandlers)
                return;

            LoadFlowDirections();
            LoadGeneratedFlowSemesters();
            dgv.DataSource = null;
        }

        private void cbFlowDirection_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_suspendEditorHandlers)
                return;

            LoadGeneratedFlowSemesters();
            dgv.DataSource = null;
        }

        private void cbFlowSemester_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_suspendEditorHandlers || !IsFlowMode())
                return;

            SelectLatestScheduleForFlowSemester();
            dgv.DataSource = null;
        }

        private void cbGroup_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_suspendEditorHandlers || !IsEditMode())
                return;

            if (!TryGetSelectedScheduleId(out int scheduleId))
                return;

            ResetEditor(true);
            ShowEditableWeekMatrix(scheduleId);
        }

        private void cbEditGroup_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_suspendEditorHandlers)
                return;

            LoadEditSubjectsForSelectedGroupAndSemester(null);
            LoadEditTeachersForSelectedSubject(null);
            LoadEditSecondTeachersForSelectedSubject(null);
            RefreshEditLessonTypeItems(true);
            ClearEditorSelection();
        }

        private void cbEditSubject_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_suspendEditorHandlers)
                return;

            LoadEditTeachersForSelectedSubject(null);
            LoadEditSecondTeachersForSelectedSubject(null);
            RefreshEditLessonTypeItems(true);
            ClearEditorSelection();
        }

        private void cbEditTeacher_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_suspendEditorHandlers)
                return;

            RefreshEditLessonTypeItems(true);
        }

        private void cbEditType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_suspendEditorHandlers)
                return;

            UpdateSecondTeacherControlState(_selectedSeries != null);
        }

        private void nudWeek_ValueChanged(object sender, EventArgs e)
        {
            if (_suspendEditorHandlers || !IsEditMode())
                return;

            if (!TryGetSelectedScheduleId(out int scheduleId))
                return;

            ResetEditor(true);
            ShowEditableWeekMatrix(scheduleId);
        }

        private Dictionary<int, string> LoadBellTimes()
        {
            var dict = new Dictionary<int, string>();
            DataTable dt = DataBase.Query(@"
SELECT НомерПары,
       CONVERT(varchar(5), ВремяНачала, 108) + '-' + CONVERT(varchar(5), ВремяОкончания, 108) AS Время
FROM dbo.Звонки
ORDER BY НомерПары;");

            foreach (DataRow r in dt.Rows)
                dict[Convert.ToInt32(r["НомерПары"])] = r["Время"].ToString();

            return dict;
        }

        private static string DayFull(int d)
        {
            switch (d)
            {
                case 1: return "Понедельник";
                case 2: return "Вторник";
                case 3: return "Среда";
                case 4: return "Четверг";
                case 5: return "Пятница";
                default: return string.Empty;
            }
        }

        private int GetMaxPairsFromBells(Dictionary<int, string> bellTimes)
        {
            if (bellTimes != null && bellTimes.Count > 0)
                return bellTimes.Keys.Max();

            return 5;
        }

        private void ConfigureMatrixGrid()
        {
            dgv.ReadOnly = true;
            dgv.AutoGenerateColumns = true;
            dgv.RowHeadersVisible = false;
            dgv.AllowUserToAddRows = false;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.MultiSelect = false;
            dgv.Font = new System.Drawing.Font("Segoe UI", 8F);
            dgv.ColumnHeadersDefaultCellStyle.Font = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Bold);
            dgv.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            dgv.DefaultCellStyle.Padding = new Padding(2);
            dgv.ColumnHeadersHeight = 28;
            dgv.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            dgv.RowTemplate.MinimumHeight = 34;

            dgv.CellPainting -= Dgv_CellPainting_DaySeparator;
            dgv.CellPainting += Dgv_CellPainting_DaySeparator;
        }

        private void ConfigureEditGrid()
        {
            dgv.ReadOnly = true;
            dgv.AutoGenerateColumns = true;
            dgv.RowHeadersVisible = false;
            dgv.AllowUserToAddRows = false;
            dgv.SelectionMode = DataGridViewSelectionMode.CellSelect;
            dgv.MultiSelect = false;
            dgv.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            dgv.ColumnHeadersDefaultCellStyle.Font = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Bold);
            dgv.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            dgv.DefaultCellStyle.Padding = new Padding(2);
            dgv.ColumnHeadersHeight = 28;
            dgv.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            dgv.RowTemplate.MinimumHeight = 42;
            dgv.CellPainting -= Dgv_CellPainting_DaySeparator;
            dgv.CellPainting += Dgv_CellPainting_DaySeparator;
        }

        private void ShowMatrix(int scheduleId)
        {
            ShowMatrix(scheduleId, null);
        }

        private void ShowMatrix(int scheduleId, List<string> fixedGroups)
        {
            string group = cbGroup.SelectedValue == null ? AllGroupsLabel : cbGroup.SelectedValue.ToString();
            string room = cbRoom.SelectedValue == null ? string.Empty : cbRoom.SelectedValue.ToString();
            int? teacherId = GetSelectedTeacherId();
            bool hasFixedGroups = fixedGroups != null;
            var fixedGroupSet = hasFixedGroups
                ? new HashSet<string>(fixedGroups.Where(x => !string.IsNullOrWhiteSpace(x)), StringComparer.OrdinalIgnoreCase)
                : null;

            bool hasGroup = !hasFixedGroups
                            && !string.IsNullOrWhiteSpace(group)
                            && !string.Equals(group, AllGroupsLabel, StringComparison.OrdinalIgnoreCase);
            bool hasRoom = !hasFixedGroups && !string.IsNullOrWhiteSpace(room);
            bool hasTeacher = !hasFixedGroups && teacherId.HasValue;

            var bellTimes = LoadBellTimes();
            int maxPairs = GetMaxPairsFromBells(bellTimes);
            int semester = GetSelectedScheduleSemester();

            ConfigureMatrixGrid();

            DataTable src = DataBase.Query(@"
;WITH base AS (
    SELECT
        CAST(rz.ДеньНедели AS int) AS ДеньНедели,
        CAST(rz.НомерПары AS int) AS НомерПары,
        rz.НазваниеГруппы,
        rz.IDПредмет,
        p.Название AS Предмет,
        rz.ТипЗанятия,
        rz.IDПреподаватель,
        t.ФИО AS Преподаватель,
        rz.IDПреподаватель2,
        t2.ФИО AS Преподаватель2,
        CAST(0 AS bit) AS ЭтоФизрук,
        CASE
            WHEN rz.ТипЗанятия = N'Физкультура' THEN N''
            ELSE rz.НомерАудитории
        END AS НомерАудитории
    FROM dbo.РасписаниеЗанятий rz
    JOIN dbo.Предметы p ON p.IDПредмет = rz.IDПредмет
    LEFT JOIN dbo.Преподаватели t ON t.IDПреподаватель = rz.IDПреподаватель
    LEFT JOIN dbo.Преподаватели t2 ON t2.IDПреподаватель = rz.IDПреподаватель2
    WHERE rz.IDРасписание = @sid
)
SELECT
    b.ДеньНедели,
    b.НомерПары,
    b.НазваниеГруппы,
    b.IDПредмет,
    b.Предмет,
    b.ТипЗанятия,
    b.IDПреподаватель,
    b.Преподаватель,
    b.IDПреподаватель2,
    b.Преподаватель2,
    b.ЭтоФизрук,
    b.НомерАудитории,
    STUFF((
        SELECT N', ' + CAST(rz2.НомерНедели AS nvarchar(10))
        FROM dbo.РасписаниеЗанятий rz2
        WHERE rz2.IDРасписание = @sid
          AND rz2.ДеньНедели = b.ДеньНедели
          AND rz2.НомерПары = b.НомерПары
          AND rz2.НазваниеГруппы = b.НазваниеГруппы
          AND rz2.IDПредмет = b.IDПредмет
          AND rz2.ТипЗанятия = b.ТипЗанятия
          AND ((rz2.IDПреподаватель = b.IDПреподаватель) OR (rz2.IDПреподаватель IS NULL AND b.IDПреподаватель IS NULL))
          AND ((rz2.IDПреподаватель2 = b.IDПреподаватель2) OR (rz2.IDПреподаватель2 IS NULL AND b.IDПреподаватель2 IS NULL))
          AND (b.ТипЗанятия = N'Физкультура' OR rz2.НомерАудитории = b.НомерАудитории)
        ORDER BY rz2.НомерНедели
        FOR XML PATH(''), TYPE
    ).value('.', 'nvarchar(max)'), 1, 2, N'') AS Недели
FROM base b
GROUP BY
    b.ДеньНедели,
    b.НомерПары,
    b.НазваниеГруппы,
    b.IDПредмет,
    b.Предмет,
    b.ТипЗанятия,
    b.IDПреподаватель,
    b.Преподаватель,
    b.IDПреподаватель2,
    b.Преподаватель2,
    b.ЭтоФизрук,
    b.НомерАудитории
ORDER BY b.ДеньНедели, b.НомерПары, b.НазваниеГруппы;",
                new SqlParameter("@sid", scheduleId));

            var filteredItems = src.AsEnumerable()
                .Where(x =>
                    (!hasFixedGroups || fixedGroupSet.Contains(x.Field<string>("НазваниеГруппы"))) &&
                    (!hasGroup || string.Equals(x.Field<string>("НазваниеГруппы"), group, StringComparison.OrdinalIgnoreCase)) &&
                    (!hasTeacher || x.Field<int?>("IDПреподаватель") == teacherId.Value || x.Field<int?>("IDПреподаватель2") == teacherId.Value) &&
                    (!hasRoom || string.Equals(x.Field<string>("НомерАудитории") ?? string.Empty, room, StringComparison.OrdinalIgnoreCase)))
                .ToList();
            filteredItems = MergeLessonRowsForDisplay(filteredItems);

            var groupsWithDisplayedLessons = new HashSet<string>(
                filteredItems
                    .Select(row => row.Field<string>("НазваниеГруппы"))
                    .Where(name => !string.IsNullOrWhiteSpace(name)),
                StringComparer.OrdinalIgnoreCase);

            List<string> groups;
            if (hasFixedGroups)
            {
                groups = fixedGroups
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Where(x => groupsWithDisplayedLessons.Contains(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
            else if (hasGroup)
            {
                groups = groupsWithDisplayedLessons.Contains(group)
                    ? new List<string> { group }
                    : new List<string>();
            }
            else
            {
                groups = groupsWithDisplayedLessons
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList();
            }

            if (groups.Count == 0)
            {
                dgv.DataSource = null;
                return;
            }

            BindGroupMatrix(filteredItems, groups, bellTimes, maxPairs, semester);
        }

        private void BindGroupMatrix(
            List<DataRow> itemsForDisplay,
            List<string> groups,
            Dictionary<int, string> bellTimes,
            int maxPairs,
            int semester)
        {
            Dictionary<string, int> semesterWeeksByGroup = GetSemesterWeeksByGroup(groups, semester);

            DataTable pivot = new DataTable();
            pivot.Columns.Add("День", typeof(string));
            pivot.Columns.Add("Время", typeof(string));

            foreach (string itemGroup in groups)
                pivot.Columns.Add(itemGroup, typeof(string));

            for (int day = 1; day <= 5; day++)
            {
                for (int pair = 1; pair <= maxPairs; pair++)
                {
                    DataRow pr = pivot.NewRow();
                    pr["День"] = pair == 1 ? DayFull(day) : string.Empty;
                    pr["Время"] = bellTimes.ContainsKey(pair) ? bellTimes[pair] : string.Empty;

                    foreach (string itemGroup in groups)
                        pr[itemGroup] = string.Empty;

                    var slotItems = itemsForDisplay
                        .Where(x => x.Field<int>("ДеньНедели") == day
                                 && x.Field<int>("НомерПары") == pair)
                        .ToList();

                    foreach (DataRow item in slotItems)
                    {
                        string itemGroup = item.Field<string>("НазваниеГруппы");
                        if (!pivot.Columns.Contains(itemGroup))
                            continue;

                        string cell =
                            FormatSubjectWithLessonType(item) + "\n" +
                            FormatTeacherDisplay(item) +
                            FormatRoomLine(item);

                        string weeksCsv = BuildWeeksDisplayText(
                            item.Field<string>("Недели"),
                            itemGroup,
                            semesterWeeksByGroup);
                        if (!string.IsNullOrWhiteSpace(weeksCsv))
                            cell += "\nНедели: " + weeksCsv;

                        if (!string.IsNullOrWhiteSpace(Convert.ToString(pr[itemGroup])))
                            pr[itemGroup] = Convert.ToString(pr[itemGroup]) + "\n---\n" + cell;
                        else
                            pr[itemGroup] = cell;
                    }

                    pivot.Rows.Add(pr);
                }
            }

            dgv.DataSource = pivot;

            dgv.Columns["День"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            dgv.Columns["Время"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;

            foreach (string itemGroup in groups)
                dgv.Columns[itemGroup].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        }

        private void ShowTeacherSemester(int scheduleId, int teacherId)
        {
            var bellTimes = LoadBellTimes();
            int maxPairs = GetMaxPairsFromBells(bellTimes);
            int semester = GetSelectedScheduleSemester();
            int seasonParity = Math.Abs(semester % 2);

            ConfigureMatrixGrid();

            DataTable src = DataBase.Query(@"
;WITH season_schedules AS (
    SELECT
        IDРасписание,
        Семестр
    FROM dbo.Расписания
    WHERE Семестр BETWEEN 1 AND 10
      AND (Семестр % 2) = @seasonParity
),
base AS (
    SELECT
        ss.IDРасписание,
        ss.Семестр,
        CAST(rz.ДеньНедели AS int) AS ДеньНедели,
        CAST(rz.НомерПары AS int) AS НомерПары,
        rz.НазваниеГруппы,
        rz.IDПредмет,
        p.Название AS Предмет,
        rz.ТипЗанятия,
        rz.IDПреподаватель,
        rz.IDПреподаватель2,
        CAST(0 AS bit) AS ЭтоФизрук,
        CASE
            WHEN rz.ТипЗанятия = N'Физкультура' THEN N''
            ELSE rz.НомерАудитории
        END AS НомерАудитории
    FROM dbo.РасписаниеЗанятий rz
    JOIN season_schedules ss ON ss.IDРасписание = rz.IDРасписание
    JOIN dbo.Предметы p ON p.IDПредмет = rz.IDПредмет
    LEFT JOIN dbo.Преподаватели t ON t.IDПреподаватель = rz.IDПреподаватель
    WHERE rz.IDПреподаватель = @teacherId
       OR rz.IDПреподаватель2 = @teacherId
)
SELECT
    b.IDРасписание,
    b.Семестр,
    b.ДеньНедели,
    b.НомерПары,
    b.НазваниеГруппы,
    b.IDПредмет,
    b.Предмет,
    b.ТипЗанятия,
    b.IDПреподаватель,
    b.IDПреподаватель2,
    b.ЭтоФизрук,
    b.НомерАудитории,
    STUFF((
        SELECT N', ' + CAST(rz2.НомерНедели AS nvarchar(10))
        FROM dbo.РасписаниеЗанятий rz2
        WHERE rz2.IDРасписание = b.IDРасписание
          AND (rz2.IDПреподаватель = @teacherId OR rz2.IDПреподаватель2 = @teacherId)
          AND rz2.ДеньНедели = b.ДеньНедели
          AND rz2.НомерПары = b.НомерПары
          AND rz2.НазваниеГруппы = b.НазваниеГруппы
          AND rz2.IDПредмет = b.IDПредмет
          AND rz2.ТипЗанятия = b.ТипЗанятия
          AND ((rz2.IDПреподаватель = b.IDПреподаватель) OR (rz2.IDПреподаватель IS NULL AND b.IDПреподаватель IS NULL))
          AND ((rz2.IDПреподаватель2 = b.IDПреподаватель2) OR (rz2.IDПреподаватель2 IS NULL AND b.IDПреподаватель2 IS NULL))
          AND (b.ТипЗанятия = N'Физкультура' OR rz2.НомерАудитории = b.НомерАудитории)
        ORDER BY rz2.НомерНедели
        FOR XML PATH(''), TYPE
    ).value('.', 'nvarchar(max)'), 1, 2, N'') AS Недели
FROM base b
GROUP BY
    b.IDРасписание,
    b.Семестр,
    b.ДеньНедели,
    b.НомерПары,
    b.НазваниеГруппы,
    b.IDПредмет,
    b.Предмет,
    b.ТипЗанятия,
    b.IDПреподаватель,
    b.IDПреподаватель2,
    b.ЭтоФизрук,
    b.НомерАудитории
ORDER BY b.ДеньНедели, b.НомерПары, b.НазваниеГруппы, b.Предмет;",
                new SqlParameter("@seasonParity", seasonParity),
                new SqlParameter("@teacherId", teacherId));

            List<string> groups = src.AsEnumerable()
                .Select(r => r.Field<string>("НазваниеГруппы"))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            if (groups.Count == 0)
            {
                dgv.DataSource = null;
                MessageBox.Show("У выбранного преподавателя нет занятий в расписаниях этого учебного периода.");
                return;
            }

            Dictionary<string, int> semesterWeeksByGroup = GetSemesterWeeksByGroup(groups, semester);
            List<DataRow> teacherItems = MergeLessonRowsForDisplay(src.AsEnumerable().ToList());

            DataTable pivot = new DataTable();
            pivot.Columns.Add("День", typeof(string));
            pivot.Columns.Add("Время", typeof(string));
            pivot.Columns.Add("Занятия", typeof(string));

            for (int day = 1; day <= 5; day++)
            {
                for (int pair = 1; pair <= maxPairs; pair++)
                {
                    DataRow pr = pivot.NewRow();
                    pr["День"] = pair == 1 ? DayFull(day) : string.Empty;
                    pr["Время"] = bellTimes.ContainsKey(pair) ? bellTimes[pair] : string.Empty;
                    pr["Занятия"] = string.Empty;

                    var items = teacherItems
                        .Where(x => x.Field<int>("ДеньНедели") == day
                                 && x.Field<int>("НомерПары") == pair)
                        .ToList();

                    var cellBlocks = items
                        .GroupBy(BuildTeacherSemesterCellBlockKey)
                        .Select(group =>
                        {
                            DataRow item = group.First();
                            string groupNames = string.Join(", ",
                                group.Select(row => Convert.ToString(row["НазваниеГруппы"]))
                                    .Where(name => !string.IsNullOrWhiteSpace(name))
                                    .Distinct(StringComparer.OrdinalIgnoreCase)
                                    .OrderBy(name => name));

                            string cell = FormatSubjectWithLessonType(item);

                            string room = FormatRoomText(item);
                            if (!string.IsNullOrWhiteSpace(room))
                                cell += "\n" + room;

                            if (!string.IsNullOrWhiteSpace(groupNames))
                                cell += "\nГруппы: " + groupNames;

                            string weeksCsv = BuildWeeksDisplayText(
                                group.SelectMany(row => ParseWeeksCsv(Convert.ToString(row["Недели"]))),
                                Convert.ToString(item["НазваниеГруппы"]),
                                semesterWeeksByGroup);
                            if (!string.IsNullOrWhiteSpace(weeksCsv))
                                cell += "\nНедели: " + weeksCsv;

                            return cell;
                        })
                        .OrderBy(text => text, StringComparer.CurrentCultureIgnoreCase)
                        .ToList();

                    pr["Занятия"] = string.Join("\n---\n", cellBlocks);
                    pivot.Rows.Add(pr);
                }
            }

            dgv.DataSource = pivot;

            if (dgv.Columns.Count == 0)
                return;

            dgv.Columns["День"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            dgv.Columns["Время"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            dgv.Columns["Занятия"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        }

        private static string BuildTeacherSemesterCellBlockKey(DataRow row)
        {
            return string.Join("\u001F",
                Convert.ToString(row["IDПредмет"]),
                Convert.ToString(row["ТипЗанятия"]),
                Convert.ToString(row["IDПреподаватель"]),
                Convert.ToString(row["IDПреподаватель2"]),
                Convert.ToString(row["НомерАудитории"]),
                Convert.ToString(row["Недели"]));
        }

        private List<string> GetScheduleGroups(int scheduleId, bool hasGroup, string group)
        {
            if (hasGroup)
                return new List<string> { group };

            DataTable dtGroups = DataBase.Query(@"
SELECT DISTINCT НазваниеГруппы
FROM dbo.РасписаниеЗанятий
WHERE IDРасписание = @sid
ORDER BY НазваниеГруппы;",
                new SqlParameter("@sid", scheduleId));

            return dtGroups.AsEnumerable()
                .Select(r => r.Field<string>("НазваниеГруппы"))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .OrderBy(x => x)
                .ToList();
        }

        private List<string> GetFlowGroups()
        {
            int? course = GetSelectedFlowCourse();
            string direction = GetSelectedFlowDirection();

            DataTable dtGroups = DataBase.Query(@"
SELECT DISTINCT
    НазваниеГруппы,
    Курс,
    ISNULL(NULLIF(LTRIM(RTRIM(Поток)), N''), Направление) AS Поток
FROM dbo.Группы
WHERE (@course IS NULL OR Курс = @course)
  AND (@direction = N'' OR ISNULL(NULLIF(LTRIM(RTRIM(Поток)), N''), Направление) = @direction)
ORDER BY Курс, Поток, НазваниеГруппы;",
                new SqlParameter("@course", (object)course ?? DBNull.Value),
                new SqlParameter("@direction", direction ?? string.Empty));

            return dtGroups.AsEnumerable()
                .Select(row => row.Field<string>("НазваниеГруппы"))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private void ShowFlowMatrix()
        {
            List<string> groups = GetFlowGroups();
            if (groups.Count == 0)
            {
                dgv.DataSource = null;
                MessageBox.Show("Для выбранного потока нет групп в справочнике.");
                return;
            }

            int? course = GetSelectedFlowCourse();
            int semester = GetSelectedFlowSemester().Value;
            string direction = GetSelectedFlowDirection();
            var bellTimes = LoadBellTimes();
            int maxPairs = GetMaxPairsFromBells(bellTimes);

            ConfigureMatrixGrid();

            DataTable src = DataBase.Query(@"
;WITH flow_groups AS (
    SELECT
        НазваниеГруппы,
        Курс,
        ISNULL(NULLIF(LTRIM(RTRIM(Поток)), N''), Направление) AS Поток
    FROM dbo.Группы
    WHERE (@course IS NULL OR Курс = @course)
      AND (@direction = N'' OR ISNULL(NULLIF(LTRIM(RTRIM(Поток)), N''), Направление) = @direction)
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
),
base AS (
    SELECT
        rz.IDРасписание,
        CAST(rz.ДеньНедели AS int) AS ДеньНедели,
        CAST(rz.НомерПары AS int) AS НомерПары,
        rz.НазваниеГруппы,
        rz.IDПредмет,
        p.Название AS Предмет,
        rz.ТипЗанятия,
        rz.IDПреподаватель,
        t.ФИО AS Преподаватель,
        rz.IDПреподаватель2,
        t2.ФИО AS Преподаватель2,
        CAST(0 AS bit) AS ЭтоФизрук,
        CASE
            WHEN rz.ТипЗанятия = N'Физкультура' THEN N''
            ELSE rz.НомерАудитории
        END AS НомерАудитории
    FROM dbo.РасписаниеЗанятий rz
    JOIN latest ls
        ON ls.IDРасписание = rz.IDРасписание
       AND ls.НазваниеГруппы = rz.НазваниеГруппы
    JOIN dbo.Предметы p ON p.IDПредмет = rz.IDПредмет
    LEFT JOIN dbo.Преподаватели t ON t.IDПреподаватель = rz.IDПреподаватель
    LEFT JOIN dbo.Преподаватели t2 ON t2.IDПреподаватель = rz.IDПреподаватель2
)
SELECT
    b.ДеньНедели,
    b.НомерПары,
    b.НазваниеГруппы,
    b.IDПредмет,
    b.Предмет,
    b.ТипЗанятия,
    b.IDПреподаватель,
    b.Преподаватель,
    b.IDПреподаватель2,
    b.Преподаватель2,
    b.ЭтоФизрук,
    b.НомерАудитории,
    STUFF((
        SELECT N', ' + CAST(rz2.НомерНедели AS nvarchar(10))
        FROM dbo.РасписаниеЗанятий rz2
        WHERE rz2.IDРасписание = b.IDРасписание
          AND rz2.ДеньНедели = b.ДеньНедели
          AND rz2.НомерПары = b.НомерПары
          AND rz2.НазваниеГруппы = b.НазваниеГруппы
          AND rz2.IDПредмет = b.IDПредмет
          AND rz2.ТипЗанятия = b.ТипЗанятия
          AND ((rz2.IDПреподаватель = b.IDПреподаватель) OR (rz2.IDПреподаватель IS NULL AND b.IDПреподаватель IS NULL))
          AND ((rz2.IDПреподаватель2 = b.IDПреподаватель2) OR (rz2.IDПреподаватель2 IS NULL AND b.IDПреподаватель2 IS NULL))
          AND (b.ТипЗанятия = N'Физкультура' OR rz2.НомерАудитории = b.НомерАудитории)
        ORDER BY rz2.НомерНедели
        FOR XML PATH(''), TYPE
    ).value('.', 'nvarchar(max)'), 1, 2, N'') AS Недели
FROM base b
GROUP BY
    b.IDРасписание,
    b.ДеньНедели,
    b.НомерПары,
    b.НазваниеГруппы,
    b.IDПредмет,
    b.Предмет,
    b.ТипЗанятия,
    b.IDПреподаватель,
    b.Преподаватель,
    b.IDПреподаватель2,
    b.Преподаватель2,
    b.ЭтоФизрук,
    b.НомерАудитории
ORDER BY b.ДеньНедели, b.НомерПары, b.НазваниеГруппы;",
                new SqlParameter("@course", (object)course ?? DBNull.Value),
                new SqlParameter("@direction", direction ?? string.Empty),
                new SqlParameter("@semester", semester));

            List<DataRow> items = MergeLessonRowsForDisplay(src.AsEnumerable().ToList());
            BindGroupMatrix(items, groups, bellTimes, maxPairs, semester);
        }

        private void ShowEditableWeekMatrix(int scheduleId)
        {
            bool showFullSemesterGrid = Environment.TickCount >= int.MinValue;
            if (showFullSemesterGrid)
            {
                string group = cbGroup.SelectedValue == null ? AllGroupsLabel : Convert.ToString(cbGroup.SelectedValue);
                bool hasGroup = !string.IsNullOrWhiteSpace(group)
                                && !string.Equals(group, AllGroupsLabel, StringComparison.OrdinalIgnoreCase);

                var bellTimes = LoadBellTimes();
                int maxPairs = GetMaxPairsFromBells(bellTimes);
                int semester = GetSelectedScheduleSemester();

                ConfigureEditGrid();

                DataTable src = DataBase.Query(@"
;WITH base AS (
    SELECT
        CAST(rz.ДеньНедели AS int) AS ДеньНедели,
        CAST(rz.НомерПары AS int) AS НомерПары,
        rz.НазваниеГруппы,
        rz.IDПредмет,
        p.Название AS Предмет,
        rz.ТипЗанятия,
        rz.IDПреподаватель,
        t.ФИО AS Преподаватель,
        rz.IDПреподаватель2,
        t2.ФИО AS Преподаватель2,
        CAST(0 AS bit) AS ЭтоФизрук,
        CASE
            WHEN rz.ТипЗанятия = N'Физкультура' THEN N''
            ELSE rz.НомерАудитории
        END AS НомерАудитории
    FROM dbo.РасписаниеЗанятий rz
    JOIN dbo.Предметы p ON p.IDПредмет = rz.IDПредмет
    LEFT JOIN dbo.Преподаватели t ON t.IDПреподаватель = rz.IDПреподаватель
    LEFT JOIN dbo.Преподаватели t2 ON t2.IDПреподаватель = rz.IDПреподаватель2
    WHERE rz.IDРасписание = @sid
)
SELECT
    b.ДеньНедели,
    b.НомерПары,
    b.НазваниеГруппы,
    b.IDПредмет,
    b.Предмет,
    b.ТипЗанятия,
    b.IDПреподаватель,
    b.Преподаватель,
    b.IDПреподаватель2,
    b.Преподаватель2,
    b.ЭтоФизрук,
    b.НомерАудитории,
    STUFF((
        SELECT N', ' + CAST(rz2.НомерНедели AS nvarchar(10))
        FROM dbo.РасписаниеЗанятий rz2
        WHERE rz2.IDРасписание = @sid
          AND rz2.ДеньНедели = b.ДеньНедели
          AND rz2.НомерПары = b.НомерПары
          AND rz2.НазваниеГруппы = b.НазваниеГруппы
          AND rz2.IDПредмет = b.IDПредмет
          AND rz2.ТипЗанятия = b.ТипЗанятия
          AND ((rz2.IDПреподаватель = b.IDПреподаватель) OR (rz2.IDПреподаватель IS NULL AND b.IDПреподаватель IS NULL))
          AND ((rz2.IDПреподаватель2 = b.IDПреподаватель2) OR (rz2.IDПреподаватель2 IS NULL AND b.IDПреподаватель2 IS NULL))
          AND (b.ТипЗанятия = N'Физкультура' OR rz2.НомерАудитории = b.НомерАудитории)
        ORDER BY rz2.НомерНедели
        FOR XML PATH(''), TYPE
    ).value('.', 'nvarchar(max)'), 1, 2, N'') AS Недели
FROM base b
GROUP BY
    b.ДеньНедели,
    b.НомерПары,
    b.НазваниеГруппы,
    b.IDПредмет,
    b.Предмет,
    b.ТипЗанятия,
    b.IDПреподаватель,
    b.Преподаватель,
    b.IDПреподаватель2,
    b.Преподаватель2,
    b.ЭтоФизрук,
    b.НомерАудитории
ORDER BY b.ДеньНедели, b.НомерПары, b.НазваниеГруппы;",
                    new SqlParameter("@sid", scheduleId));

                var filteredItems = src.AsEnumerable()
                    .Where(x => !hasGroup || string.Equals(x.Field<string>("НазваниеГруппы"), group, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                filteredItems = MergeLessonRowsForDisplay(filteredItems);

                List<string> groups = GetScheduleGroups(scheduleId, hasGroup, group);
                if (groups.Count == 0)
                {
                    dgv.DataSource = null;
                    MessageBox.Show("Нет групп для отображения (расписание пустое).");
                    return;
                }

                Dictionary<string, int> semesterWeeksByGroup = GetSemesterWeeksByGroup(groups, semester);

                DataTable pivot = new DataTable();
                pivot.Columns.Add(EditDayKeyColumn, typeof(int));
                pivot.Columns.Add(EditPairKeyColumn, typeof(int));
                pivot.Columns.Add("День", typeof(string));
                pivot.Columns.Add("Время", typeof(string));

                foreach (string itemGroup in groups)
                    pivot.Columns.Add(itemGroup, typeof(string));

                for (int day = 1; day <= 5; day++)
                {
                    for (int pair = 1; pair <= maxPairs; pair++)
                    {
                        DataRow pr = pivot.NewRow();
                        pr[EditDayKeyColumn] = day;
                        pr[EditPairKeyColumn] = pair;
                        pr["День"] = pair == 1 ? DayFull(day) : string.Empty;
                        pr["Время"] = bellTimes.ContainsKey(pair) ? bellTimes[pair] : string.Empty;

                        foreach (string itemGroup in groups)
                            pr[itemGroup] = string.Empty;

                        var items = filteredItems
                            .Where(x => x.Field<int>("ДеньНедели") == day
                                     && x.Field<int>("НомерПары") == pair)
                            .ToList();

                        foreach (DataRow item in items)
                        {
                            string itemGroup = item.Field<string>("НазваниеГруппы");
                            if (!pivot.Columns.Contains(itemGroup))
                                continue;

                            string cell =
                                FormatSubjectWithLessonType(item) + "\n" +
                                FormatTeacherDisplay(item) +
                                FormatRoomLine(item);

                            string weeksCsv = BuildWeeksDisplayText(
                                item.Field<string>("Недели"),
                                itemGroup,
                                semesterWeeksByGroup);
                            if (!string.IsNullOrWhiteSpace(weeksCsv))
                                cell += "\nНедели: " + weeksCsv;

                            if (!string.IsNullOrWhiteSpace(Convert.ToString(pr[itemGroup])))
                                pr[itemGroup] = Convert.ToString(pr[itemGroup]) + "\n---\n" + cell;
                            else
                                pr[itemGroup] = cell;
                        }

                        pivot.Rows.Add(pr);
                    }
                }

                dgv.DataSource = pivot;

                if (dgv.Columns.Contains(EditDayKeyColumn))
                    dgv.Columns[EditDayKeyColumn].Visible = false;

                if (dgv.Columns.Contains(EditPairKeyColumn))
                    dgv.Columns[EditPairKeyColumn].Visible = false;

                dgv.Columns["День"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                dgv.Columns["Время"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;

                foreach (string itemGroup in groups)
                    dgv.Columns[itemGroup].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

                dgv.ClearSelection();
                return;
            }

#if false
            int viewWeek = (int)nudWeek.Value;

            var bellTimes = LoadBellTimes();
            int maxPairs = GetMaxPairsFromBells(bellTimes);

            ConfigureEditGrid();

            DataTable src = DataBase.Query(@"
SELECT
    rz.IDЗапись,
    CAST(rz.ДеньНедели AS int) AS ДеньНедели,
    CAST(rz.НомерПары AS int) AS НомерПары,
    rz.НазваниеГруппы,
    rz.IDПредмет,
    p.Название AS Предмет,
    rz.ТипЗанятия,
    rz.IDПреподаватель,
    t.ФИО AS Преподаватель,
    CAST(0 AS bit) AS ЭтоФизрук,
    rz.НомерАудитории,
    rz.СвязкаЗанятия,
    rz.IDУчебныйПлан
FROM dbo.РасписаниеЗанятий rz
JOIN dbo.Предметы p ON p.IDПредмет = rz.IDПредмет
LEFT JOIN dbo.Преподаватели t ON t.IDПреподаватель = rz.IDПреподаватель
WHERE rz.IDРасписание = @sid
  AND rz.НомерНедели = @week;",
                new SqlParameter("@sid", scheduleId),
                new SqlParameter("@week", viewWeek));

            var filteredItems = src.AsEnumerable().ToList();
            filteredItems = MergeLessonRowsForDisplay(filteredItems);

            List<string> groups = GetScheduleGroups(scheduleId, false, string.Empty);
            if (groups.Count == 0)
            {
                dgv.DataSource = null;
                MessageBox.Show("Нет групп для отображения (расписание пустое).");
                return;
            }

            DataTable pivot = new DataTable();
            pivot.Columns.Add(EditDayKeyColumn, typeof(int));
            pivot.Columns.Add(EditPairKeyColumn, typeof(int));
            pivot.Columns.Add("День", typeof(string));
            pivot.Columns.Add("Время", typeof(string));

            foreach (string itemGroup in groups)
                pivot.Columns.Add(itemGroup, typeof(string));

            for (int day = 1; day <= 5; day++)
            {
                for (int pair = 1; pair <= maxPairs; pair++)
                {
                    DataRow pr = pivot.NewRow();
                    pr[EditDayKeyColumn] = day;
                    pr[EditPairKeyColumn] = pair;
                    pr["День"] = pair == 1 ? DayFull(day) : string.Empty;
                    pr["Время"] = bellTimes.ContainsKey(pair) ? bellTimes[pair] : string.Empty;

                    foreach (string itemGroup in groups)
                        pr[itemGroup] = string.Empty;

                    var items = filteredItems
                        .Where(x => x.Field<int>("ДеньНедели") == day
                                 && x.Field<int>("НомерПары") == pair)
                        .ToList();

                    foreach (DataRow item in items)
                    {
                        string itemGroup = item.Field<string>("НазваниеГруппы");
                        if (!pivot.Columns.Contains(itemGroup))
                            continue;

                        string cell =
                            FormatSubjectWithLessonType(item) + "\n" +
                            item.Field<string>("Преподаватель") +
                            FormatRoomLine(item);

                        if (!string.IsNullOrWhiteSpace(Convert.ToString(pr[itemGroup])))
                            pr[itemGroup] = Convert.ToString(pr[itemGroup]) + "\n---\n" + cell;
                        else
                            pr[itemGroup] = cell;
                    }

                    pivot.Rows.Add(pr);
                }
            }

            dgv.DataSource = pivot;

            if (dgv.Columns.Contains(EditDayKeyColumn))
                dgv.Columns[EditDayKeyColumn].Visible = false;

            if (dgv.Columns.Contains(EditPairKeyColumn))
                dgv.Columns[EditPairKeyColumn].Visible = false;

            dgv.Columns["День"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            dgv.Columns["Время"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;

            foreach (string itemGroup in groups)
                dgv.Columns[itemGroup].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            dgv.ClearSelection();
#endif
        }

        private bool IsEditGroupColumn(int columnIndex)
        {
            if (columnIndex < 0 || columnIndex >= dgv.Columns.Count)
                return false;

            string columnName = dgv.Columns[columnIndex].Name;
            return !string.Equals(columnName, EditDayKeyColumn, StringComparison.Ordinal)
                && !string.Equals(columnName, EditPairKeyColumn, StringComparison.Ordinal)
                && !string.Equals(columnName, "День", StringComparison.Ordinal)
                && !string.Equals(columnName, "Время", StringComparison.Ordinal);
        }

        private void LoadEditorFromSelectedCell(int rowIndex, int columnIndex)
        {
            LoadEditorFromSelectedCell(rowIndex, columnIndex, null);
        }

        private void LoadEditorFromSelectedCell(int rowIndex, int columnIndex, ScheduleSeriesData preferredSeries)
        {
            if (!TryGetSelectedScheduleId(out int scheduleId))
                return;

            if (!IsEditGroupColumn(columnIndex) || rowIndex < 0 || rowIndex >= dgv.Rows.Count)
            {
                ResetEditor(true);
                return;
            }

            DataGridViewRow gridRow = dgv.Rows[rowIndex];
            if (gridRow.IsNewRow)
            {
                ResetEditor(true);
                return;
            }

            string cellText = Convert.ToString(gridRow.Cells[columnIndex].Value);
            if (string.IsNullOrWhiteSpace(cellText))
            {
                ResetEditor(true);
                return;
            }

            object dayValue = gridRow.Cells[EditDayKeyColumn].Value;
            object pairValue = gridRow.Cells[EditPairKeyColumn].Value;
            if (dayValue == null || dayValue == DBNull.Value
                || pairValue == null || pairValue == DBNull.Value)
            {
                ResetEditor(true);
                return;
            }

            int day = Convert.ToInt32(dayValue);
            int pair = Convert.ToInt32(pairValue);
            string groupName = dgv.Columns[columnIndex].Name;

            DataTable slotRows = LoadSlotRowsForEditCell(scheduleId, day, pair, groupName);
            if (slotRows.Rows.Count == 0)
            {
                ResetEditor(true);
                return;
            }

            if (preferredSeries != null)
            {
                DataRow preferredRow = slotRows.AsEnumerable()
                    .FirstOrDefault(row => IsSameSeriesRow(preferredSeries, row));

                if (preferredRow != null)
                {
                    LoadEditorFromScheduleRow(preferredRow);
                    return;
                }
            }

            List<LessonVariantOption> options = BuildLessonVariantOptions(slotRows);
            if (options.Count == 1)
            {
                LoadEditorFromScheduleRow(options[0].RepresentativeRow);
                return;
            }

            if (!TryChooseLessonVariant(groupName, day, pair, options, out LessonVariantOption selectedOption))
            {
                ResetEditor(true);
                return;
            }

            LoadEditorFromScheduleRow(selectedOption.RepresentativeRow);
            return;

#if false
            int viewWeek = (int)nudWeek.Value;

            DataTable slotRows = DataBase.Query(@"
SELECT
    rz.IDЗапись,
    rz.IDРасписание,
    rz.НомерНедели,
    rz.ДеньНедели,
    rz.НомерПары,
    rz.НазваниеГруппы,
    rz.IDПредмет,
    p.Название AS Предмет,
    rz.ТипЗанятия,
    rz.IDПреподаватель,
    t.ФИО AS Преподаватель,
    rz.IDПреподаватель2,
    t2.ФИО AS Преподаватель2,
    CAST(0 AS bit) AS ЭтоФизрук,
    rz.НомерАудитории
FROM dbo.РасписаниеЗанятий rz
JOIN dbo.Предметы p ON p.IDПредмет = rz.IDПредмет
LEFT JOIN dbo.Преподаватели t ON t.IDПреподаватель = rz.IDПреподаватель
LEFT JOIN dbo.Преподаватели t2 ON t2.IDПреподаватель = rz.IDПреподаватель2
WHERE rz.IDРасписание = @sid
  AND rz.ДеньНедели = @day
  AND rz.НомерПары = @pair
  AND rz.НазваниеГруппы = @groupName
ORDER BY rz.НомерНедели, rz.IDЗапись;",
                new SqlParameter("@sid", scheduleId),
                new SqlParameter("@day", day),
                new SqlParameter("@pair", pair),
                new SqlParameter("@groupName", groupName));

            int seriesCount = slotRows.AsEnumerable()
                .Select(r => string.Join("|",
                    Convert.ToString(r["IDПредмет"]),
                    Convert.ToString(r["ТипЗанятия"]),
                    Convert.ToString(r["IDПреподаватель"]),
                    Convert.ToString(r["НомерАудитории"])))
                .Distinct()
                .Count();

            if (seriesCount == 1 && slotRows.Rows.Count > 0)
            {
                LoadEditorFromScheduleRow(slotRows.Rows[0]);
                return;
            }

            DataTable dt = DataBase.Query(@"
SELECT TOP (1)
    rz.IDЗапись,
    rz.IDРасписание,
    rz.НомерНедели,
    rz.ДеньНедели,
    rz.НомерПары,
    rz.НазваниеГруппы,
    rz.IDПредмет,
    p.Название AS Предмет,
    rz.ТипЗанятия,
    rz.IDПреподаватель,
    t.ФИО AS Преподаватель,
    rz.IDПреподаватель2,
    t2.ФИО AS Преподаватель2,
    rz.НомерАудитории,
    rz.СвязкаЗанятия,
    rz.IDУчебныйПлан
FROM dbo.РасписаниеЗанятий rz
JOIN dbo.Предметы p ON p.IDПредмет = rz.IDПредмет
LEFT JOIN dbo.Преподаватели t ON t.IDПреподаватель = rz.IDПреподаватель
WHERE rz.IDРасписание = @sid
  AND rz.НомерНедели = @week
  AND rz.ДеньНедели = @day
  AND rz.НомерПары = @pair
  AND rz.НазваниеГруппы = @groupName
ORDER BY rz.IDЗапись;",
                new SqlParameter("@sid", scheduleId),
                new SqlParameter("@week", viewWeek),
                new SqlParameter("@day", day),
                new SqlParameter("@pair", pair),
                new SqlParameter("@groupName", groupName));

            if (dt.Rows.Count == 0)
            {
                MessageBox.Show("В этой ячейке несколько разных занятий по разным неделям. Выберите сверху неделю варианта и нажмите по ячейке еще раз.",
                    "Выбор занятия", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ResetEditor(true);
                return;
            }

            LoadEditorFromScheduleRow(dt.Rows[0]);
#endif
        }

        private DataTable LoadSlotRowsForEditCell(int scheduleId, int day, int pair, string groupName)
        {
            return DataBase.Query(@"
SELECT
    rz.IDЗапись,
    rz.IDРасписание,
    rz.НомерНедели,
    rz.ДеньНедели,
    rz.НомерПары,
    rz.НазваниеГруппы,
    rz.IDПредмет,
    p.Название AS Предмет,
    rz.ТипЗанятия,
    rz.IDПреподаватель,
    t.ФИО AS Преподаватель,
    rz.IDПреподаватель2,
    t2.ФИО AS Преподаватель2,
    CAST(0 AS bit) AS ЭтоФизрук,
    rz.НомерАудитории
FROM dbo.РасписаниеЗанятий rz
JOIN dbo.Предметы p ON p.IDПредмет = rz.IDПредмет
LEFT JOIN dbo.Преподаватели t ON t.IDПреподаватель = rz.IDПреподаватель
LEFT JOIN dbo.Преподаватели t2 ON t2.IDПреподаватель = rz.IDПреподаватель2
WHERE rz.IDРасписание = @sid
  AND rz.ДеньНедели = @day
  AND rz.НомерПары = @pair
  AND rz.НазваниеГруппы = @groupName
ORDER BY rz.НомерНедели, rz.IDЗапись;",
                new SqlParameter("@sid", scheduleId),
                new SqlParameter("@day", day),
                new SqlParameter("@pair", pair),
                new SqlParameter("@groupName", groupName));
        }

        private List<LessonVariantOption> BuildLessonVariantOptions(DataTable slotRows)
        {
            int semester = GetSelectedScheduleSemester();
            Dictionary<string, int> semesterWeeksByGroup = GetSemesterWeeksByGroup(
                slotRows.AsEnumerable().Select(row => Convert.ToString(row["НазваниеГруппы"])),
                semester);

            return slotRows.AsEnumerable()
                .GroupBy(GetLessonVariantKey)
                .Select(group =>
                {
                    DataRow row = group.First();
                    string groupName = Convert.ToString(row["НазваниеГруппы"]);
                    string weeksText = BuildWeeksDisplayText(
                        group.Select(item => Convert.ToInt32(item["НомерНедели"])),
                        groupName,
                        semesterWeeksByGroup);

                    return new LessonVariantOption
                    {
                        RepresentativeRow = row,
                        DisplayText = BuildLessonVariantDisplayText(row, weeksText)
                    };
                })
                .OrderBy(option => option.DisplayText, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }

        private static string GetLessonVariantKey(DataRow row)
        {
            if (row.Table.Columns.Contains("IDУчебныйПлан") && row["IDУчебныйПлан"] != DBNull.Value)
            {
                return string.Join("|",
                    Convert.ToString(row["IDПредмет"]),
                    Convert.ToString(row["ТипЗанятия"]),
                    Convert.ToString(row["IDПреподаватель"]),
                    Convert.ToString(row["IDПреподаватель2"]),
                    Convert.ToString(row["IDУчебныйПлан"]));
            }

            return string.Join("|",
                Convert.ToString(row["IDПредмет"]),
                Convert.ToString(row["ТипЗанятия"]),
                Convert.ToString(row["IDПреподаватель"]),
                Convert.ToString(row["IDПреподаватель2"]),
                Convert.ToString(row["НомерАудитории"]));
        }

        private static string BuildLessonVariantDisplayText(DataRow row, string weeksText)
        {
            string subject = FormatSubjectWithLessonType(row);
            string teacher = FormatTeacherDisplay(row);
            string room = Convert.ToString(row["НомерАудитории"]);

            string roomPart = IsPhysicalScheduleItem(row)
                ? string.Empty
                : string.IsNullOrWhiteSpace(room)
                ? "аудитория не указана"
                : "ауд. " + room;

            var parts = new List<string> { subject };
            if (!string.IsNullOrWhiteSpace(teacher))
                parts.Add(teacher);
            if (!string.IsNullOrWhiteSpace(roomPart))
                parts.Add(roomPart);
            if (!string.IsNullOrWhiteSpace(weeksText))
                parts.Add("недели: " + weeksText);

            return string.Join(" | ", parts);
        }

        private bool TryChooseLessonVariant(
            string groupName,
            int day,
            int pair,
            List<LessonVariantOption> options,
            out LessonVariantOption selectedOption)
        {
            selectedOption = null;

            using (var dialog = new Form())
            {
                dialog.Text = "Выбор занятия";
                dialog.StartPosition = FormStartPosition.CenterParent;
                dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                dialog.MinimizeBox = false;
                dialog.MaximizeBox = false;
                dialog.ShowInTaskbar = false;
                dialog.ClientSize = new System.Drawing.Size(860, 320);

                var lblPrompt = new Label
                {
                    AutoSize = false,
                    Location = new System.Drawing.Point(12, 12),
                    Size = new System.Drawing.Size(836, 40),
                    Text = "В ячейке группы \"" + groupName + "\", " + DayFull(day) + ", пара " + pair
                        + " найдено несколько разных занятий. Выберите запись для редактирования."
                };

                var lbOptions = new ListBox
                {
                    FormattingEnabled = true,
                    HorizontalScrollbar = true,
                    IntegralHeight = false,
                    Location = new System.Drawing.Point(12, 60),
                    Name = "lbLessonVariants",
                    Size = new System.Drawing.Size(836, 214)
                };
                lbOptions.Items.AddRange(options.Cast<object>().ToArray());
                if (lbOptions.Items.Count > 0)
                    lbOptions.SelectedIndex = 0;

                var btnOk = new Button
                {
                    DialogResult = DialogResult.OK,
                    Location = new System.Drawing.Point(652, 284),
                    Size = new System.Drawing.Size(95, 28),
                    Text = "Выбрать"
                };

                var btnCancel = new Button
                {
                    DialogResult = DialogResult.Cancel,
                    Location = new System.Drawing.Point(753, 284),
                    Size = new System.Drawing.Size(95, 28),
                    Text = "Отмена"
                };

                lbOptions.DoubleClick += (sender, args) =>
                {
                    if (lbOptions.SelectedItem != null)
                        dialog.DialogResult = DialogResult.OK;
                };

                dialog.AcceptButton = btnOk;
                dialog.CancelButton = btnCancel;
                dialog.Controls.Add(lblPrompt);
                dialog.Controls.Add(lbOptions);
                dialog.Controls.Add(btnOk);
                dialog.Controls.Add(btnCancel);

                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return false;

                selectedOption = lbOptions.SelectedItem as LessonVariantOption;
                return selectedOption != null;
            }
        }

        private void LoadEditorFromScheduleRow(DataRow row)
        {
            _selectedEntryId = Convert.ToInt32(row["IDЗапись"]);
            _selectedSeries = new ScheduleSeriesData
            {
                EntryId = _selectedEntryId.Value,
                ScheduleId = Convert.ToInt32(row["IDРасписание"]),
                Day = Convert.ToInt32(row["ДеньНедели"]),
                Pair = Convert.ToInt32(row["НомерПары"]),
                GroupName = Convert.ToString(row["НазваниеГруппы"]),
                SubjectId = Convert.ToInt32(row["IDПредмет"]),
                LessonType = Convert.ToString(row["ТипЗанятия"]),
                TeacherId = ToNullableInt(row["IDПреподаватель"]),
                SecondTeacherId = ToNullableInt(row["IDПреподаватель2"]),
                Room = Convert.ToString(row["НомерАудитории"]),
                SeriesId = row.Table.Columns.Contains("СвязкаЗанятия") && row["СвязкаЗанятия"] != DBNull.Value
                    ? (Guid?)Guid.Parse(Convert.ToString(row["СвязкаЗанятия"]))
                    : null,
                CurriculumPlanId = row.Table.Columns.Contains("IDУчебныйПлан") && row["IDУчебныйПлан"] != DBNull.Value
                    ? (int?)Convert.ToInt32(row["IDУчебныйПлан"])
                    : null
            };

            _selectedSeries.Pair = GetSeriesStartPair(_selectedSeries);
            List<int> weeks = LoadSeriesWeeks(_selectedSeries);

            _suspendEditorHandlers = true;

            SelectComboBoxValue(cbEditDay, row["ДеньНедели"]);
            SelectComboBoxValue(cbEditPair, _selectedSeries.Pair);
            SelectComboBoxValue(cbEditGroup, _selectedSeries.GroupName);

            LoadEditSubjectsForSelectedGroupAndSemester(_selectedSeries.SubjectId);
            SelectComboBoxValue(cbEditSubject, _selectedSeries.SubjectId);

            LoadEditTeachersForSelectedSubject(_selectedSeries.TeacherId);
            SelectComboBoxValue(cbEditTeacher, _selectedSeries.TeacherId);
            LoadEditSecondTeachersForSelectedSubject(_selectedSeries.SecondTeacherId);
            SelectComboBoxValue(cbEditSecondTeacher, _selectedSeries.SecondTeacherId);
            RefreshEditLessonTypeItems(false);

            int typeIndex = cbEditType.FindStringExact(_selectedSeries.LessonType);
            cbEditType.SelectedIndex = typeIndex;
            SelectComboBoxValue(cbEditRoom, _selectedSeries.Room);
            txtEditWeeks.Text = string.Join(", ", weeks);

            _suspendEditorHandlers = false;

            SetEditorState(true);
        }

        private List<int> LoadSeriesWeeks(ScheduleSeriesData series)
        {
            if (IsCurriculumPlanSeries(series))
                return LoadCurriculumPlanSeriesWeeks(series);

            return ScheduleService.LoadSeriesWeeks(series);
        }

        private int GetSeriesStartPair(ScheduleSeriesData series)
        {
            if (IsCurriculumPlanSeries(series))
                return GetCurriculumPlanSeriesStartPair(series);

            return ScheduleService.GetSeriesStartPair(series);
        }

        private static bool IsCurriculumPlanSeries(ScheduleSeriesData series)
        {
            return series != null && series.CurriculumPlanId.HasValue;
        }

        private List<int> LoadCurriculumPlanSeriesWeeks(ScheduleSeriesData series)
        {
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
  AND IDУчебныйПлан = @planId
ORDER BY НомерНедели;",
                new SqlParameter("@sid", series.ScheduleId),
                new SqlParameter("@day", series.Day),
                new SqlParameter("@pair", series.Pair),
                new SqlParameter("@groupName", series.GroupName),
                new SqlParameter("@subjectId", series.SubjectId),
                new SqlParameter("@lessonType", series.LessonType),
                new SqlParameter("@teacherId", (object)series.TeacherId ?? DBNull.Value),
                new SqlParameter("@secondTeacherId", (object)series.SecondTeacherId ?? DBNull.Value),
                new SqlParameter("@planId", series.CurriculumPlanId.Value));

            return dt.AsEnumerable()
                .Select(row => Convert.ToInt32(row["НомерНедели"]))
                .Distinct()
                .OrderBy(week => week)
                .ToList();
        }

        private int GetCurriculumPlanSeriesStartPair(ScheduleSeriesData series)
        {
            if (series == null || !string.Equals(series.LessonType, "ЛБ", StringComparison.OrdinalIgnoreCase) || series.Pair <= 1)
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
  AND IDУчебныйПлан = @planId;",
                new SqlParameter("@sid", series.ScheduleId),
                new SqlParameter("@day", series.Day),
                new SqlParameter("@previousPair", series.Pair - 1),
                new SqlParameter("@groupName", series.GroupName),
                new SqlParameter("@subjectId", series.SubjectId),
                new SqlParameter("@lessonType", series.LessonType),
                new SqlParameter("@teacherId", (object)series.TeacherId ?? DBNull.Value),
                new SqlParameter("@secondTeacherId", (object)series.SecondTeacherId ?? DBNull.Value),
                new SqlParameter("@planId", series.CurriculumPlanId.Value));

            return Convert.ToInt32(value) > 0 ? series.Pair - 1 : series.Pair;
        }

        private bool TryParseWeeksList(string text, int maxWeek, out List<int> weeks, out string errorMessage)
        {
            weeks = new List<int>();
            errorMessage = null;

            if (string.IsNullOrWhiteSpace(text))
            {
                errorMessage = "Укажите недели, на которых должна остаться пара.";
                return false;
            }

            var values = new SortedSet<int>();
            string[] tokens = text.Split(new[] { ',', ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string rawToken in tokens)
            {
                string token = rawToken.Trim();
                if (string.IsNullOrWhiteSpace(token))
                    continue;

                string[] rangeParts = token.Split(new[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
                if (rangeParts.Length == 2)
                {
                    if (!int.TryParse(rangeParts[0].Trim(), out int startWeek) ||
                        !int.TryParse(rangeParts[1].Trim(), out int endWeek) ||
                        startWeek > endWeek)
                    {
                        errorMessage = "Неверный диапазон недель. Используйте формат 1-4, 6, 8.";
                        return false;
                    }

                    if (startWeek < 1 || endWeek > maxWeek)
                    {
                        errorMessage = "Номер недели выходит за пределы семестра.";
                        return false;
                    }

                    for (int week = startWeek; week <= endWeek; week++)
                        values.Add(week);

                    continue;
                }

                if (!int.TryParse(token, out int singleWeek))
                {
                    errorMessage = "Список недель заполнен неверно. Используйте числа через запятую.";
                    return false;
                }

                if (singleWeek < 1 || singleWeek > maxWeek)
                {
                    errorMessage = "Номер недели выходит за пределы семестра.";
                    return false;
                }

                values.Add(singleWeek);
            }

            if (values.Count == 0)
            {
                errorMessage = "Нужно оставить хотя бы одну неделю.";
                return false;
            }

            weeks = values.ToList();
            return true;
        }

        private static bool IsSameSeriesRow(ScheduleSeriesData series, DataRow row)
        {
            if (IsCurriculumPlanSeries(series)
                && row != null
                && row.Table.Columns.Contains("IDУчебныйПлан")
                && row["IDУчебныйПлан"] != DBNull.Value)
            {
                return Convert.ToInt32(row["IDРасписание"]) == series.ScheduleId
                    && Convert.ToInt32(row["ДеньНедели"]) == series.Day
                    && Convert.ToInt32(row["НомерПары"]) == series.Pair
                    && string.Equals(Convert.ToString(row["НазваниеГруппы"]), series.GroupName, StringComparison.OrdinalIgnoreCase)
                    && Convert.ToInt32(row["IDПредмет"]) == series.SubjectId
                    && string.Equals(Convert.ToString(row["ТипЗанятия"]), series.LessonType, StringComparison.OrdinalIgnoreCase)
                    && NullableIntEquals(ToNullableInt(row["IDПреподаватель"]), series.TeacherId)
                    && NullableIntEquals(ToNullableInt(row["IDПреподаватель2"]), series.SecondTeacherId)
                    && Convert.ToInt32(row["IDУчебныйПлан"]) == series.CurriculumPlanId.Value;
            }

            return ScheduleService.IsSameSeriesRow(series, row);
        }

        private bool HasSeriesConflict(ScheduleSeriesData series, int targetDay, int targetPair, List<int> targetWeeks, int? targetSecondTeacherId, out string conflictMessage)
        {
            return HasEditSeriesConflict(series, targetDay, targetPair, targetWeeks, targetSecondTeacherId, out conflictMessage);
        }

        private void SaveSeriesChanges(ScheduleSeriesData series, int targetDay, int targetPair, List<int> targetWeeks, int? targetSecondTeacherId)
        {
            if (IsCurriculumPlanSeries(series))
            {
                SaveCurriculumPlanSeriesChanges(series, targetDay, targetPair, targetWeeks, targetSecondTeacherId);
                return;
            }

            ScheduleService.SaveSeriesChanges(series, targetDay, targetPair, targetWeeks, targetSecondTeacherId);
        }

        private bool HasEditSeriesConflict(
            ScheduleSeriesData series,
            int targetDay,
            int targetPair,
            List<int> targetWeeks,
            int? targetSecondTeacherId,
            out string conflictMessage)
        {
            conflictMessage = null;
            var weekSet = new HashSet<int>(targetWeeks);
            int length = ScheduleService.GetSeriesLength(series);
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
    НомерАудитории,
    IDУчебныйПлан
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

                if (IsSameEditSeriesBlockRow(series, sourceStartPair, row))
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

                if (CanShareEditLecture(series, row))
                    continue;

                if (HasTeacherConflict(rowTeacherId, rowSecondTeacherId, series.TeacherId, targetSecondTeacherId))
                {
                    conflictMessage = "Конфликт по преподавателю на неделе " + week + ".";
                    return true;
                }

                if (!string.IsNullOrWhiteSpace(series.Room)
                    && string.Equals(rowRoom ?? string.Empty, series.Room ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                {
                    conflictMessage = "Конфликт по аудитории на неделе " + week + ".";
                    return true;
                }
            }

            return false;
        }

        private static bool CanShareEditLecture(ScheduleSeriesData series, DataRow row)
        {
            if (series == null || row == null)
                return false;

            string rowRoom = Convert.ToString(row["НомерАудитории"]);

            return string.Equals(series.LessonType, "ЛК", StringComparison.OrdinalIgnoreCase)
                && string.Equals(Convert.ToString(row["ТипЗанятия"]), "ЛК", StringComparison.OrdinalIgnoreCase)
                && Convert.ToInt32(row["IDПредмет"]) == series.SubjectId
                && NullableIntEquals(ToNullableInt(row["IDПреподаватель"]), series.TeacherId)
                && NullableIntEquals(ToNullableInt(row["IDПреподаватель2"]), series.SecondTeacherId)
                && !string.IsNullOrWhiteSpace(series.Room)
                && !string.IsNullOrWhiteSpace(rowRoom)
                && string.Equals(rowRoom, series.Room, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsSameEditSeriesBlockRow(ScheduleSeriesData series, int sourceStartPair, DataRow row)
        {
            if (series == null || row == null)
                return false;

            int rowPair = Convert.ToInt32(row["НомерПары"]);
            int length = ScheduleService.GetSeriesLength(series);

            bool sameBase =
                Convert.ToInt32(row["IDРасписание"]) == series.ScheduleId
                && Convert.ToInt32(row["ДеньНедели"]) == series.Day
                && rowPair >= sourceStartPair
                && rowPair < sourceStartPair + length
                && string.Equals(Convert.ToString(row["НазваниеГруппы"]), series.GroupName, StringComparison.OrdinalIgnoreCase)
                && Convert.ToInt32(row["IDПредмет"]) == series.SubjectId
                && string.Equals(Convert.ToString(row["ТипЗанятия"]), series.LessonType, StringComparison.OrdinalIgnoreCase)
                && NullableIntEquals(ToNullableInt(row["IDПреподаватель"]), series.TeacherId)
                && NullableIntEquals(ToNullableInt(row["IDПреподаватель2"]), series.SecondTeacherId);

            if (!sameBase)
                return false;

            if (IsCurriculumPlanSeries(series)
                && row.Table.Columns.Contains("IDУчебныйПлан")
                && row["IDУчебныйПлан"] != DBNull.Value)
            {
                return Convert.ToInt32(row["IDУчебныйПлан"]) == series.CurriculumPlanId.Value;
            }

            return string.Equals(
                Convert.ToString(row["НомерАудитории"]) ?? string.Empty,
                series.Room ?? string.Empty,
                StringComparison.OrdinalIgnoreCase);
        }

        private void SaveCurriculumPlanSeriesChanges(ScheduleSeriesData series, int targetDay, int targetPair, List<int> targetWeeks, int? targetSecondTeacherId)
        {
            int length = ScheduleService.GetSeriesLength(series);
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
  AND IDУчебныйПлан = @planId;",
                    new SqlParameter("@sid", series.ScheduleId),
                    new SqlParameter("@day", series.Day),
                    new SqlParameter("@pair", sourceStartPair),
                    new SqlParameter("@lastPair", sourceStartPair + length - 1),
                    new SqlParameter("@groupName", series.GroupName),
                    new SqlParameter("@subjectId", series.SubjectId),
                    new SqlParameter("@lessonType", series.LessonType),
                    new SqlParameter("@teacherId", (object)series.TeacherId ?? DBNull.Value),
                    new SqlParameter("@secondTeacherId", (object)series.SecondTeacherId ?? DBNull.Value),
                    new SqlParameter("@planId", series.CurriculumPlanId.Value));

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
                            new SqlParameter("@planId", series.CurriculumPlanId.Value));
                    }
                }
            });
        }

        private void SelectEditMatrixCell(string groupName, int day, int pair, ScheduleSeriesData preferredSeries = null)
        {
            if (!dgv.Columns.Contains(groupName)
                || !dgv.Columns.Contains(EditDayKeyColumn)
                || !dgv.Columns.Contains(EditPairKeyColumn))
            {
                return;
            }

            foreach (DataGridViewRow row in dgv.Rows)
            {
                if (row.IsNewRow)
                    continue;

                object rowDayValue = row.Cells[EditDayKeyColumn].Value;
                object rowPairValue = row.Cells[EditPairKeyColumn].Value;
                if (rowDayValue == null || rowDayValue == DBNull.Value
                    || rowPairValue == null || rowPairValue == DBNull.Value)
                {
                    continue;
                }

                if (Convert.ToInt32(rowDayValue) != day
                    || Convert.ToInt32(rowPairValue) != pair)
                {
                    continue;
                }

                DataGridViewCell cell = row.Cells[groupName];
                if (cell == null || string.IsNullOrWhiteSpace(Convert.ToString(cell.Value)))
                    return;

                dgv.ClearSelection();
                cell.Selected = true;
                dgv.CurrentCell = cell;
                LoadEditorFromSelectedCell(row.Index, cell.ColumnIndex, preferredSeries);
                return;
            }
        }

        private void ShowEditEntries(int scheduleId, int? selectEntryId = null)
        {
            string group = cbGroup.SelectedValue == null ||
                           string.Equals(Convert.ToString(cbGroup.SelectedValue), AllGroupsLabel, StringComparison.OrdinalIgnoreCase)
                ? string.Empty
                : Convert.ToString(cbGroup.SelectedValue);

            string room = cbRoom.SelectedValue == null
                ? string.Empty
                : Convert.ToString(cbRoom.SelectedValue);

            int? teacherId = GetSelectedTeacherId();

            DataTable dt = DataBase.Query(@"
SELECT
    rz.IDЗапись,
    rz.IDРасписание,
    rz.НомерНедели,
    rz.ДеньНедели,
    CASE rz.ДеньНедели
        WHEN 1 THEN N'Понедельник'
        WHEN 2 THEN N'Вторник'
        WHEN 3 THEN N'Среда'
        WHEN 4 THEN N'Четверг'
        WHEN 5 THEN N'Пятница'
        ELSE N''
    END AS День,
    rz.НомерПары,
    ISNULL(CONVERT(varchar(5), z.ВремяНачала, 108) + '-' + CONVERT(varchar(5), z.ВремяОкончания, 108), N'') AS Время,
    rz.НазваниеГруппы,
    rz.IDПредмет,
    CASE
        WHEN rz.ТипЗанятия = N'Физкультура'
            THEN N'Элективные курсы по физической культуре и спорту'
        ELSE p.Название
    END AS Предмет,
    rz.ТипЗанятия,
    rz.IDПреподаватель,
    t.ФИО AS Преподаватель,
    rz.НомерАудитории,
    rz.СвязкаЗанятия,
    rz.IDУчебныйПлан
FROM dbo.РасписаниеЗанятий rz
JOIN dbo.Предметы p ON p.IDПредмет = rz.IDПредмет
LEFT JOIN dbo.Преподаватели t ON t.IDПреподаватель = rz.IDПреподаватель
LEFT JOIN dbo.Преподаватели t2 ON t2.IDПреподаватель = rz.IDПреподаватель2
LEFT JOIN dbo.Звонки z ON z.НомерПары = rz.НомерПары
WHERE rz.IDРасписание = @sid
  AND (@group = N'' OR rz.НазваниеГруппы = @group)
  AND (@teacherId IS NULL OR rz.IDПреподаватель = @teacherId OR rz.IDПреподаватель2 = @teacherId)
  AND (@room = N'' OR rz.НомерАудитории = @room)
ORDER BY
    rz.НомерНедели,
    rz.ДеньНедели,
    rz.НомерПары,
    rz.НазваниеГруппы,
    p.Название;",
                new SqlParameter("@sid", scheduleId),
                new SqlParameter("@group", group),
                new SqlParameter("@teacherId", (object)teacherId ?? DBNull.Value),
                new SqlParameter("@room", room));

            ConfigureEditGrid();
            dgv.DataSource = dt;
            GridHelper.HideIdColumns(dgv);

            if (dgv.Columns.Count == 0)
                return;

            dgv.Columns["ДеньНедели"].Visible = false;
            if (dgv.Columns.Contains("СвязкаЗанятия"))
                dgv.Columns["СвязкаЗанятия"].Visible = false;

            dgv.Columns["НомерНедели"].HeaderText = "Неделя";
            dgv.Columns["День"].HeaderText = "День";
            dgv.Columns["НомерПары"].HeaderText = "Пара";
            dgv.Columns["Время"].HeaderText = "Время";
            dgv.Columns["НазваниеГруппы"].HeaderText = "Группа";
            dgv.Columns["Предмет"].HeaderText = "Предмет";
            dgv.Columns["ТипЗанятия"].HeaderText = "Тип";
            dgv.Columns["Преподаватель"].HeaderText = "Преподаватель";
            dgv.Columns["Преподаватель2"].HeaderText = "Преподаватель 2";
            dgv.Columns["НомерАудитории"].HeaderText = "Аудитория";

            dgv.Columns["НомерНедели"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            dgv.Columns["День"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            dgv.Columns["НомерПары"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            dgv.Columns["Время"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            dgv.Columns["НазваниеГруппы"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            dgv.Columns["ТипЗанятия"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            dgv.Columns["НомерАудитории"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            dgv.Columns["Предмет"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dgv.Columns["Преподаватель"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dgv.Columns["Преподаватель2"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            if (selectEntryId.HasValue)
            {
                SelectGridEntry(selectEntryId.Value);
                LoadEditorFromSelectedRow();
            }
            else
            {
                dgv.ClearSelection();
            }
        }

        private void SelectGridEntry(int entryId)
        {
            foreach (DataGridViewRow row in dgv.Rows)
            {
                if (row.IsNewRow)
                    continue;

                object entryIdValue = row.Cells["IDЗапись"].Value;
                if (entryIdValue == null || entryIdValue == DBNull.Value)
                    continue;

                if (Convert.ToInt32(entryIdValue) != entryId)
                    continue;

                row.Selected = true;
                dgv.CurrentCell = row.Cells["НомерНедели"];
                return;
            }
        }

        private static DataTable CreateEmptySubjectLookupTable()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("IDПредмет", typeof(int));
            dt.Columns.Add("Название", typeof(string));
            return dt;
        }

        private static DataTable CreateEmptyTeacherLookupTable()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("IDПреподаватель", typeof(int));
            dt.Columns.Add("ФИО", typeof(string));
            return dt;
        }

        private void AppendMissingSubject(DataTable dt, int subjectId)
        {
            foreach (DataRow row in dt.Rows)
            {
                if (Convert.ToInt32(row["IDПредмет"]) == subjectId)
                    return;
            }

            if (_allSubjects == null)
                return;

            foreach (DataRow row in _allSubjects.Rows)
            {
                if (Convert.ToInt32(row["IDПредмет"]) != subjectId)
                    continue;

                DataRow newRow = dt.NewRow();
                newRow["IDПредмет"] = subjectId;
                newRow["Название"] = Convert.ToString(row["Название"]) + " (вне профиля/семестра)";
                dt.Rows.Add(newRow);
                return;
            }
        }

        private void LoadEditSubjectsForSelectedGroupAndSemester(int? includeSubjectId)
        {
            DataTable dt;

            if (!TryGetSelectedEditGroupName(out string groupName) || !TryGetSelectedScheduleId(out _))
            {
                dt = CreateEmptySubjectLookupTable();
            }
            else
            {
                int semester = GetSelectedScheduleSemester();
                dt = DataBase.GetSubjectsForGroupAndSemester(groupName, semester);

                if (includeSubjectId.HasValue)
                    AppendMissingSubject(dt, includeSubjectId.Value);
            }

            cbEditSubject.DisplayMember = "Название";
            cbEditSubject.ValueMember = "IDПредмет";
            cbEditSubject.DataSource = dt;
        }

        private void LoadEditTeachersForSelectedSubject(int? includeTeacherId)
        {
            DataTable dt;

            if (!TryGetSelectedEditSubjectId(out int subjectId))
            {
                dt = CreateEmptyTeacherLookupTable();
            }
            else
            {
                dt = DataBase.GetTeachersForSubject(subjectId, includeTeacherId);
            }

            cbEditTeacher.DisplayMember = "ФИО";
            cbEditTeacher.ValueMember = "IDПреподаватель";
            cbEditTeacher.DataSource = dt;
        }

        private void LoadEditSecondTeachersForSelectedSubject(int? includeTeacherId)
        {
            DataTable dt;

            if (!TryGetSelectedEditSubjectId(out int subjectId))
                dt = CreateEmptyTeacherLookupTable();
            else
                dt = DataBase.GetTeachersForSubject(subjectId, includeTeacherId);

            AddEmptyTeacherChoice(dt);

            cbEditSecondTeacher.DisplayMember = "ФИО";
            cbEditSecondTeacher.ValueMember = "IDПреподаватель";
            cbEditSecondTeacher.DataSource = dt;
        }

        private static void AddEmptyTeacherChoice(DataTable dt)
        {
            if (dt == null)
                return;

            DataRow emptyRow = dt.NewRow();
            emptyRow["IDПреподаватель"] = DBNull.Value;
            emptyRow["ФИО"] = string.Empty;
            dt.Rows.InsertAt(emptyRow, 0);
        }

        private bool ValidateEditEntry(
            out int scheduleId,
            out int semester,
            out int week,
            out int day,
            out int pair,
            out string groupName,
            out int subjectId,
            out string lessonType,
            out int? teacherId,
            out string room)
        {
            scheduleId = 0;
            semester = 0;
            week = 0;
            day = 0;
            pair = 0;
            groupName = string.Empty;
            subjectId = 0;
            lessonType = string.Empty;
            teacherId = null;
            room = string.Empty;

            if (!TryGetSelectedScheduleId(out scheduleId))
            {
                MessageBox.Show("Выберите расписание.", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            semester = GetSelectedScheduleSemester();
            week = (int)nudWeek.Value;

            if (!TryGetSelectedComboInt(cbEditDay, out day))
            {
                MessageBox.Show("Выберите день недели.", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (!TryGetSelectedComboInt(cbEditPair, out pair))
            {
                MessageBox.Show("Выберите номер пары.", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (!TryGetSelectedEditGroupName(out groupName))
            {
                MessageBox.Show("Выберите группу.", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (!TryGetSelectedEditSubjectId(out subjectId))
            {
                MessageBox.Show("Выберите предмет.", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            lessonType = cbEditType.SelectedItem == null ? string.Empty : cbEditType.SelectedItem.ToString();
            if (string.IsNullOrWhiteSpace(lessonType))
            {
                MessageBox.Show("Выберите тип занятия.", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            bool isPhysicalLesson = string.Equals(lessonType, PhysicalLessonType, StringComparison.OrdinalIgnoreCase)
                || DataBase.IsPhysicalEducationSubject(subjectId);

            if (!isPhysicalLesson && !TryGetSelectedEditTeacherId(out teacherId))
            {
                MessageBox.Show("Выберите преподавателя.", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (!isPhysicalLesson && !TryGetSelectedEditRoom(out room))
            {
                MessageBox.Show("Выберите аудиторию.", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (isPhysicalLesson)
            {
                TryGetSelectedEditTeacherId(out teacherId);
                TryGetSelectedEditRoom(out room);
            }

            return true;
        }

        private bool TryGetSelectedEditGroupName(out string groupName)
        {
            groupName = string.Empty;

            if (cbEditGroup.SelectedValue == null)
                return false;

            groupName = Convert.ToString(cbEditGroup.SelectedValue).Trim();
            return !string.IsNullOrWhiteSpace(groupName);
        }

        private bool TryGetSelectedEditSubjectId(out int subjectId)
        {
            return TryGetSelectedComboInt(cbEditSubject, out subjectId);
        }

        private void RefreshEditLessonTypeItems(bool selectDefaultIfMissing)
        {
            string selectedType = cbEditType.SelectedItem?.ToString();
            bool allowPhysicalType = TryGetSelectedEditSubjectId(out int subjectId)
                && DataBase.IsPhysicalEducationSubject(subjectId);

            cbEditType.Items.Clear();
            cbEditType.Items.AddRange(allowPhysicalType
                ? new object[] { PhysicalLessonType }
                : new object[] { "ЛК", "У", "ЛБ", "КР" });

            int selectedIndex = string.IsNullOrWhiteSpace(selectedType)
                ? -1
                : cbEditType.FindStringExact(selectedType);

            if (selectedIndex >= 0)
                cbEditType.SelectedIndex = selectedIndex;
            else if (selectDefaultIfMissing && cbEditType.Items.Count > 0)
                cbEditType.SelectedIndex = 0;
            else
                cbEditType.SelectedIndex = -1;
        }

        private bool TryGetSelectedEditTeacherId(out int? teacherId)
        {
            teacherId = null;

            if (!TryGetSelectedComboInt(cbEditTeacher, out int value))
                return false;

            teacherId = value;
            return true;
        }

        private int? GetSelectedEditSecondTeacherId()
        {
            if (cbEditSecondTeacher == null || cbEditSecondTeacher.SelectedValue == null || cbEditSecondTeacher.SelectedValue == DBNull.Value)
                return null;

            return int.TryParse(cbEditSecondTeacher.SelectedValue.ToString(), out int value)
                ? value
                : (int?)null;
        }

        private bool ValidateSecondTeacherForSeries(ScheduleSeriesData series, out int? secondTeacherId)
        {
            secondTeacherId = null;

            if (series == null || !string.Equals(series.LessonType, "ЛБ", StringComparison.OrdinalIgnoreCase))
                return true;

            secondTeacherId = GetSelectedEditSecondTeacherId();
            if (!secondTeacherId.HasValue)
                return true;

            if (series.TeacherId.HasValue && secondTeacherId.Value == series.TeacherId.Value)
            {
                MessageBox.Show("Второй преподаватель должен отличаться от основного.", "Изменение",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (!DataBase.TeacherCanTeachSubject(secondTeacherId.Value, series.SubjectId))
            {
                MessageBox.Show("Второй преподаватель не закреплен за этим предметом.", "Изменение",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }

        private void UpdateSecondTeacherControlState(bool hasSelection)
        {
            bool isLab = string.Equals(cbEditType.SelectedItem?.ToString(), "ЛБ", StringComparison.OrdinalIgnoreCase);

            lblEditSecondTeacher.Enabled = hasSelection && isLab;
            cbEditSecondTeacher.Enabled = hasSelection && isLab;

            if (!isLab)
                SelectComboBoxValue(cbEditSecondTeacher, null);
        }

        private bool TryGetSelectedEditRoom(out string room)
        {
            room = string.Empty;

            if (cbEditRoom.SelectedValue == null)
                return false;

            room = Convert.ToString(cbEditRoom.SelectedValue).Trim();
            return !string.IsNullOrWhiteSpace(room);
        }

        private static bool TryGetSelectedComboInt(ComboBox comboBox, out int value)
        {
            value = 0;

            if (comboBox.SelectedValue == null || comboBox.SelectedValue == DBNull.Value)
                return false;

            return int.TryParse(comboBox.SelectedValue.ToString(), out value);
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

        private static bool ComboBoxContainsValue(ComboBox comboBox, string value)
        {
            string target = (value ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(target))
                return false;

            string valueMember = comboBox.ValueMember;

            for (int i = 0; i < comboBox.Items.Count; i++)
            {
                var rowView = comboBox.Items[i] as DataRowView;
                if (rowView != null && rowView.Row.Table.Columns.Contains(valueMember))
                {
                    if (string.Equals(Convert.ToString(rowView[valueMember]), target, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
                else if (string.Equals(Convert.ToString(comboBox.Items[i]), target, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static void SelectComboBoxValue(ComboBox comboBox, object value)
        {
            if (comboBox == null)
                return;

            if (value == null || value == DBNull.Value)
            {
                comboBox.SelectedIndex = -1;
                return;
            }

            comboBox.SelectedValue = value;

            string target = Convert.ToString(value);
            string valueMember = comboBox.ValueMember;

            for (int i = 0; i < comboBox.Items.Count; i++)
            {
                var rowView = comboBox.Items[i] as DataRowView;
                if (rowView != null && rowView.Row.Table.Columns.Contains(valueMember))
                {
                    if (string.Equals(Convert.ToString(rowView[valueMember]), target, StringComparison.OrdinalIgnoreCase))
                    {
                        comboBox.SelectedIndex = i;
                        return;
                    }
                }
                else if (string.Equals(Convert.ToString(comboBox.Items[i]), target, StringComparison.OrdinalIgnoreCase))
                {
                    comboBox.SelectedIndex = i;
                    return;
                }
            }
        }

        private void dgv_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (!IsEditMode() || e.RowIndex < 0 || e.ColumnIndex < 0)
                return;

            LoadEditorFromSelectedCell(e.RowIndex, e.ColumnIndex);
        }

        private void LoadEditorFromSelectedRow()
        {
            if (!IsEditMode() || dgv.CurrentRow == null)
                return;

            DataGridViewRow row = dgv.CurrentRow;
            if (row.IsNewRow)
                return;

            object entryIdValue = row.Cells["IDЗапись"].Value;
            object subjectIdValue = row.Cells["IDПредмет"].Value;
            object teacherIdValue = row.Cells["IDПреподаватель"].Value;
            object secondTeacherIdValue = row.Cells["IDПреподаватель2"].Value;
            object weekValue = row.Cells["НомерНедели"].Value;

            if (entryIdValue == null || entryIdValue == DBNull.Value
                || subjectIdValue == null || subjectIdValue == DBNull.Value
                || weekValue == null || weekValue == DBNull.Value)
            {
                return;
            }

            _selectedEntryId = Convert.ToInt32(entryIdValue);
            int subjectId = Convert.ToInt32(subjectIdValue);
            int? teacherId = ToNullableInt(teacherIdValue);
            int? secondTeacherId = ToNullableInt(secondTeacherIdValue);
            string groupName = Convert.ToString(row.Cells["НазваниеГруппы"].Value);
            string lessonType = Convert.ToString(row.Cells["ТипЗанятия"].Value);
            string room = Convert.ToString(row.Cells["НомерАудитории"].Value);

            _suspendEditorHandlers = true;

            nudWeek.Value = Convert.ToDecimal(weekValue);
            SelectComboBoxValue(cbEditDay, row.Cells["ДеньНедели"].Value);
            SelectComboBoxValue(cbEditPair, row.Cells["НомерПары"].Value);
            SelectComboBoxValue(cbEditGroup, groupName);

            LoadEditSubjectsForSelectedGroupAndSemester(subjectId);
            SelectComboBoxValue(cbEditSubject, subjectId);

            LoadEditTeachersForSelectedSubject(teacherId);
            SelectComboBoxValue(cbEditTeacher, teacherId);
            LoadEditSecondTeachersForSelectedSubject(secondTeacherId);
            SelectComboBoxValue(cbEditSecondTeacher, secondTeacherId);
            RefreshEditLessonTypeItems(false);

            int typeIndex = cbEditType.FindStringExact(lessonType);
            if (typeIndex >= 0)
                cbEditType.SelectedIndex = typeIndex;

            SelectComboBoxValue(cbEditRoom, room);

            _suspendEditorHandlers = false;

            btnUpdateEntry.Enabled = true;
            btnDeleteEntry.Enabled = true;
            UpdateSecondTeacherControlState(true);
        }

        private void btnAddEntry_Click(object sender, EventArgs e)
        {
            try
            {
                _selectedEntryId = null;

                if (!ValidateEditEntry(
                    out int scheduleId,
                    out int semester,
                    out int week,
                    out int day,
                    out int pair,
                    out string groupName,
                    out int subjectId,
                    out string lessonType,
                    out int? teacherId,
                    out string room))
                {
                    return;
                }

                ScheduleOperationResult result = ScheduleService.AddEntry(new ScheduleEntryData
                {
                    ScheduleId = scheduleId,
                    Semester = semester,
                    Week = week,
                    Day = day,
                    Pair = pair,
                    GroupName = groupName,
                    SubjectId = subjectId,
                    LessonType = lessonType,
                    TeacherId = teacherId,
                    Room = room
                });

                if (!result.Success)
                {
                    MessageBox.Show(result.Message, "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                int entryId = result.EntryId.Value;
                ShowEditEntries(scheduleId, entryId);
                MessageBox.Show("Запись расписания добавлена.");
            }
            catch (SqlException ex) when (ex.Number == 547)
            {
                MessageBox.Show("Нельзя добавить запись: проверьте связи и значения.", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка добавления:\n" + ex.Message, "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnUpdateEntry_Click(object sender, EventArgs e)
        {
            if (_selectedSeries == null)
            {
                MessageBox.Show("Выберите занятие в сетке расписания.", "Изменение", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                if (!TryGetSelectedComboInt(cbEditDay, out int targetDay))
                {
                    MessageBox.Show("Выберите день недели.", "Изменение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!TryGetSelectedComboInt(cbEditPair, out int targetPair))
                {
                    MessageBox.Show("Выберите новую пару.", "Изменение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                ScheduleSeriesData series = _selectedSeries;
                if (!ValidateSecondTeacherForSeries(series, out int? targetSecondTeacherId))
                    return;

                int semester = GetSelectedScheduleSemester();
                int maxWeek = DataBase.GetWeeksForGroup(series.GroupName, semester);
                int length = ScheduleService.GetSeriesLength(series);
                int maxPair = GetMaxEditPair();

                if (targetPair + length - 1 > maxPair)
                {
                    MessageBox.Show(
                        "Для ЛБ нужно две подряд идущие пары. Выберите пару не позже " + (maxPair - length + 1) + ".",
                        "Изменение",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                if (!TryParseWeeksList(txtEditWeeks.Text, maxWeek, out List<int> targetWeeks, out string parseError))
                {
                    MessageBox.Show(parseError, "Изменение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (HasSeriesConflict(series, targetDay, targetPair, targetWeeks, targetSecondTeacherId, out string conflictMessage))
                {
                    MessageBox.Show(conflictMessage, "Конфликт расписания", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                SaveSeriesChanges(series, targetDay, targetPair, targetWeeks, targetSecondTeacherId);

                var updatedSeries = new ScheduleSeriesData
                {
                    EntryId = series.EntryId,
                    ScheduleId = series.ScheduleId,
                    Day = targetDay,
                    Pair = targetPair,
                    GroupName = series.GroupName,
                    SubjectId = series.SubjectId,
                    LessonType = series.LessonType,
                    TeacherId = series.TeacherId,
                    SecondTeacherId = targetSecondTeacherId,
                    Room = series.Room,
                    SeriesId = series.SeriesId,
                    CurriculumPlanId = series.CurriculumPlanId
                };

                ResetEditor(true);
                ShowEditableWeekMatrix(series.ScheduleId);
                SelectEditMatrixCell(series.GroupName, targetDay, targetPair, updatedSeries);

                MessageBox.Show("Изменения расписания сохранены.");
                return;
            }
            catch (SqlException ex) when (ex.Number == 547)
            {
                MessageBox.Show("Нельзя сохранить изменения: проверьте связи и значения.", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка изменения:\n" + ex.Message, "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

#pragma warning disable 162
            if (!_selectedEntryId.HasValue)
            {
                MessageBox.Show("Выберите запись в таблице.", "Изменение", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                if (!ValidateEditEntry(
                    out int scheduleId,
                    out int semester,
                    out int week,
                    out int day,
                    out int pair,
                    out string groupName,
                    out int subjectId,
                    out string lessonType,
                    out int? teacherId,
                    out string room))
                {
                    return;
                }

                ScheduleOperationResult result = ScheduleService.UpdateEntry(new ScheduleEntryData
                {
                    EntryId = _selectedEntryId,
                    ScheduleId = scheduleId,
                    Semester = semester,
                    Week = week,
                    Day = day,
                    Pair = pair,
                    GroupName = groupName,
                    SubjectId = subjectId,
                    LessonType = lessonType,
                    TeacherId = teacherId,
                    Room = room
                });

                if (!result.Success)
                {
                    MessageBox.Show(result.Message, "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                ShowEditEntries(scheduleId, _selectedEntryId.Value);
                MessageBox.Show("Запись расписания обновлена.");
            }
            catch (SqlException ex) when (ex.Number == 547)
            {
                MessageBox.Show("Нельзя изменить запись: проверьте связи и значения.", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка изменения:\n" + ex.Message, "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
#pragma warning restore 162
        }

        private int GetMaxEditPair()
        {
            int maxPair = 0;

            foreach (object item in cbEditPair.Items)
            {
                var rowView = item as DataRowView;
                if (rowView == null)
                    continue;

                object value = rowView["PairValue"];
                if (value != null && value != DBNull.Value)
                    maxPair = Math.Max(maxPair, Convert.ToInt32(value));
            }

            return maxPair > 0 ? maxPair : 5;
        }

        private void btnDeleteEntry_Click(object sender, EventArgs e)
        {
            if (!_selectedEntryId.HasValue)
            {
                MessageBox.Show("Выберите запись в таблице.", "Удаление", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DialogResult confirm = MessageBox.Show(
                "Удалить выбранную запись расписания?",
                "Подтверждение",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes)
                return;

            try
            {
                if (!TryGetSelectedScheduleId(out int scheduleId))
                {
                    MessageBox.Show("Выберите расписание.", "Удаление", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                ScheduleService.DeleteEntry(_selectedEntryId.Value);

                ResetEditor();
                ShowEditEntries(scheduleId);
                MessageBox.Show("Запись расписания удалена.");
            }
            catch (SqlException ex) when (ex.Number == 547)
            {
                MessageBox.Show("Нельзя удалить запись из-за связанных данных.", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка удаления:\n" + ex.Message, "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnResetEdit_Click(object sender, EventArgs e)
        {
            ResetEditor(true);
            dgv.ClearSelection();
        }

        private void Dgv_CellPainting_DaySeparator(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            int maxPairs = 5;
            try
            {
                Dictionary<int, string> bellTimes = LoadBellTimes();
                maxPairs = GetMaxPairsFromBells(bellTimes);
            }
            catch
            {
                maxPairs = 5;
            }

            if (maxPairs <= 0)
                maxPairs = 5;

            bool isNewDayRow = e.RowIndex % maxPairs == 0;
            bool shouldDraw = isNewDayRow && e.RowIndex != 0;

            if (!shouldDraw)
                return;

            e.Paint(e.CellBounds, DataGridViewPaintParts.All);

            using (var pen = new System.Drawing.Pen(System.Drawing.Color.Black, 3))
            {
                int y = e.CellBounds.Top;
                e.Graphics.DrawLine(pen, e.CellBounds.Left, y, e.CellBounds.Right, y);
            }

            e.Handled = true;
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            ClearFilters();
            ResetEditor();
            dgv.DataSource = null;
            UpdateDeleteScheduleButtonState();
        }

        private void btnDeleteSchedule_Click(object sender, EventArgs e)
        {
            try
            {
                if (IsFlowMode() || IsTeacherSemesterMode())
                {
                    MessageBox.Show("Для удаления выберите обычный режим просмотра и конкретное расписание.", "Удаление",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (!TryGetSelectedScheduleId(out int scheduleId))
                {
                    MessageBox.Show("Выберите расписание.", "Удаление", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string scheduleName = GetSelectedScheduleDisplayName();
                int entryCount = Convert.ToInt32(DataBase.ExecuteScalar(@"
SELECT COUNT(*)
FROM dbo.РасписаниеЗанятий
WHERE IDРасписание = @sid;",
                    new SqlParameter("@sid", scheduleId)));

                DialogResult answer = MessageBox.Show(
                    "Удалить расписание?\n\n" +
                    scheduleName +
                    "\n\nБудет удалено занятий: " + entryCount +
                    "\nУчебный план, группы, предметы и преподаватели останутся.",
                    "Удаление расписания",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2);

                if (answer != DialogResult.Yes)
                    return;

                DataBase.ExecuteInTransaction((con, tx) =>
                {
                    DataBase.ExecuteNonQuery(con, tx, @"
DELETE FROM dbo.РасписаниеЗанятий
WHERE IDРасписание = @sid;",
                        new SqlParameter("@sid", scheduleId));

                    DataBase.ExecuteNonQuery(con, tx, @"
DELETE FROM dbo.Расписания
WHERE IDРасписание = @sid;",
                        new SqlParameter("@sid", scheduleId));
                });

                dgv.DataSource = null;
                ResetEditor();

                bool wasSuspended = _suspendEditorHandlers;
                _suspendEditorHandlers = true;
                try
                {
                    LoadLookups();
                    if (cbSchedule.Items.Count > 0)
                        cbSchedule.SelectedIndex = 0;
                }
                finally
                {
                    _suspendEditorHandlers = wasSuspended;
                }

                LoadFilterGroupsForSelectedSchedule(false);
                LoadTeachersForSelectedSchedule(false);
                UpdateViewModeState();
                UpdateDeleteScheduleButtonState();

                MessageBox.Show("Расписание удалено.", "Удаление", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка удаления расписания:\n" + ex.Message, "Удаление",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string GetSelectedScheduleDisplayName()
        {
            var rowView = cbSchedule.SelectedItem as DataRowView;
            if (rowView != null && rowView.Row.Table.Columns.Contains("DisplayName"))
                return Convert.ToString(rowView["DisplayName"]);

            return cbSchedule.Text;
        }

        private void UpdateDeleteScheduleButtonState()
        {
            if (btnDeleteSchedule == null)
                return;

            btnDeleteSchedule.Enabled = !IsFlowMode() && !IsTeacherSemesterMode() && TryGetSelectedScheduleId(out _);
        }

        private void btnShow_Click(object sender, EventArgs e)
        {
            try
            {
                if (IsFlowMode())
                {
                    if (!GetSelectedFlowCourse().HasValue)
                    {
                        MessageBox.Show("Выберите курс.");
                        return;
                    }

                    if (!GetSelectedFlowSemester().HasValue)
                    {
                        MessageBox.Show("Выберите семестр.");
                        return;
                    }

                    ShowFlowMatrix();
                    return;
                }

                if (!TryGetSelectedScheduleId(out int scheduleId))
                {
                    MessageBox.Show("Выберите расписание.");
                    return;
                }

                if (IsEditMode())
                {
                    ResetEditor(true);
                    ShowEditableWeekMatrix(scheduleId);
                    return;
                }

                if (IsTeacherSemesterMode())
                {
                    int? teacherId = GetSelectedTeacherId();
                    if (!teacherId.HasValue)
                    {
                        MessageBox.Show("Выберите преподавателя.");
                        return;
                    }

                    ShowTeacherSemester(scheduleId, teacherId.Value);
                }
                else
                {
                    ShowMatrix(scheduleId);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка показа:\n" + ex.Message);
            }
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            try
            {
                if (dgv.DataSource == null || dgv.Rows.Count == 0)
                {
                    MessageBox.Show("Нечего экспортировать. Сначала нажми «Показать».");
                    return;
                }

                if (sfdExcel.ShowDialog() != DialogResult.OK)
                    return;

                ExportDataGridViewToExcel(dgv, sfdExcel.FileName);

                MessageBox.Show("Готово! Файл сохранён.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка экспорта:\n" + ex.Message);
            }
        }

        private void ExportDataGridViewToExcel(DataGridView grid, string filePath)
        {
            if (File.Exists(filePath))
            {
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                }
            }

            using (var wb = new XLWorkbook())
            {
                var ws = wb.Worksheets.Add("Расписание");

                int excelCol = 1;
                List<DataGridViewColumn> visibleCols = grid.Columns.Cast<DataGridViewColumn>().Where(c => c.Visible).ToList();

                foreach (DataGridViewColumn col in visibleCols)
                {
                    ws.Cell(1, excelCol).Value = col.HeaderText;
                    ws.Cell(1, excelCol).Style.Font.Bold = true;
                    ws.Cell(1, excelCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    ws.Cell(1, excelCol).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    ws.Cell(1, excelCol).Style.Fill.BackgroundColor = XLColor.LightGray;
                    ws.Cell(1, excelCol).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    excelCol++;
                }

                int excelRow = 2;

                foreach (DataGridViewRow row in grid.Rows)
                {
                    if (row.IsNewRow)
                        continue;

                    excelCol = 1;
                    int maxLinesInRow = 1;

                    foreach (DataGridViewColumn col in visibleCols)
                    {
                        string val = row.Cells[col.Index].Value == null ? string.Empty : row.Cells[col.Index].Value.ToString();
                        val = val.Replace("\r\n", "\n");

                        var cell = ws.Cell(excelRow, excelCol);
                        cell.Value = val;
                        cell.Style.Alignment.WrapText = true;
                        cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
                        cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                        int lines = string.IsNullOrEmpty(val) ? 1 : val.Count(ch => ch == '\n') + 1;
                        if (lines > maxLinesInRow)
                            maxLinesInRow = lines;

                        excelCol++;
                    }

                    double rowHeight = 15 * maxLinesInRow;
                    if (rowHeight < 18)
                        rowHeight = 18;
                    if (rowHeight > 300)
                        rowHeight = 300;

                    ws.Row(excelRow).Height = rowHeight;
                    excelRow++;
                }

                if (visibleCols.Count >= 2)
                {
                    ws.Column(1).Width = GetExcelColumnWidth(visibleCols[0].HeaderText);
                    ws.Column(2).Width = GetExcelColumnWidth(visibleCols[1].HeaderText);
                }

                for (int c = 3; c <= visibleCols.Count; c++)
                    ws.Column(c).Width = GetExcelColumnWidth(visibleCols[c - 1].HeaderText);

                ws.SheetView.FreezeRows(1);
                wb.SaveAs(filePath);
            }
        }

        private static double GetExcelColumnWidth(string headerText)
        {
            switch (headerText)
            {
                case "Неделя":
                    return 10;
                case "День":
                    return 18;
                case "Пара":
                    return 8;
                case "Время":
                    return 12;
                case "Группа":
                    return 16;
                case "Предмет":
                    return 28;
                case "Тип занятия":
                case "Тип":
                    return 14;
                case "Аудитория":
                    return 12;
                case "Преподаватель":
                case "Преподаватель 2":
                    return 28;
                default:
                    return 40;
            }
        }

        private static string FormatSubjectWithLessonType(DataRow item)
        {
            string subject = Convert.ToString(item["Предмет"]).Trim();
            string lessonType = Convert.ToString(item["ТипЗанятия"]).Trim();

            if (IsPhysicalScheduleItem(item))
                return PhysicalSubjectDisplayName;

            if (string.IsNullOrWhiteSpace(lessonType))
                return subject;

            return subject + " (" + lessonType + ")";
        }

        private static string FormatTeacherDisplay(DataRow item)
        {
            if (item == null)
                return string.Empty;

            string teacher = item.Table.Columns.Contains("Преподаватель")
                ? Convert.ToString(item["Преподаватель"]).Trim()
                : string.Empty;
            string secondTeacher = item.Table.Columns.Contains("Преподаватель2")
                ? Convert.ToString(item["Преподаватель2"]).Trim()
                : string.Empty;

            if (string.IsNullOrWhiteSpace(secondTeacher))
                return teacher;

            if (string.IsNullOrWhiteSpace(teacher))
                return secondTeacher;

            return teacher + ", " + secondTeacher;
        }

        private static List<DataRow> MergeLessonRowsForDisplay(List<DataRow> rows)
        {
            if (rows == null || rows.Count == 0)
                return rows ?? new List<DataRow>();

            DataTable sourceTable = rows[0].Table;
            if (sourceTable == null
                || !sourceTable.Columns.Contains("Недели")
                || !sourceTable.Columns.Contains("НомерАудитории"))
            {
                return rows;
            }

            DataTable mergedTable = sourceTable.Clone();
            var result = new List<DataRow>();

            foreach (IGrouping<string, DataRow> group in rows.GroupBy(BuildDisplayMergeKey))
            {
                DataRow first = group.First();
                DataRow merged = mergedTable.NewRow();
                merged.ItemArray = (object[])first.ItemArray.Clone();

                List<int> weeks = group
                    .SelectMany(row => ParseWeeksCsv(Convert.ToString(row["Недели"])))
                    .Distinct()
                    .OrderBy(week => week)
                    .ToList();

                merged["Недели"] = string.Join(", ", weeks);
                merged["НомерАудитории"] = GetDisplayRoomForMergedRows(group);

                mergedTable.Rows.Add(merged);
                result.Add(merged);
            }

            return result
                .OrderBy(row => Convert.ToInt32(row["ДеньНедели"]))
                .ThenBy(row => Convert.ToInt32(row["НомерПары"]))
                .ThenBy(row => Convert.ToString(row["НазваниеГруппы"]))
                .ThenBy(row => Convert.ToString(row["Предмет"]))
                .ToList();
        }

        private static string BuildDisplayMergeKey(DataRow row)
        {
            return string.Join("\u001F",
                Convert.ToString(row["ДеньНедели"]),
                Convert.ToString(row["НомерПары"]),
                Convert.ToString(row["НазваниеГруппы"]),
                Convert.ToString(row["IDПредмет"]),
                Convert.ToString(row["ТипЗанятия"]),
                Convert.ToString(row["IDПреподаватель"]),
                row.Table.Columns.Contains("IDПреподаватель2") ? Convert.ToString(row["IDПреподаватель2"]) : string.Empty);
        }

        private static string GetDisplayRoomForMergedRows(IEnumerable<DataRow> rows)
        {
            List<DataRow> rowList = rows.ToList();
            if (rowList.Any(IsPhysicalScheduleItem))
                return string.Empty;

            return rowList
                .Select(row => new
                {
                    Room = Convert.ToString(row["НомерАудитории"]).Trim(),
                    WeekCount = Math.Max(1, ParseWeeksCsv(Convert.ToString(row["Недели"])).Count)
                })
                .Where(x => !string.IsNullOrWhiteSpace(x.Room))
                .GroupBy(x => x.Room, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(group => group.Sum(x => x.WeekCount))
                .ThenBy(group => group.Key)
                .Select(group => group.Key)
                .FirstOrDefault() ?? string.Empty;
        }

        private static string FormatRoomLine(DataRow item)
        {
            string room = FormatRoomText(item);
            return string.IsNullOrWhiteSpace(room) ? string.Empty : "\n" + room;
        }

        private static string FormatRoomText(DataRow item)
        {
            if (IsPhysicalScheduleItem(item))
                return string.Empty;

            string room = item != null && item.Table.Columns.Contains("НомерАудитории")
                ? Convert.ToString(item["НомерАудитории"]).Trim()
                : string.Empty;

            return string.IsNullOrWhiteSpace(room) ? string.Empty : "ауд. " + room;
        }

        private static bool IsPhysicalScheduleItem(DataRow item)
        {
            if (item == null)
                return false;

            string lessonType = item.Table.Columns.Contains("ТипЗанятия")
                ? Convert.ToString(item["ТипЗанятия"]).Trim()
                : string.Empty;
            if (string.Equals(lessonType, PhysicalLessonType, StringComparison.OrdinalIgnoreCase))
                return true;

            string subject = item.Table.Columns.Contains("Предмет")
                ? Convert.ToString(item["Предмет"]).Trim()
                : string.Empty;

            return string.Equals(subject, "Физкультура", StringComparison.OrdinalIgnoreCase)
                || string.Equals(subject, "Физ-ра", StringComparison.OrdinalIgnoreCase)
                || string.Equals(subject, PhysicalSubjectDisplayName, StringComparison.OrdinalIgnoreCase);
        }

        private void ViewScheduleForm_Resize(object sender, EventArgs e)
        {
            LayoutContent();
        }
    }
}
